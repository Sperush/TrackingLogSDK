using System;

namespace TrackingLogSDK
{
    /// <summary>
    /// Queued telemetry message ready for transport delivery.
    /// </summary>
    public sealed class TrackingEnvelope
    {
        public string Id;
        public string Collection;
        public string ActionName;
        public string JsonBody;
        public long EnqueuedAtMs;
        public int RetryCount;
        public long NextAttemptAtMs;

        public TrackingEnvelope()
        {
            Id = Guid.NewGuid().ToString("N");
        }
    }
}
