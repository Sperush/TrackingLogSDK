using System;

namespace TrackingLogSDK
{
    /// <summary>
    /// Pluggable transport abstraction for dispatching serialized telemetry payloads.
    /// </summary>
    public interface ILogTransport
    {
        string Name { get; }

        /// <summary>
        /// Asynchronously sends an envelope and invokes onComplete with the delivery result.
        /// </summary>
        void Send(TrackingEnvelope envelope, Action<TransportResult> onComplete);

        /// <summary>
        /// Best-effort synchronous/keepalive send used when the app or WebGL tab is closing.
        /// Returns true if handed off to the OS/browser successfully.
        /// </summary>
        bool TrySendImmediate(TrackingEnvelope envelope);
    }
}
