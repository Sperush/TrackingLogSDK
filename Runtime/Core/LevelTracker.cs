using System;
using System.Collections.Generic;

namespace TrackingLogSDK
{
    /// <summary>
    /// Tracks active level attempt state, active play duration, pause exclusions, component events, and result snapshots.
    /// </summary>
    public sealed class LevelTracker
    {
        private readonly IClock _clock;
        private readonly List<LevelSubEvent> _allEvents = new List<LevelSubEvent>();
        private readonly List<LevelSubEvent> _deltaEvents = new List<LevelSubEvent>();
        private readonly Dictionary<string, object> _persistentResult = new Dictionary<string, object>(StringComparer.Ordinal);

        private Func<IDictionary<string, object>> _resultProvider;
        private float _inProgressTimerSec;
        private long _pauseStartedAtMs;

        public bool IsActive { get; private set; }
        public bool IsPaused { get; private set; }
        public object LevelId { get; private set; }
        public string LevelKey { get; private set; }
        public string LevelType { get; private set; }
        public int AttemptId { get; private set; }
        public long StartTimeMs { get; private set; }
        public float ActiveDurationSec { get; private set; }
        public float TotalRunDurationSec { get; private set; }
        public int BufferedEventCount { get { return _allEvents.Count; } }

        public LevelTracker(IClock clock = null)
        {
            _clock = clock ?? new SystemClock();
        }

        public LevelPayloadData Start(object levelId, string levelType, int attemptId, bool isRetry)
        {
            LevelId = levelId ?? string.Empty;
            LevelKey = Convert.ToString(LevelId, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            LevelType = levelType ?? string.Empty;
            AttemptId = attemptId;
            StartTimeMs = _clock.UtcNowMs;
            ActiveDurationSec = 0f;
            TotalRunDurationSec = 0f;
            _inProgressTimerSec = 0f;
            _pauseStartedAtMs = 0L;
            IsActive = true;
            IsPaused = false;

            _allEvents.Clear();
            _deltaEvents.Clear();
            _persistentResult.Clear();
            _resultProvider = null;

            return new LevelPayloadData
            {
                AttemptId = AttemptId,
                LevelId = LevelId,
                StartTimeMs = StartTimeMs,
                EndTimeMs = null,
                DurationSec = 0f,
                ActionName = isRetry ? "retry" : "start",
                ActionType = null,
                LoseReason = null,
                LevelType = LevelType,
                Events = Array.Empty<LevelSubEvent>(),
                Result = new Dictionary<string, object>(0)
            };
        }

        public void SetResultProvider(Func<IDictionary<string, object>> provider)
        {
            _resultProvider = provider;
        }

        public void SetResultValue(string key, object value)
        {
            if (!IsActive || string.IsNullOrEmpty(key))
            {
                return;
            }

            _persistentResult[key] = value;
        }

        public void RecordEvent(string eventName, string value)
        {
            if (!IsActive || string.IsNullOrEmpty(eventName))
            {
                return;
            }

            var subEvent = new LevelSubEvent(eventName, value ?? string.Empty, TotalRunDurationSec);
            _allEvents.Add(subEvent);
            _deltaEvents.Add(subEvent);
        }

        public void Pause(string reason, bool autoLogPauseEvent)
        {
            if (!IsActive || IsPaused)
            {
                return;
            }

            IsPaused = true;
            _pauseStartedAtMs = _clock.UtcNowMs;
            if (autoLogPauseEvent)
            {
                RecordEvent("pause", string.IsNullOrEmpty(reason) ? "menu" : reason);
            }
        }

        public void Resume(bool autoLogPauseEvent)
        {
            if (!IsActive || !IsPaused)
            {
                return;
            }

            double pauseDuration = Math.Max(0.0, (_clock.UtcNowMs - _pauseStartedAtMs) / 1000.0);
            IsPaused = false;
            if (autoLogPauseEvent)
            {
                RecordEvent("resume", pauseDuration.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Resets segment start_time and duration when resuming an attempt after returning to the app.
        /// </summary>
        public void ResumeFromAppReturn()
        {
            if (!IsActive)
            {
                return;
            }

            StartTimeMs = _clock.UtcNowMs;
            ActiveDurationSec = 0f;
            _inProgressTimerSec = 0f;
        }

        public bool Tick(float deltaSeconds, float intervalSeconds, bool deltaEventsOnly, out LevelPayloadData inProgressPayload)
        {
            inProgressPayload = null;

            if (!IsActive || IsPaused || deltaSeconds <= 0f)
            {
                return false;
            }

            ActiveDurationSec += deltaSeconds;
            TotalRunDurationSec += deltaSeconds;

            if (intervalSeconds <= 0f)
            {
                return false;
            }

            _inProgressTimerSec += deltaSeconds;
            if (_inProgressTimerSec < intervalSeconds)
            {
                return false;
            }

            while (_inProgressTimerSec >= intervalSeconds)
            {
                _inProgressTimerSec -= intervalSeconds;
            }

            List<LevelSubEvent> eventsSnapshot = deltaEventsOnly
                ? new List<LevelSubEvent>(_deltaEvents)
                : new List<LevelSubEvent>(_allEvents);

            if (deltaEventsOnly)
            {
                _deltaEvents.Clear();
            }

            inProgressPayload = new LevelPayloadData
            {
                AttemptId = AttemptId,
                LevelId = LevelId,
                StartTimeMs = StartTimeMs,
                EndTimeMs = _clock.UtcNowMs,
                DurationSec = ActiveDurationSec,
                ActionName = "in_progress",
                ActionType = null,
                LoseReason = null,
                LevelType = LevelType,
                Events = eventsSnapshot,
                Result = BuildMergedResult(null)
            };

            return true;
        }

        public LevelPayloadData End(string actionType, string loseReason, IDictionary<string, object> finalResult)
        {
            if (!IsActive)
            {
                return null;
            }

            string normalizedActionType = string.IsNullOrEmpty(actionType) ? "quit_level" : actionType;
            string normalizedLoseReason = string.Equals(normalizedActionType, "lose", StringComparison.Ordinal)
                ? loseReason
                : null;

            var payload = new LevelPayloadData
            {
                AttemptId = AttemptId,
                LevelId = LevelId,
                StartTimeMs = StartTimeMs,
                EndTimeMs = _clock.UtcNowMs,
                DurationSec = ActiveDurationSec,
                ActionName = "end",
                ActionType = normalizedActionType,
                LoseReason = normalizedLoseReason,
                LevelType = LevelType,
                Events = new List<LevelSubEvent>(_allEvents),
                Result = BuildMergedResult(finalResult)
            };

            IsActive = false;
            IsPaused = false;
            _allEvents.Clear();
            _deltaEvents.Clear();
            _persistentResult.Clear();
            _resultProvider = null;

            return payload;
        }

        private Dictionary<string, object> BuildMergedResult(IDictionary<string, object> overrideResult)
        {
            var merged = new Dictionary<string, object>(StringComparer.Ordinal);

            if (_resultProvider != null)
            {
                IDictionary<string, object> provided = _resultProvider();
                if (provided != null)
                {
                    foreach (KeyValuePair<string, object> kv in provided)
                    {
                        if (!string.IsNullOrEmpty(kv.Key))
                        {
                            merged[kv.Key] = kv.Value;
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, object> kv in _persistentResult)
            {
                merged[kv.Key] = kv.Value;
            }

            if (overrideResult != null)
            {
                foreach (KeyValuePair<string, object> kv in overrideResult)
                {
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        merged[kv.Key] = kv.Value;
                    }
                }
            }

            return merged;
        }
    }
}
