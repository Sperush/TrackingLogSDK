using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrackingLogSDK
{
    /// <summary>
    /// Serializable header entry for Inspector configuration.
    /// </summary>
    [Serializable]
    public struct HeaderEntry
    {
        public string Key;
        public string Value;
    }

    /// <summary>
    /// ScriptableObject asset for configuring Tracking Log SDK in the Unity Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Tracking Log SDK/Config", fileName = "TrackingLogConfig")]
    public sealed class TrackingConfigAsset : ScriptableObject
    {
        [Header("Endpoint & Authentication")]
        public string EndpointUrl = "";
        public string ApiKey = "";
        public string ApiKeyHeader = "X-API-Key";
        public string BearerToken = "";
        public List<HeaderEntry> CustomHeaders = new List<HeaderEntry>();
        [Min(1)]
        public int RequestTimeoutSeconds = 10;

        [Header("Application Metadata")]
        public string GameId = "";
        public string GameVersion = "";
        public string BuildId = "";
        [Tooltip("Leave empty to auto-detect platform.")]
        public string PlatformOverride = "";
        [Tooltip("Leave empty to auto-detect browser on WebGL.")]
        public string BrowserOverride = "";

        [Header("Collections & Format")]
        public string SessionCollection = "session_track";
        public string LevelCollection = "level_track";
        public bool IncludeCollectionField = true;
        public PayloadFormat PayloadFormat = PayloadFormat.Flat;
        public string WrappedDataField = "data";

        [Header("Tracking Behavior")]
        [Min(0.5f)]
        public float InProgressIntervalSeconds = 5f;
        [Tooltip("If true, in_progress sends only events since the previous in_progress tick; end always sends all run events.")]
        public bool InProgressDeltaEventsOnly = true;
        public bool AutoStartSession = true;
        public bool AutoPauseOnFocusLost = true;
        public bool AutoLogPauseEvents = false;
        public bool PersistAttemptCounts = true;
        [Min(1)]
        public int MaxQueueSize = 500;
        [Min(0)]
        public int MaxRetryCount = 5;
        [Min(0.5f)]
        public float InitialRetryDelaySeconds = 2f;
        public bool EnableOfflinePersistence = true;
        public string OfflineQueueFilePath = "";

        [Header("Transports & Debug")]
        public bool EnableHttpTransport = true;
        public bool EnableConsoleTransport = false;
        public bool EnableFileTransport = false;
        public string LogFilePath = "";
        [Min(0)]
        public int KeepRecentHistoryCount = 100;

        /// <summary>
        /// Converts this asset into a runtime TrackingConfig instance.
        /// </summary>
        public TrackingConfig ToConfig()
        {
            var config = new TrackingConfig
            {
                EndpointUrl = EndpointUrl ?? string.Empty,
                ApiKey = ApiKey ?? string.Empty,
                ApiKeyHeader = string.IsNullOrWhiteSpace(ApiKeyHeader) ? "X-API-Key" : ApiKeyHeader,
                BearerToken = BearerToken ?? string.Empty,
                RequestTimeoutSeconds = RequestTimeoutSeconds > 0 ? RequestTimeoutSeconds : 10,
                GameId = GameId ?? string.Empty,
                GameVersion = string.IsNullOrWhiteSpace(GameVersion) ? Application.version : GameVersion,
                BuildId = BuildId ?? string.Empty,
                PlatformOverride = PlatformOverride ?? string.Empty,
                BrowserOverride = BrowserOverride ?? string.Empty,
                SessionCollection = string.IsNullOrWhiteSpace(SessionCollection) ? "session_track" : SessionCollection,
                LevelCollection = string.IsNullOrWhiteSpace(LevelCollection) ? "level_track" : LevelCollection,
                IncludeCollectionField = IncludeCollectionField,
                PayloadFormat = PayloadFormat,
                WrappedDataField = string.IsNullOrWhiteSpace(WrappedDataField) ? "data" : WrappedDataField,
                InProgressIntervalSeconds = InProgressIntervalSeconds > 0f ? InProgressIntervalSeconds : 5f,
                InProgressDeltaEventsOnly = InProgressDeltaEventsOnly,
                AutoStartSession = AutoStartSession,
                AutoPauseOnFocusLost = AutoPauseOnFocusLost,
                AutoLogPauseEvents = AutoLogPauseEvents,
                PersistAttemptCounts = PersistAttemptCounts,
                MaxQueueSize = MaxQueueSize > 0 ? MaxQueueSize : 500,
                MaxRetryCount = MaxRetryCount >= 0 ? MaxRetryCount : 5,
                InitialRetryDelaySeconds = InitialRetryDelaySeconds > 0f ? InitialRetryDelaySeconds : 2f,
                EnableOfflinePersistence = EnableOfflinePersistence,
                OfflineQueueFilePath = OfflineQueueFilePath ?? string.Empty,
                EnableHttpTransport = EnableHttpTransport,
                EnableConsoleTransport = EnableConsoleTransport,
                EnableFileTransport = EnableFileTransport,
                LogFilePath = LogFilePath ?? string.Empty,
                KeepRecentHistoryCount = KeepRecentHistoryCount >= 0 ? KeepRecentHistoryCount : 100
            };

            if (CustomHeaders != null)
            {
                for (int i = 0; i < CustomHeaders.Count; i++)
                {
                    HeaderEntry entry = CustomHeaders[i];
                    if (!string.IsNullOrWhiteSpace(entry.Key))
                    {
                        config.CustomHeaders[entry.Key.Trim()] = entry.Value ?? string.Empty;
                    }
                }
            }

            return config;
        }
    }
}
