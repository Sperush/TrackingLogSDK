using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TrackingLogSDK
{
    /// <summary>
    /// Zero-dependency, C# 7.3 / IL2CPP / WebGL-safe JSON serializer and parser for TrackingConfig.
    /// </summary>
    public static class TrackingConfigSerializer
    {
        public static string CreateTemplateJson(string gameId = "physic_game", string collection = "B1_Quang")
        {
            var cfg = new TrackingConfig
            {
                EndpointUrl = TrackingConfig.DefaultEndpointUrl,
                GameId = gameId ?? string.Empty,
                Collection = collection ?? string.Empty,
                BuildId = "build_01",
                EnableHttpTransport = true,
                EnableConsoleTransport = false
            };
            return ToJson(cfg, prettyPrint: true);
        }

        public static string ToJson(TrackingConfig config, bool prettyPrint = true)
        {
            TrackingConfig cfg = config ?? new TrackingConfig();
            var sb = new StringBuilder(1024);
            string nl = prettyPrint ? "\n" : string.Empty;
            string ind = prettyPrint ? "  " : string.Empty;
            string sp = prettyPrint ? " " : string.Empty;

            sb.Append('{').Append(nl);
            AppendStringProp(sb, ind, sp, "endpoint_url", cfg.EndpointUrl ?? string.Empty, true, nl);
            AppendStringProp(sb, ind, sp, "api_key", cfg.ApiKey ?? string.Empty, true, nl);
            AppendStringProp(sb, ind, sp, "api_key_header", cfg.ApiKeyHeader ?? "X-API-Key", true, nl);
            AppendStringProp(sb, ind, sp, "bearer_token", cfg.BearerToken ?? string.Empty, true, nl);
            AppendIntProp(sb, ind, sp, "request_timeout_seconds", cfg.RequestTimeoutSeconds, true, nl);

            AppendStringProp(sb, ind, sp, "game_id", cfg.GameId ?? string.Empty, true, nl);
            AppendStringProp(sb, ind, sp, "collection", cfg.Collection ?? string.Empty, true, nl);
            AppendStringProp(sb, ind, sp, "game_version", cfg.GameVersion ?? string.Empty, true, nl);
            AppendStringProp(sb, ind, sp, "build_id", cfg.BuildId ?? string.Empty, true, nl);
            AppendStringProp(sb, ind, sp, "platform_override", cfg.PlatformOverride ?? string.Empty, true, nl);
            AppendStringProp(sb, ind, sp, "browser_override", cfg.BrowserOverride ?? string.Empty, true, nl);

            AppendStringProp(sb, ind, sp, "session_action", cfg.SessionAction ?? "session_track", true, nl);
            AppendStringProp(sb, ind, sp, "level_action", cfg.LevelAction ?? "level_track", true, nl);
            AppendStringProp(sb, ind, sp, "session_collection", cfg.SessionCollection ?? "session_track", true, nl);
            AppendStringProp(sb, ind, sp, "level_collection", cfg.LevelCollection ?? "level_track", true, nl);
            AppendBoolProp(sb, ind, sp, "include_collection_field", cfg.IncludeCollectionField, true, nl);
            AppendBoolProp(sb, ind, sp, "include_action_field", cfg.IncludeActionField, true, nl);
            AppendBoolProp(sb, ind, sp, "include_user_field", cfg.IncludeUserField, true, nl);
            AppendStringProp(sb, ind, sp, "payload_format", cfg.PayloadFormat.ToString(), true, nl);
            AppendStringProp(sb, ind, sp, "wrapped_data_field", cfg.WrappedDataField ?? "data", true, nl);

            AppendBoolProp(sb, ind, sp, "auto_generate_anonymous_user_id", cfg.AutoGenerateAnonymousUserId, true, nl);
            AppendFloatProp(sb, ind, sp, "in_progress_interval_seconds", cfg.InProgressIntervalSeconds, true, nl);
            AppendBoolProp(sb, ind, sp, "in_progress_delta_events_only", cfg.InProgressDeltaEventsOnly, true, nl);
            AppendBoolProp(sb, ind, sp, "auto_start_session", cfg.AutoStartSession, true, nl);
            AppendBoolProp(sb, ind, sp, "auto_pause_on_focus_lost", cfg.AutoPauseOnFocusLost, true, nl);
            AppendBoolProp(sb, ind, sp, "auto_log_pause_events", cfg.AutoLogPauseEvents, true, nl);
            AppendBoolProp(sb, ind, sp, "persist_attempt_counts", cfg.PersistAttemptCounts, true, nl);
            AppendIntProp(sb, ind, sp, "max_queue_size", cfg.MaxQueueSize, true, nl);
            AppendIntProp(sb, ind, sp, "max_retry_count", cfg.MaxRetryCount, true, nl);
            AppendFloatProp(sb, ind, sp, "initial_retry_delay_seconds", cfg.InitialRetryDelaySeconds, true, nl);
            AppendBoolProp(sb, ind, sp, "enable_offline_persistence", cfg.EnableOfflinePersistence, true, nl);
            AppendStringProp(sb, ind, sp, "offline_queue_file_path", cfg.OfflineQueueFilePath ?? string.Empty, true, nl);

            AppendBoolProp(sb, ind, sp, "enable_http_transport", cfg.EnableHttpTransport, true, nl);
            AppendBoolProp(sb, ind, sp, "enable_console_transport", cfg.EnableConsoleTransport, true, nl);
            AppendBoolProp(sb, ind, sp, "enable_file_transport", cfg.EnableFileTransport, true, nl);
            AppendStringProp(sb, ind, sp, "log_file_path", cfg.LogFilePath ?? string.Empty, true, nl);
            AppendIntProp(sb, ind, sp, "keep_recent_history_count", cfg.KeepRecentHistoryCount, true, nl);

            sb.Append(ind).Append("\"custom_headers\":").Append(sp);
            AppendStringDict(sb, cfg.CustomHeaders, prettyPrint, ind);
            sb.Append(nl).Append('}');

            return sb.ToString();
        }

        public static TrackingConfig FromJson(string json, TrackingConfig baseConfig = null)
        {
            TrackingConfig cfg = baseConfig != null ? baseConfig.Clone() : new TrackingConfig();
            if (string.IsNullOrWhiteSpace(json))
            {
                return cfg;
            }

            var parser = new JsonObjectParser(json);
            Dictionary<string, object> root = parser.ParseRootObject();
            if (root == null)
            {
                return cfg;
            }

            ApplyString(root, "endpoint_url", v => { if (!string.IsNullOrWhiteSpace(v)) cfg.EndpointUrl = v.Trim(); });
            ApplyString(root, "api_key", v => cfg.ApiKey = v ?? string.Empty);
            ApplyString(root, "api_key_header", v => { if (!string.IsNullOrWhiteSpace(v)) cfg.ApiKeyHeader = v.Trim(); });
            ApplyString(root, "bearer_token", v => cfg.BearerToken = v ?? string.Empty);
            ApplyInt(root, "request_timeout_seconds", v => { if (v > 0) cfg.RequestTimeoutSeconds = v; });

            ApplyString(root, "game_id", v => cfg.GameId = v ?? string.Empty);
            ApplyString(root, "collection", v => cfg.Collection = v != null ? v.Trim() : string.Empty);
            ApplyString(root, "game_version", v => { if (v != null) cfg.GameVersion = v; });
            ApplyString(root, "build_id", v => cfg.BuildId = v ?? string.Empty);
            ApplyString(root, "platform_override", v => cfg.PlatformOverride = v ?? string.Empty);
            ApplyString(root, "browser_override", v => cfg.BrowserOverride = v ?? string.Empty);

            ApplyString(root, "session_action", v => { if (!string.IsNullOrWhiteSpace(v)) cfg.SessionAction = v.Trim(); });
            ApplyString(root, "level_action", v => { if (!string.IsNullOrWhiteSpace(v)) cfg.LevelAction = v.Trim(); });
            ApplyString(root, "session_collection", v => { if (!string.IsNullOrWhiteSpace(v)) cfg.SessionCollection = v.Trim(); });
            ApplyString(root, "level_collection", v => { if (!string.IsNullOrWhiteSpace(v)) cfg.LevelCollection = v.Trim(); });
            ApplyBool(root, "include_collection_field", v => cfg.IncludeCollectionField = v);
            ApplyBool(root, "include_action_field", v => cfg.IncludeActionField = v);
            ApplyBool(root, "include_user_field", v => cfg.IncludeUserField = v);
            ApplyString(root, "payload_format", v =>
            {
                if (!string.IsNullOrWhiteSpace(v) && Enum.TryParse(v.Trim(), true, out PayloadFormat parsed))
                {
                    cfg.PayloadFormat = parsed;
                }
            });
            ApplyString(root, "wrapped_data_field", v => { if (!string.IsNullOrWhiteSpace(v)) cfg.WrappedDataField = v.Trim(); });

            ApplyBool(root, "auto_generate_anonymous_user_id", v => cfg.AutoGenerateAnonymousUserId = v);
            ApplyFloat(root, "in_progress_interval_seconds", v => { if (v > 0f) cfg.InProgressIntervalSeconds = v; });
            ApplyBool(root, "in_progress_delta_events_only", v => cfg.InProgressDeltaEventsOnly = v);
            ApplyBool(root, "auto_start_session", v => cfg.AutoStartSession = v);
            ApplyBool(root, "auto_pause_on_focus_lost", v => cfg.AutoPauseOnFocusLost = v);
            ApplyBool(root, "auto_log_pause_events", v => cfg.AutoLogPauseEvents = v);
            ApplyBool(root, "persist_attempt_counts", v => cfg.PersistAttemptCounts = v);
            ApplyInt(root, "max_queue_size", v => { if (v > 0) cfg.MaxQueueSize = v; });
            ApplyInt(root, "max_retry_count", v => { if (v >= 0) cfg.MaxRetryCount = v; });
            ApplyFloat(root, "initial_retry_delay_seconds", v => { if (v > 0f) cfg.InitialRetryDelaySeconds = v; });
            ApplyBool(root, "enable_offline_persistence", v => cfg.EnableOfflinePersistence = v);
            ApplyString(root, "offline_queue_file_path", v => cfg.OfflineQueueFilePath = v ?? string.Empty);

            ApplyBool(root, "enable_http_transport", v => cfg.EnableHttpTransport = v);
            ApplyBool(root, "enable_console_transport", v => cfg.EnableConsoleTransport = v);
            ApplyBool(root, "enable_file_transport", v => cfg.EnableFileTransport = v);
            ApplyString(root, "log_file_path", v => cfg.LogFilePath = v ?? string.Empty);
            ApplyInt(root, "keep_recent_history_count", v => { if (v >= 0) cfg.KeepRecentHistoryCount = v; });

            if (root.TryGetValue("custom_headers", out object headersObj) && headersObj is Dictionary<string, object> headersDict)
            {
                foreach (KeyValuePair<string, object> kv in headersDict)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key) && kv.Value != null)
                    {
                        cfg.CustomHeaders[kv.Key.Trim()] = Convert.ToString(kv.Value, CultureInfo.InvariantCulture) ?? string.Empty;
                    }
                }
            }

            return cfg;
        }

        private static void ApplyString(Dictionary<string, object> dict, string key, Action<string> setter)
        {
            if (dict.TryGetValue(key, out object val) && val != null)
            {
                setter(Convert.ToString(val, CultureInfo.InvariantCulture));
            }
        }

        private static void ApplyBool(Dictionary<string, object> dict, string key, Action<bool> setter)
        {
            if (dict.TryGetValue(key, out object val) && val is bool)
            {
                setter((bool)val);
            }
        }

        private static void ApplyInt(Dictionary<string, object> dict, string key, Action<int> setter)
        {
            if (!dict.TryGetValue(key, out object val) || val == null)
            {
                return;
            }
            if (val is long) setter((int)(long)val);
            else if (val is int) setter((int)val);
            else if (val is double) setter((int)(double)val);
        }

        private static void ApplyFloat(Dictionary<string, object> dict, string key, Action<float> setter)
        {
            if (!dict.TryGetValue(key, out object val) || val == null)
            {
                return;
            }
            if (val is double) setter((float)(double)val);
            else if (val is long) setter((long)val);
            else if (val is int) setter((int)val);
        }

        private static void AppendStringProp(StringBuilder sb, string ind, string sp, string key, string value, bool comma, string nl)
        {
            sb.Append(ind).Append('"').Append(key).Append("\":").Append(sp);
            AppendEscapedString(sb, value);
            if (comma) sb.Append(',');
            sb.Append(nl);
        }

        private static void AppendIntProp(StringBuilder sb, string ind, string sp, string key, int value, bool comma, string nl)
        {
            sb.Append(ind).Append('"').Append(key).Append("\":").Append(sp)
              .Append(value.ToString(CultureInfo.InvariantCulture));
            if (comma) sb.Append(',');
            sb.Append(nl);
        }

        private static void AppendFloatProp(StringBuilder sb, string ind, string sp, string key, float value, bool comma, string nl)
        {
            float safe = float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
            sb.Append(ind).Append('"').Append(key).Append("\":").Append(sp)
              .Append(safe.ToString("G9", CultureInfo.InvariantCulture));
            if (comma) sb.Append(',');
            sb.Append(nl);
        }

        private static void AppendBoolProp(StringBuilder sb, string ind, string sp, string key, bool value, bool comma, string nl)
        {
            sb.Append(ind).Append('"').Append(key).Append("\":").Append(sp)
              .Append(value ? "true" : "false");
            if (comma) sb.Append(',');
            sb.Append(nl);
        }

        private static void AppendStringDict(StringBuilder sb, Dictionary<string, string> dict, bool prettyPrint, string ind)
        {
            if (dict == null || dict.Count == 0)
            {
                sb.Append("{}");
                return;
            }

            string nl = prettyPrint ? "\n" : string.Empty;
            string innerInd = prettyPrint ? ind + ind : string.Empty;
            string sp = prettyPrint ? " " : string.Empty;

            sb.Append('{').Append(nl);
            int idx = 0;
            foreach (KeyValuePair<string, string> kv in dict)
            {
                idx++;
                sb.Append(innerInd);
                AppendEscapedString(sb, kv.Key ?? string.Empty);
                sb.Append(':').Append(sp);
                AppendEscapedString(sb, kv.Value ?? string.Empty);
                if (idx < dict.Count) sb.Append(',');
                sb.Append(nl);
            }
            sb.Append(ind).Append('}');
        }

        private static void AppendEscapedString(StringBuilder sb, string value)
        {
            sb.Append('"');
            if (value != null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < ' ')
                            {
                                sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                sb.Append(c);
                            }
                            break;
                    }
                }
            }
            sb.Append('"');
        }

        private sealed class JsonObjectParser
        {
            private readonly string _json;
            private int _pos;

            public JsonObjectParser(string json)
            {
                _json = json ?? string.Empty;
            }

            public Dictionary<string, object> ParseRootObject()
            {
                SkipWhitespace();
                if (_pos >= _json.Length || _json[_pos] != '{')
                {
                    return null;
                }
                return ParseObject();
            }

            private Dictionary<string, object> ParseObject()
            {
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                _pos++; // '{'
                SkipWhitespace();
                if (_pos < _json.Length && _json[_pos] == '}')
                {
                    _pos++;
                    return dict;
                }

                while (_pos < _json.Length)
                {
                    SkipWhitespace();
                    if (_pos >= _json.Length || _json[_pos] != '"')
                    {
                        break;
                    }

                    string key = ParseString();
                    SkipWhitespace();
                    if (_pos >= _json.Length || _json[_pos] != ':')
                    {
                        break;
                    }
                    _pos++; // ':'

                    object value = ParseValue();
                    dict[key] = value;

                    SkipWhitespace();
                    if (_pos < _json.Length && _json[_pos] == ',')
                    {
                        _pos++;
                        continue;
                    }
                    if (_pos < _json.Length && _json[_pos] == '}')
                    {
                        _pos++;
                        break;
                    }
                    break;
                }

                return dict;
            }

            private List<object> ParseArray()
            {
                var list = new List<object>();
                _pos++; // '['
                SkipWhitespace();
                if (_pos < _json.Length && _json[_pos] == ']')
                {
                    _pos++;
                    return list;
                }

                while (_pos < _json.Length)
                {
                    list.Add(ParseValue());
                    SkipWhitespace();
                    if (_pos < _json.Length && _json[_pos] == ',')
                    {
                        _pos++;
                        continue;
                    }
                    if (_pos < _json.Length && _json[_pos] == ']')
                    {
                        _pos++;
                        break;
                    }
                    break;
                }

                return list;
            }

            private object ParseValue()
            {
                SkipWhitespace();
                if (_pos >= _json.Length)
                {
                    return null;
                }

                char c = _json[_pos];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't':
                        if (MatchLiteral("true")) return true;
                        return null;
                    case 'f':
                        if (MatchLiteral("false")) return false;
                        return null;
                    case 'n':
                        MatchLiteral("null");
                        return null;
                    default:
                        return ParseNumber();
                }
            }

            private string ParseString()
            {
                _pos++; // '"'
                var sb = new StringBuilder();
                while (_pos < _json.Length)
                {
                    char c = _json[_pos++];
                    if (c == '"')
                    {
                        return sb.ToString();
                    }
                    if (c == '\\' && _pos < _json.Length)
                    {
                        char esc = _json[_pos++];
                        switch (esc)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (_pos + 4 <= _json.Length)
                                {
                                    string hex = _json.Substring(_pos, 4);
                                    if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                                    {
                                        sb.Append((char)code);
                                    }
                                    _pos += 4;
                                }
                                break;
                            default:
                                sb.Append(esc);
                                break;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }

            private object ParseNumber()
            {
                int start = _pos;
                bool isFloat = false;
                while (_pos < _json.Length)
                {
                    char c = _json[_pos];
                    if (char.IsDigit(c) || c == '-' || c == '+')
                    {
                        _pos++;
                    }
                    else if (c == '.' || c == 'e' || c == 'E')
                    {
                        isFloat = true;
                        _pos++;
                    }
                    else
                    {
                        break;
                    }
                }

                string raw = _json.Substring(start, _pos - start);
                if (isFloat && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                {
                    return d;
                }
                if (!isFloat && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                {
                    return l;
                }
                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double fallback))
                {
                    return fallback;
                }
                return 0L;
            }

            private bool MatchLiteral(string literal)
            {
                if (_pos + literal.Length > _json.Length)
                {
                    return false;
                }
                if (string.Compare(_json, _pos, literal, 0, literal.Length, StringComparison.Ordinal) == 0)
                {
                    _pos += literal.Length;
                    return true;
                }
                return false;
            }

            private void SkipWhitespace()
            {
                while (_pos < _json.Length && char.IsWhiteSpace(_json[_pos]))
                {
                    _pos++;
                }
            }
        }
    }
}
