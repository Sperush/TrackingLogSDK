using System;

namespace TrackingLogSDK
{
    /// <summary>
    /// Tracks the active play session lifecycle and active duration excluding background time.
    /// </summary>
    public sealed class SessionTracker
    {
        private readonly IClock _clock;

        public string SessionId { get; private set; }
        public long StartTimeMs { get; private set; }
        public long? EndTimeMs { get; private set; }
        public float ActiveDurationSec { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsInBackground { get; private set; }

        public SessionTracker(IClock clock = null)
        {
            _clock = clock ?? new SystemClock();
            SessionId = string.Empty;
        }

        public void Start()
        {
            SessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
            StartTimeMs = _clock.UtcNowMs;
            EndTimeMs = null;
            ActiveDurationSec = 0f;
            IsActive = true;
            IsInBackground = false;
        }

        public void Tick(float deltaSeconds)
        {
            if (!IsActive || IsInBackground || deltaSeconds <= 0f)
            {
                return;
            }

            ActiveDurationSec += deltaSeconds;
        }

        public void SetBackground(bool inBackground)
        {
            IsInBackground = inBackground;
        }

        public void End()
        {
            if (!IsActive)
            {
                return;
            }

            EndTimeMs = _clock.UtcNowMs;
            IsActive = false;
        }
    }
}
