using System;
using System.Collections.Generic;

namespace TrackingLogSDK
{
    /// <summary>
    /// Common metadata automatically attached to every event sent by the SDK.
    /// </summary>
    public sealed class TrackingMetadata
    {
        public string SessionId = string.Empty;
        public string UserId = string.Empty;
        public string GameId = string.Empty;
        public string GameVersion = string.Empty;
        public string BuildId = string.Empty;
        public string Platform = string.Empty;
        public string Browser = string.Empty;
        public string DeviceModel = string.Empty;
        public string OsVersion = string.Empty;

        public readonly Dictionary<string, object> CustomMetadata =
            new Dictionary<string, object>(StringComparer.Ordinal);

        public TrackingMetadata Clone()
        {
            var copy = new TrackingMetadata
            {
                SessionId = SessionId,
                UserId = UserId,
                GameId = GameId,
                GameVersion = GameVersion,
                BuildId = BuildId,
                Platform = Platform,
                Browser = Browser,
                DeviceModel = DeviceModel,
                OsVersion = OsVersion
            };

            foreach (KeyValuePair<string, object> kv in CustomMetadata)
            {
                copy.CustomMetadata[kv.Key] = kv.Value;
            }

            return copy;
        }
    }
}
