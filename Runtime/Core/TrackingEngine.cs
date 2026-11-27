using System;
using System.Collections.Generic;
using System.Globalization;

namespace TrackingLogSDK
{
    /// <summary>
    /// Recorded payload entry exposed for live debugging tools.
    /// </summary>
    public sealed class CapturedLogEntry
    {
        public string Collection;
        public string ActionName;
        public long TimestampMs;
        public string JsonBody;
    }

    /// <summary>
    /// Pure C# coordinator managing session, level lifecycle, metadata, serialization, and dispatch queue.
    /// </summary>
    public sealed class TrackingEngine
    {
        private readonly TrackingConfig _config;
        private readonly TrackingMetadata _metadata;
        private readonly IClock _clock;
        private readonly IAttemptStore _attemptStore;
        private readonly LogDispatchQueue _queue;
        private readonly SessionTracker _session;
        private readonly LevelTracker _level;
        private readonly List<CapturedLogEntry> _recentLogs = new List<CapturedLogEntry>();
        private readonly object _historyLock = new object();

        private bool _quitHandled;
        private bool _pausedByFocusLoss;

        public TrackingConfig Config { get { return _config; } }
        public TrackingMetadata Metadata { get { return _metadata; } }
        public SessionTracker Session { get { return _session; } }
        public LevelTracker Level { get { return _level; } }
        public LogDispatchQueue Queue { get { return _queue; } }

        public event Action<CapturedLogEntry> OnPayloadCaptured;

        public TrackingEngine(
            TrackingConfig config,
            TrackingMetadata metadata,
            ILogTransport transport,
            IAttemptStore attemptStore = null,
            IClock clock = null,
            ILogQueueStorage queueStorage = null)
        {
            _config = config != null ? config.Clone() : new TrackingConfig();
            _metadata = metadata != null ? metadata.Clone() : new TrackingMetadata();
            _clock = clock ?? new SystemClock();
            _attemptStore = attemptStore ?? new InMemoryAttemptStore();
            _session = new SessionTracker(_clock);
            _level = new LevelTracker(_clock);
            _queue = new LogDispatchQueue(
                transport,
                _clock,
                _config.MaxQueueSize,
                _config.MaxRetryCount,
                _config.InitialRetryDelaySeconds,
                queueStorage);

            if (_config.AutoStartSession)
            {
                StartSession();
            }
        }

        public void SetUserId(string userId)
        {
            _metadata.UserId = userId ?? string.Empty;
        }

        public void SetCustomMetadata(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            _metadata.CustomMetadata[key] = value;
        }

        public void RemoveCustomMetadata(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _metadata.CustomMetadata.Remove(key);
            }
        }

        public void StartSession()
        {
            if (_session.IsActive)
            {
                EndSession();
            }

            _quitHandled = false;
            _session.Start();
            _metadata.SessionId = _session.SessionId;

            long nowMs = _clock.UtcNowMs;
            string json = TrackingPayloadBuilder.BuildSessionPayload(
                _config,
                _metadata,
                nowMs,
                "start",
                _session.StartTimeMs,
                null,
                0f);

            EnqueuePayload(_config.SessionCollection, "start", nowMs, json, immediate: false);
        }

        public void EndSession()
        {
            EndSessionInternal(immediate: false);
        }

        public void SetApplicationBackground(bool inBackground)
        {
            _session.SetBackground(inBackground);

            if (!_config.AutoPauseOnFocusLost || !_level.IsActive)
            {
                return;
            }

            if (inBackground && !_level.IsPaused)
            {
                _pausedByFocusLoss = true;
                _level.Pause("focus_lost", _config.AutoLogPauseEvents);
            }
            else if (!inBackground && _pausedByFocusLoss && _level.IsPaused)
            {
                _pausedByFocusLoss = false;
                _level.Resume(_config.AutoLogPauseEvents);
            }
        }

        public void StartLevel(string levelId, string levelType = "normal")
        {
            BeginLevelInternal(levelId, levelType, isRetry: false);
        }

        public void StartLevel(int levelId, string levelType = "normal")
        {
            BeginLevelInternal(levelId, levelType, isRetry: false);
        }

        public void RetryLevel(string levelId, string levelType = "normal")
        {
            BeginLevelInternal(levelId, levelType, isRetry: true);
        }

        public void RetryLevel(int levelId, string levelType = "normal")
        {
            BeginLevelInternal(levelId, levelType, isRetry: true);
        }

        public void RetryCurrentLevel()
        {
            if (_level.LevelId == null)
            {
                return;
            }

            BeginLevelInternal(_level.LevelId, _level.LevelType, isRetry: true);
        }

        public void TrackLevelEvent(string eventName, string value = "")
        {
            _level.RecordEvent(eventName, value);
        }

        public void SetLevelResultProvider(Func<IDictionary<string, object>> provider)
        {
            _level.SetResultProvider(provider);
        }

        public void SetLevelResultValue(string key, object value)
        {
            _level.SetResultValue(key, value);
        }

        public void PauseLevel(string reason = "menu")
        {
            _pausedByFocusLoss = false;
            _level.Pause(reason, _config.AutoLogPauseEvents);
        }

        public void ResumeLevel()
        {
            _pausedByFocusLoss = false;
            _level.Resume(_config.AutoLogPauseEvents);
        }

        public void ResumeLevelFromAppReturn()
        {
            _level.ResumeFromAppReturn();
        }

        public void EndLevelWin(IDictionary<string, object> finalResult = null)
        {
            EndLevel("win", null, finalResult);
        }

