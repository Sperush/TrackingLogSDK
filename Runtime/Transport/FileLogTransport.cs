using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace TrackingLogSDK
{
    /// <summary>
    /// Appends serialized JSON payloads as JSON Lines (.jsonl) to disk on non-WebGL platforms.
    /// </summary>
    public sealed class FileLogTransport : ILogTransport
    {
        private readonly string _filePath;
        private readonly object _lock = new object();

        public string Name
        {
            get { return "File"; }
        }

        public string FilePath
        {
            get { return _filePath; }
        }

        public FileLogTransport(string filePath = null)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                _filePath = filePath;
            }
            else
            {
                _filePath = Path.Combine(Application.persistentDataPath, "tracking_logs.jsonl");
            }
        }

        public void Send(TrackingEnvelope envelope, Action<TransportResult> onComplete)
        {
            string error;
            bool ok = TryAppend(envelope, out error);
            if (onComplete != null)
            {
                onComplete(ok ? TransportResult.Ok(200) : TransportResult.Fail(0, error, false));
            }
        }

        public bool TrySendImmediate(TrackingEnvelope envelope)
        {
            string error;
            return TryAppend(envelope, out error);
        }

        private bool TryAppend(TrackingEnvelope envelope, out string error)
        {
            error = string.Empty;
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#else
            if (envelope == null || string.IsNullOrEmpty(envelope.JsonBody))
            {
                return true;
            }

            try
            {
                lock (_lock)
                {
                    string dir = Path.GetDirectoryName(_filePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    File.AppendAllText(_filePath, envelope.JsonBody + "\n", Encoding.UTF8);
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
#endif
        }
    }
}
