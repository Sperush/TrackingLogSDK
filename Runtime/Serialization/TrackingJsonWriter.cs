using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TrackingLogSDK
{
    /// <summary>
    /// Zero-dependency, IL2CPP and WebGL-safe JSON writer using InvariantCulture.
    /// </summary>
    public sealed class TrackingJsonWriter
    {
        private readonly StringBuilder _sb = new StringBuilder(512);
        private readonly Stack<bool> _needsComma = new Stack<bool>();

        public void BeginObject()
        {
            WriteValuePrefix();
            _sb.Append('{');
            _needsComma.Push(false);
        }

        public void EndObject()
        {
            _needsComma.Pop();
            _sb.Append('}');
        }

        public void BeginArray()
        {
            WriteValuePrefix();
            _sb.Append('[');
            _needsComma.Push(false);
        }

        public void EndArray()
        {
            _needsComma.Pop();
            _sb.Append(']');
        }

        public void WriteKey(string key)
        {
            if (_needsComma.Count > 0 && _needsComma.Peek())
            {
                _sb.Append(',');
            }
            if (_needsComma.Count > 0)
            {
                _needsComma.Pop();
                _needsComma.Push(false);
            }

            WriteEscapedString(key ?? string.Empty);
            _sb.Append(':');
        }

        public void WriteProperty(string key, string value)
        {
            WriteKey(key);
            WriteString(value);
        }

        public void WriteProperty(string key, int value)
        {
            WriteKey(key);
            WriteInt(value);
        }

        public void WriteProperty(string key, long value)
        {
            WriteKey(key);
            WriteLong(value);
        }

        public void WriteProperty(string key, long? value)
        {
            WriteKey(key);
            if (value.HasValue)
            {
                WriteLong(value.Value);
            }
            else
            {
                WriteNull();
            }
        }

        public void WriteProperty(string key, float value, int decimals = -1)
        {
            WriteKey(key);
            WriteFloat(value, decimals);
        }

        public void WriteProperty(string key, double value, int decimals = -1)
        {
            WriteKey(key);
            WriteDouble(value, decimals);
        }

        public void WriteProperty(string key, bool value)
        {
            WriteKey(key);
            WriteBool(value);
        }

        public void WritePropertyValue(string key, object value)
        {
            WriteKey(key);
            WriteValue(value);
        }

        public void WriteNull()
        {
            WriteValuePrefix();
            _sb.Append("null");
        }

        public void WriteBool(bool value)
        {
            WriteValuePrefix();
            _sb.Append(value ? "true" : "false");
        }

        public void WriteInt(int value)
        {
            WriteValuePrefix();
            _sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteLong(long value)
        {
            WriteValuePrefix();
            _sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteFloat(float value, int decimals = -1)
        {
            WriteValuePrefix();
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                _sb.Append('0');
                return;
            }

            double val = decimals >= 0 ? Math.Round(value, decimals, MidpointRounding.AwayFromZero) : value;
            _sb.Append(val.ToString("G9", CultureInfo.InvariantCulture));
        }

        public void WriteDouble(double value, int decimals = -1)
        {
            WriteValuePrefix();
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                _sb.Append('0');
                return;
            }

            double val = decimals >= 0 ? Math.Round(value, decimals, MidpointRounding.AwayFromZero) : value;
            _sb.Append(val.ToString("G15", CultureInfo.InvariantCulture));
        }

        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteNull();
                return;
            }

            WriteValuePrefix();
            WriteEscapedString(value);
        }

        public void WriteValue(object value)
        {
            if (value == null)
            {
                WriteNull();
                return;
            }

            var serializable = value as ITrackingSerializable;
            if (serializable != null)
            {
                serializable.WriteJson(this);
                return;
            }

            var str = value as string;
            if (str != null)
            {
                WriteString(str);
                return;
            }

            if (value is bool)
            {
                WriteBool((bool)value);
                return;
            }
            if (value is int)
            {
                WriteInt((int)value);
                return;
            }
            if (value is long)
            {
                WriteLong((long)value);
                return;
            }
            if (value is short)
            {
                WriteInt((short)value);
                return;
            }
            if (value is byte)
            {
                WriteInt((byte)value);
                return;
            }
            if (value is uint)
            {
                WriteLong((uint)value);
                return;
            }
            if (value is float)
            {
                WriteFloat((float)value);
                return;
            }
            if (value is double)
            {
                WriteDouble((double)value);
                return;
            }
            if (value is decimal)
            {
                WriteValuePrefix();
                _sb.Append(((decimal)value).ToString(CultureInfo.InvariantCulture));
                return;
            }

            var typedDict = value as IDictionary<string, object>;
            if (typedDict != null)
            {
                BeginObject();
                foreach (KeyValuePair<string, object> kv in typedDict)
                {
                    WritePropertyValue(kv.Key, kv.Value);
                }
                EndObject();
                return;
            }

            var dict = value as IDictionary;
            if (dict != null)
            {
                BeginObject();
                foreach (DictionaryEntry entry in dict)
                {
                    WritePropertyValue(Convert.ToString(entry.Key, CultureInfo.InvariantCulture), entry.Value);
                }
                EndObject();
                return;
            }

            var enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                BeginArray();
                foreach (object item in enumerable)
                {
                    WriteValue(item);
                }
                EndArray();
                return;
            }

            if (value is Enum)
            {
                WriteString(value.ToString());
                return;
            }

            WriteString(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        public override string ToString()
        {
            return _sb.ToString();
        }

        private void WriteValuePrefix()
        {
            if (_needsComma.Count > 0)
            {
                if (_needsComma.Peek())
                {
                    _sb.Append(',');
                }
                else
                {
                    _needsComma.Pop();
                    _needsComma.Push(true);
                }
            }
        }

        private void WriteEscapedString(string value)
        {
            _sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"':
                        _sb.Append("\\\"");
                        break;
                    case '\\':
                        _sb.Append("\\\\");
                        break;
                    case '\b':
                        _sb.Append("\\b");
                        break;
                    case '\f':
                        _sb.Append("\\f");
                        break;
                    case '\n':
                        _sb.Append("\\n");
                        break;
                    case '\r':
                        _sb.Append("\\r");
                        break;
                    case '\t':
                        _sb.Append("\\t");
                        break;
                    default:
                        if (c < ' ')
                        {
                            _sb.Append("\\u");
                            _sb.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            _sb.Append(c);
                        }
                        break;
                }
            }
            _sb.Append('"');
        }
    }
}