        public void EndLevelLose(string loseReason, IDictionary<string, object> finalResult = null)
        {
            EndLevel("lose", loseReason, finalResult);
        }

        public void QuitLevel(IDictionary<string, object> finalResult = null)
        {
            EndLevel("quit_level", null, finalResult);
        }

        public void EndLevel(string actionType, string loseReason = null, IDictionary<string, object> finalResult = null)
        {
            EndLevelInternal(actionType, loseReason, finalResult, immediate: false);
        }

        public void TrackCustomEvent(string collection, IDictionary<string, object> fields, string actionName = "")
        {
            if (string.IsNullOrEmpty(collection))
            {
                return;
            }

            long nowMs = _clock.UtcNowMs;
            string json = TrackingPayloadBuilder.BuildCustomPayload(_config, _metadata, nowMs, collection, fields);
            EnqueuePayload(collection, actionName ?? string.Empty, nowMs, json, immediate: false);
        }

        public void Tick(float deltaSeconds)
        {
            _session.Tick(deltaSeconds);

            LevelPayloadData inProgress;
            if (_level.Tick(deltaSeconds, _config.InProgressIntervalSeconds, _config.InProgressDeltaEventsOnly, out inProgress))
            {
                long nowMs = _clock.UtcNowMs;
                string json = TrackingPayloadBuilder.BuildLevelPayload(_config, _metadata, nowMs, inProgress);
                EnqueuePayload(_config.LevelCollection, inProgress.ActionName, nowMs, json, immediate: false);
            }

            _queue.Tick(_clock.UtcNowMs);
        }

        public void Flush()
        {
            _queue.FlushImmediate();
        }

        public void HandleApplicationQuit()
        {
            if (_quitHandled)
            {
                return;
            }
            _quitHandled = true;

            if (_level.IsActive)
            {
                EndLevelInternal("quit_app", null, null, immediate: true);
            }

            if (_session.IsActive)
            {
                EndSessionInternal(immediate: true);
            }

            _queue.FlushImmediate();
            _queue.PersistNow();
        }

        public CapturedLogEntry[] GetRecentLogs()
        {
            lock (_historyLock)
            {
                return _recentLogs.ToArray();
            }
        }

        public void ClearRecentLogs()
        {
            lock (_historyLock)
            {
                _recentLogs.Clear();
            }
        }

        private void BeginLevelInternal(object levelId, string levelType, bool isRetry)
        {
            if (!_session.IsActive)
            {
                StartSession();
            }

            if (_level.IsActive)
            {
                EndLevelInternal("quit_level", null, null, immediate: false);
            }

            string levelKey = Convert.ToString(levelId ?? string.Empty, CultureInfo.InvariantCulture) ?? string.Empty;
            int attemptId = _attemptStore.NextAttempt(_metadata.GameId, levelKey);
            LevelPayloadData data = _level.Start(levelId, levelType, attemptId, isRetry);

            long nowMs = _clock.UtcNowMs;
            string json = TrackingPayloadBuilder.BuildLevelPayload(_config, _metadata, nowMs, data);
            EnqueuePayload(_config.LevelCollection, data.ActionName, nowMs, json, immediate: false);
        }

        private void EndLevelInternal(string actionType, string loseReason, IDictionary<string, object> finalResult, bool immediate)
        {
            LevelPayloadData data = _level.End(actionType, loseReason, finalResult);
            if (data == null)
            {
                return;
            }

            _pausedByFocusLoss = false;
            long nowMs = _clock.UtcNowMs;
            string json = TrackingPayloadBuilder.BuildLevelPayload(_config, _metadata, nowMs, data);
            EnqueuePayload(_config.LevelCollection, data.ActionName, nowMs, json, immediate: immediate);
        }

        private void EndSessionInternal(bool immediate)
        {
            if (!_session.IsActive)
            {
                return;
            }

            _session.End();
            long nowMs = _clock.UtcNowMs;
            string json = TrackingPayloadBuilder.BuildSessionPayload(
                _config,
                _metadata,
                nowMs,
                "end",
                _session.StartTimeMs,
                _session.EndTimeMs,
                _session.ActiveDurationSec);

            EnqueuePayload(_config.SessionCollection, "end", nowMs, json, immediate: immediate);
        }

        private void EnqueuePayload(string collection, string actionName, long timestampMs, string json, bool immediate)
        {
            var envelope = new TrackingEnvelope
            {
                Collection = collection ?? string.Empty,
                ActionName = actionName ?? string.Empty,
                JsonBody = json ?? "{}",
                EnqueuedAtMs = timestampMs,
                NextAttemptAtMs = timestampMs
            };

            RecordHistory(collection, actionName, timestampMs, envelope.JsonBody);

            if (immediate)
            {
                _queue.SendImmediate(envelope);
            }
            else
            {
                _queue.Enqueue(envelope);
            }
        }

        private void RecordHistory(string collection, string actionName, long timestampMs, string json)
        {
            if (_config.KeepRecentHistoryCount <= 0)
            {
                return;
            }

            var entry = new CapturedLogEntry
            {
                Collection = collection,
                ActionName = actionName,
                TimestampMs = timestampMs,
                JsonBody = json
            };

            lock (_historyLock)
            {
                _recentLogs.Add(entry);
                while (_recentLogs.Count > _config.KeepRecentHistoryCount)
                {
                    _recentLogs.RemoveAt(0);
                }
            }

            Action<CapturedLogEntry> handler = OnPayloadCaptured;
            if (handler != null)
            {
                handler(entry);
            }
        }
    }
}
