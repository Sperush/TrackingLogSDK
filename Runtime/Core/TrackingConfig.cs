using System;
using System.Collections.Generic;

namespace TrackingLogSDK
{
    /// <summary>
    /// Controls whether serialized JSON places collection/metadata fields at the root
    /// or wraps the event payload under a nested data object.
    /// </summary>
    public enum PayloadFormat
    {
        Flat = 0,
        Wrapped = 1
    }

    /// <summary>
    /// Runtime configuration for the Tracking Log SDK.
    /// </summary>
    [Serializable]
    public sealed class TrackingConfig
    {
        // Endpoint & Authentication
        public string EndpointUrl = string.Empty;
        public string ApiKey = string.Empty;
        public string ApiKeyHeader = "X-API-Key";
        public string BearerToken = string.Empty;
        public Dictionary<string, string> CustomHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public int RequestTimeoutSeconds = 10;

        // Application Metadata
        public string GameId = string.Empty;
        public string GameVersion = "1.0.0";
        public string BuildId = string.Empty;
        public string PlatformOverride = string.Empty;
        public string BrowserOverride = string.Empty;

        // Collections & Payload Layout
        public string SessionCollection = "session_track";
        public string LevelCollection = "level_track";
        public bool IncludeCollectionField = true;
        public PayloadFormat PayloadFormat = PayloadFormat.Flat;
        public string WrappedDataField = "data";

        // Tracking Behavior
        public float InProgressIntervalSeconds = 5f;
        public bool InProgressDeltaEventsOnly = true;
        public bool AutoStartSession = true;
        public bool AutoPauseOnFocusLost = true;
        public bool AutoLogPauseEvents = false;
        public bool PersistAttemptCounts = true;
        public int MaxQueueSize = 500;
        public int MaxRetryCount = 5;
        public float InitialRetryDelaySeconds = 2f;
        public bool EnableOfflinePersistence = true;
        public string OfflineQueueFilePath = string.Empty;

        // Built-in Transports & Diagnostics
        public bool EnableHttpTransport = true;
        public bool EnableConsoleTransport = false;
        public bool EnableFileTransport = false;
        public string LogFilePath = string.Empty;
        public int KeepRecentHistoryCount = 100;

        public TrackingConfig Clone()
        {
            var copy = (TrackingConfig)MemberwiseClone();
            copy.CustomHeaders = CustomHeaders != null
                ? new Dictionary<string, string>(CustomHeaders, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return copy;
        }
    }
}
