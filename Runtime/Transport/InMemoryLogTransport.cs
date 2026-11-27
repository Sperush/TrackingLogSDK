using System;
using System.Collections.Generic;

namespace TrackingLogSDK
{
    /// <summary>
    /// In-memory transport for unit testing and offline inspection.
    /// </summary>
    public sealed class InMemoryLogTransport : ILogTransport
    {
        private readonly object _lock = new object();
        private readonly List<TrackingEnvelope> _sent = new List<TrackingEnvelope>();
        private int _failNextCount;
        private bool _failRetryable = true;

        public string Name
        {
            get { return "InMemory"; }
        }

        public IReadOnlyList<TrackingEnvelope> Sent
        {
            get
            {
                lock (_lock)
                {
                    return _sent.ToArray();
                }
            }
        }

        public void SimulateFailures(int count, bool retryable = true)
        {
            lock (_lock)
            {
                _failNextCount = count;
                _failRetryable = retryable;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _sent.Clear();
            }
        }

        public void Send(TrackingEnvelope envelope, Action<TransportResult> onComplete)
        {
            TransportResult result;
            lock (_lock)
            {
                if (_failNextCount > 0)
                {
                    _failNextCount--;
                    result = TransportResult.Fail(503, "Simulated transport failure", _failRetryable);
                }
                else
                {
                    _sent.Add(envelope);
                    result = TransportResult.Ok(200);
                }
            }

            if (onComplete != null)
            {
                onComplete(result);
            }
        }

        public bool TrySendImmediate(TrackingEnvelope envelope)
        {
            lock (_lock)
            {
                if (_failNextCount > 0)
                {
                    _failNextCount--;
                    return false;
                }

                _sent.Add(envelope);
                return true;
            }
        }
    }
}
