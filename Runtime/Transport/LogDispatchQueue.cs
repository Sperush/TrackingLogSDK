using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TrackingLogSDK
{
    /// <summary>
    /// Persistence hook for saving unsent envelopes across app restarts.
    /// </summary>
    public interface ILogQueueStorage
    {
        List<TrackingEnvelope> Load();
        void Save(IReadOnlyList<TrackingEnvelope> envelopes);
    }

    /// <summary>
    /// Simple pipe-delimited JSONL persistence store for unsent TrackingEnvelope items.
    /// </summary>
    public sealed class FileLogQueueStorage : ILogQueueStorage
    {
        private readonly string _filePath;
        private readonly object _lock = new object();

        public FileLogQueueStorage(string filePath)
        {
            _filePath = filePath;
        }

        public List<TrackingEnvelope> Load()
        {
            var list = new List<TrackingEnvelope>();
            if (string.IsNullOrEmpty(_filePath))
            {
                return list;
            }

            try
            {
                lock (_lock)
                {
                    if (!File.Exists(_filePath))
                    {
                        return list;
                    }

                    string[] lines = File.ReadAllLines(_filePath, Encoding.UTF8);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        // Format: id|collection|action|enqueuedAt|retryCount|jsonBody
                        string[] parts = line.Split(new[] { '|' }, 6);
                        if (parts.Length < 6)
                        {
                            continue;
                        }

                        long enqueuedAt;
                        int retryCount;
                        long.TryParse(parts[3], out enqueuedAt);
                        int.TryParse(parts[4], out retryCount);

                        list.Add(new TrackingEnvelope
                        {
                            Id = parts[0],
                            Collection = parts[1],
                            ActionName = parts[2],
                            EnqueuedAtMs = enqueuedAt,
                            RetryCount = retryCount,
                            NextAttemptAtMs = 0,
                            JsonBody = parts[5]
                        });
                    }
                }
            }
            catch
            {
                // Ignore corrupt offline queue file rather than crashing game startup.
            }

            return list;
        }

        public void Save(IReadOnlyList<TrackingEnvelope> envelopes)
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                return;
            }

            try
            {
                lock (_lock)
                {
                    if (envelopes == null || envelopes.Count == 0)
                    {
                        if (File.Exists(_filePath))
                        {
                            File.Delete(_filePath);
                        }
                        return;
                    }

                    string dir = Path.GetDirectoryName(_filePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var sb = new StringBuilder();
                    for (int i = 0; i < envelopes.Count; i++)
                    {
                        TrackingEnvelope env = envelopes[i];
                        if (env == null || string.IsNullOrEmpty(env.JsonBody))
                        {
                            continue;
                        }

                        string safeJson = env.JsonBody.Replace("\r", "").Replace("\n", " ");
                        sb.Append(SanitizeField(env.Id)).Append('|')
                          .Append(SanitizeField(env.Collection)).Append('|')
                          .Append(SanitizeField(env.ActionName)).Append('|')
                          .Append(env.EnqueuedAtMs).Append('|')
                          .Append(env.RetryCount).Append('|')
                          .Append(safeJson).Append('\n');
                    }

                    File.WriteAllText(_filePath, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // Best-effort offline persistence.
            }
        }

        private static string SanitizeField(string value)
        {
            return (value ?? string.Empty).Replace("|", "_").Replace("\r", "").Replace("\n", "");
        }
    }

    /// <summary>
    /// Bounded FIFO dispatch queue with exponential backoff retries and optional offline storage.
    /// </summary>
    public sealed class LogDispatchQueue
    {
        private readonly ILogTransport _transport;
        private readonly IClock _clock;
        private readonly ILogQueueStorage _storage;
        private readonly int _maxQueueSize;
        private readonly int _maxRetryCount;
        private readonly float _initialRetryDelaySeconds;
        private readonly List<TrackingEnvelope> _pending = new List<TrackingEnvelope>();
        private readonly object _lock = new object();

        private bool _isSending;
        private bool _dirtyStorage;

        public int PendingCount
        {
            get
            {
                lock (_lock)
                {
                    return _pending.Count;
                }
            }
        }

        public bool IsSending
        {
            get
            {
                lock (_lock)
                {
                    return _isSending;
                }
            }
        }

        public LogDispatchQueue(
            ILogTransport transport,
            IClock clock,
            int maxQueueSize = 500,
            int maxRetryCount = 5,
            float initialRetryDelaySeconds = 2f,
            ILogQueueStorage storage = null)
        {
            _transport = transport;
            _clock = clock ?? new SystemClock();
            _maxQueueSize = maxQueueSize > 0 ? maxQueueSize : 500;
            _maxRetryCount = maxRetryCount >= 0 ? maxRetryCount : 5;
            _initialRetryDelaySeconds = initialRetryDelaySeconds > 0f ? initialRetryDelaySeconds : 2f;
            _storage = storage;

            if (_storage != null)
            {
                List<TrackingEnvelope> restored = _storage.Load();
                if (restored != null && restored.Count > 0)
                {
                    int startIndex = Math.Max(0, restored.Count - _maxQueueSize);
                    for (int i = startIndex; i < restored.Count; i++)
                    {
                        _pending.Add(restored[i]);
                    }
                }
            }
        }

        public void Enqueue(TrackingEnvelope envelope)
        {
            if (envelope == null)
            {
                return;
            }

            lock (_lock)
            {
                if (_pending.Count >= _maxQueueSize)
                {
                    // Drop the oldest item when capacity is reached.
                    _pending.RemoveAt(0);
                }

                _pending.Add(envelope);
                _dirtyStorage = true;
            }

            PersistIfDirty();
            Tick(_clock.UtcNowMs);
        }

        public void Tick(long nowMs)
        {
            TrackingEnvelope nextToSend = null;

            lock (_lock)
            {
                if (_isSending || _transport == null || _pending.Count == 0)
                {
                    return;
                }

                TrackingEnvelope head = _pending[0];
                if (head.NextAttemptAtMs > nowMs)
                {
                    return;
                }

                _isSending = true;
                nextToSend = head;
            }

            _transport.Send(nextToSend, result => HandleSendResult(nextToSend, result));
        }

        public void FlushImmediate()
        {
            if (_transport == null)
            {
                return;
            }

            List<TrackingEnvelope> snapshot;
            lock (_lock)
            {
                if (_pending.Count == 0)
                {
                    return;
                }
                snapshot = new List<TrackingEnvelope>(_pending);
            }

            var remaining = new List<TrackingEnvelope>();
            for (int i = 0; i < snapshot.Count; i++)
            {
                TrackingEnvelope env = snapshot[i];
                if (!_transport.TrySendImmediate(env))
                {
                    remaining.Add(env);
                }
            }

            lock (_lock)
            {
                _pending.Clear();
                for (int i = 0; i < remaining.Count; i++)
                {
                    _pending.Add(remaining[i]);
                }
                _isSending = false;
                _dirtyStorage = true;
            }

            PersistIfDirty();
        }

        public bool SendImmediate(TrackingEnvelope envelope)
        {
            if (envelope == null)
            {
                return true;
            }

            if (_transport != null && _transport.TrySendImmediate(envelope))
            {
                return true;
            }

            lock (_lock)
            {
                if (_pending.Count >= _maxQueueSize)
                {
                    _pending.RemoveAt(0);
                }
                _pending.Add(envelope);
                _dirtyStorage = true;
            }

            PersistIfDirty();
            return false;
        }

        public void PersistNow()
        {
            if (_storage == null)
            {
                return;
            }

            List<TrackingEnvelope> snapshot;
            lock (_lock)
            {
                snapshot = new List<TrackingEnvelope>(_pending);
                _dirtyStorage = false;
            }
            _storage.Save(snapshot);
        }

        private void HandleSendResult(TrackingEnvelope envelope, TransportResult result)
        {
            bool shouldContinueImmediately = false;

            lock (_lock)
            {
                _isSending = false;

                int index = _pending.IndexOf(envelope);
                if (index >= 0)
                {
                    if (result.Success)
                    {
                        _pending.RemoveAt(index);
                        _dirtyStorage = true;
                        shouldContinueImmediately = _pending.Count > 0 && _pending[0].NextAttemptAtMs <= _clock.UtcNowMs;
                    }
                    else if (result.IsRetryable && envelope.RetryCount < _maxRetryCount)
                    {
                        envelope.RetryCount++;
                        double delaySeconds = _initialRetryDelaySeconds * Math.Pow(2.0, envelope.RetryCount - 1);
                        long delayMs = (long)(delaySeconds * 1000.0);
                        envelope.NextAttemptAtMs = _clock.UtcNowMs + delayMs;
                        _dirtyStorage = true;
                    }
                    else
                    {
                        // Non-retryable client error (e.g. 400) or max retries exhausted.
                        _pending.RemoveAt(index);
                        _dirtyStorage = true;
                        shouldContinueImmediately = _pending.Count > 0 && _pending[0].NextAttemptAtMs <= _clock.UtcNowMs;
                    }
                }
            }

            PersistIfDirty();

            if (shouldContinueImmediately)
            {
                Tick(_clock.UtcNowMs);
            }
        }

        private void PersistIfDirty()
        {
            if (_storage == null)
            {
                return;
            }

            List<TrackingEnvelope> snapshot = null;
            lock (_lock)
            {
                if (_dirtyStorage)
                {
                    snapshot = new List<TrackingEnvelope>(_pending);
                    _dirtyStorage = false;
                }
            }

            if (snapshot != null)
            {
                _storage.Save(snapshot);
            }
        }
    }
}
