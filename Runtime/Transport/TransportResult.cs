namespace TrackingLogSDK
{
    /// <summary>
    /// Result returned by an ILogTransport after attempting to send an envelope.
    /// </summary>
    public readonly struct TransportResult
    {
        public readonly bool Success;
        public readonly long StatusCode;
        public readonly string Error;
        public readonly bool IsRetryable;

        public TransportResult(bool success, long statusCode, string error, bool isRetryable)
        {
            Success = success;
            StatusCode = statusCode;
            Error = error ?? string.Empty;
            IsRetryable = isRetryable;
        }

        public static TransportResult Ok(long statusCode = 200)
        {
            return new TransportResult(true, statusCode, string.Empty, false);
        }

        public static TransportResult Fail(long statusCode, string error, bool isRetryable)
        {
            return new TransportResult(false, statusCode, error, isRetryable);
        }
    }
}
