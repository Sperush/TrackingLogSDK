using System;
using System.Collections.Generic;

namespace TrackingLogSDK
{
    /// <summary>
    /// Snapshot data for a level_track event.
    /// </summary>
    public sealed class LevelPayloadData
    {
        public int AttemptId;
        public object LevelId;
        public long StartTimeMs;
        public long? EndTimeMs;
        public float DurationSec;
        public string ActionName;
        public string ActionType;
        public string LoseReason;
        public string LevelType;
        public IReadOnlyList<LevelSubEvent> Events;
        public IDictionary<string, object> Result;
    }

    /// <summary>
    /// Builds standardized JSON payloads for session_track, level_track, and custom events.
    /// </summary>
    public static class TrackingPayloadBuilder
    {
        public static string BuildSessionPayload(
            TrackingConfig config,
            TrackingMetadata metadata,
            long timestampMs,
            string actionName,
            long startTimeMs,
            long? endTimeMs,
            float durationSec)
        {
            string fallbackCollection = config != null ? config.SessionCollection : "session_track";
            string collection = ResolveCollection(config, fallbackCollection);
            string action = config != null && !string.IsNullOrEmpty(config.SessionAction)
                ? config.SessionAction
                : "session_track";

            return BuildPayload(config, metadata, timestampMs, collection, action, writer =>
            {
                writer.WriteProperty("action_name", actionName ?? string.Empty);
                writer.WriteProperty("start_time", startTimeMs);
                writer.WriteProperty("end_time", endTimeMs);
                writer.WriteProperty("duration", durationSec, 3);
            });
        }

        public static string BuildLevelPayload(
            TrackingConfig config,
            TrackingMetadata metadata,
            long timestampMs,
            LevelPayloadData data)
        {
            string fallbackCollection = config != null ? config.LevelCollection : "level_track";
            string collection = ResolveCollection(config, fallbackCollection);
            string action = config != null && !string.IsNullOrEmpty(config.LevelAction)
                ? config.LevelAction
                : "level_track";

            return BuildPayload(config, metadata, timestampMs, collection, action, writer =>
            {
                writer.WriteProperty("attempt_id", data.AttemptId);
                writer.WritePropertyValue("level_id", data.LevelId ?? string.Empty);
                writer.WriteProperty("start_time", data.StartTimeMs);
                writer.WriteProperty("end_time", data.EndTimeMs);
                writer.WriteProperty("duration", data.DurationSec, 3);
                writer.WriteProperty("action_name", data.ActionName ?? string.Empty);

                if (data.ActionType != null)
                {
                    writer.WriteProperty("action_type", data.ActionType);
                }
                else
                {
                    writer.WriteKey("action_type");
                    writer.WriteNull();
                }

                if (data.LoseReason != null)
                {
                    writer.WriteProperty("lose_reason", data.LoseReason);
                }
                else
                {
                    writer.WriteKey("lose_reason");
                    writer.WriteNull();
                }

                writer.WriteProperty("level_type", data.LevelType ?? string.Empty);

                writer.WriteKey("events");
                writer.BeginArray();
                if (data.Events != null)
                {
                    for (int i = 0; i < data.Events.Count; i++)
                    {
                        data.Events[i].WriteJson(writer);
                    }
                }
                writer.EndArray();

                writer.WriteKey("result");
                writer.BeginObject();
                if (data.Result != null)
                {
                    foreach (KeyValuePair<string, object> kv in data.Result)
                    {
                        writer.WritePropertyValue(kv.Key, kv.Value);
                    }
                }
                writer.EndObject();
            });
        }

        public static string BuildCustomPayload(
            TrackingConfig config,
            TrackingMetadata metadata,
            long timestampMs,
            string collection,
            IDictionary<string, object> fields,
            string action = null)
        {
            string resolvedCollection = ResolveCollection(config, collection);
            string resolvedAction = !string.IsNullOrEmpty(action) ? action : collection;

            return BuildPayload(config, metadata, timestampMs, resolvedCollection, resolvedAction, writer =>
            {
                if (fields == null)
                {
                    return;
                }

                foreach (KeyValuePair<string, object> kv in fields)
                {
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        writer.WritePropertyValue(kv.Key, kv.Value);
                    }
                }
            });
        }

        private static string ResolveCollection(TrackingConfig config, string fallbackCollection)
        {
            if (config != null && !string.IsNullOrEmpty(config.Collection))
            {
                return config.Collection;
            }
            return fallbackCollection ?? string.Empty;
        }

        private static string BuildPayload(
            TrackingConfig config,
            TrackingMetadata metadata,
            long timestampMs,
            string collection,
            string action,
            Action<TrackingJsonWriter> writeBody)
        {
            var writer = new TrackingJsonWriter();
            bool includeCollection = config == null || config.IncludeCollectionField;
            bool includeAction = config == null || config.IncludeActionField;
            bool wrapped = config != null && config.PayloadFormat == PayloadFormat.Wrapped;
            string wrappedField = config != null && !string.IsNullOrEmpty(config.WrappedDataField)
                ? config.WrappedDataField
                : "data";

            writer.BeginObject();

            if (includeCollection && !string.IsNullOrEmpty(collection))
            {
                writer.WriteProperty("collection", collection);
            }

            if (includeAction && !string.IsNullOrEmpty(action))
            {
                writer.WriteProperty("action", action);
            }

            WriteCommonMetadata(writer, config, metadata, timestampMs);

            if (wrapped)
            {
                writer.WriteKey(wrappedField);
                writer.BeginObject();
                writeBody(writer);
                writer.EndObject();
            }
            else
            {
                writeBody(writer);
            }

            writer.EndObject();
            return writer.ToString();
        }

        private static void WriteCommonMetadata(
            TrackingJsonWriter writer,
            TrackingConfig config,
            TrackingMetadata metadata,
            long timestampMs)
        {
            TrackingMetadata meta = metadata ?? new TrackingMetadata();
            bool includeUser = config == null || config.IncludeUserField;

            writer.WriteProperty("game_id", meta.GameId ?? string.Empty);
            if (includeUser)
            {
                writer.WriteProperty("user", meta.UserId ?? string.Empty);
            }
            writer.WriteProperty("session_id", meta.SessionId ?? string.Empty);
            writer.WriteProperty("user_id", meta.UserId ?? string.Empty);
            writer.WriteProperty("timestamp", timestampMs);
            writer.WriteProperty("game_version", meta.GameVersion ?? string.Empty);
            writer.WriteProperty("build_id", meta.BuildId ?? string.Empty);
            writer.WriteProperty("platform", meta.Platform ?? string.Empty);
            writer.WriteProperty("browser", meta.Browser ?? string.Empty);
            writer.WriteProperty("device_model", meta.DeviceModel ?? string.Empty);
            writer.WriteProperty("os_version", meta.OsVersion ?? string.Empty);

            if (meta.CustomMetadata != null)
            {
                foreach (KeyValuePair<string, object> kv in meta.CustomMetadata)
                {
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        writer.WritePropertyValue(kv.Key, kv.Value);
                    }
                }
            }
        }
    }
}
