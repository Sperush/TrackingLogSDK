using System;

namespace TrackingLogSDK
{
    /// <summary>
    /// Abstraction over wall-clock UTC time in Unix milliseconds for testability.
    /// </summary>
    public interface IClock
    {
        /// <summary>
        /// Current UTC time in Unix epoch milliseconds.
        /// </summary>
        long UtcNowMs { get; }
    }

    /// <summary>
    /// Default clock backed by DateTimeOffset.UtcNow.
    /// </summary>
    public sealed class SystemClock : IClock
    {
        public long UtcNowMs
        {
            get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }
        }
    }
}
