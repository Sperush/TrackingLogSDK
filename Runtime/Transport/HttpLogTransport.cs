using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TrackingLogSDK
{
    /// <summary>
    /// HTTP POST transport backed by UnityWebRequest, with WebGL sendBeacon/keepalive support on unload.
    /// </summary>
    public sealed class HttpLogTransport : ILogTransport
    {
        private readonly string _endpointUrl;
        private readonly Dictionary<string, string> _headers;
        private readonly int _timeoutSeconds;
        private readonly Action<IEnumerator> _coroutineRunner;

        public string Name
        {
            get { return "Http"; }
        }

        public HttpLogTransport(TrackingConfig config, Action<IEnumerator> coroutineRunner)
        {
            _endpointUrl = config != null ? (config.EndpointUrl ?? string.Empty).Trim() : string.Empty;
            _timeoutSeconds = config != null && config.RequestTimeoutSeconds > 0 ? config.RequestTimeoutSeconds : 10;
            _coroutineRunner = coroutineRunner;
            _headers = BuildHeaders(config);
        }

        public void Send(TrackingEnvelope envelope, Action<TransportResult> onComplete)
        {
            if (envelope == null || string.IsNullOrEmpty(_endpointUrl))
            {
                if (onComplete != null)
                {
                    onComplete(string.IsNullOrEmpty(_endpointUrl)
                        ? TransportResult.Fail(0, "EndpointUrl is empty.", false)
                        : TransportResult.Ok(200));
                }
                return;
            }

            if (_coroutineRunner == null)
            {
                if (onComplete != null)
                {
                    onComplete(TransportResult.Fail(0, "No coroutine runner available for HttpLogTransport.", true));
                }
                return;
            }

            _coroutineRunner(SendCoroutine(envelope, onComplete));
        }

        public bool TrySendImmediate(TrackingEnvelope envelope)
        {
            if (envelope == null || string.IsNullOrEmpty(_endpointUrl))
            {
                return false;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            string headersJson = SerializeHeaders(_headers);
            return TrackingWebGLBridge.SendImmediate(_endpointUrl, envelope.JsonBody, headersJson);
#else
            try
            {
                UnityWebRequest request = CreateRequest(envelope.JsonBody);
                request.SendWebRequest();
                return true;
            }
            catch
            {
                return false;
            }
#endif
        }

        private IEnumerator SendCoroutine(TrackingEnvelope envelope, Action<TransportResult> onComplete)
        {
            using (UnityWebRequest request = CreateRequest(envelope.JsonBody))
            {
                yield return request.SendWebRequest();

                long code = request.responseCode;
                if (request.result == UnityWebRequest.Result.Success || (code >= 200 && code < 300))
                {
                    if (onComplete != null)
                    {
                        onComplete(TransportResult.Ok(code > 0 ? code : 200));
                    }
                    yield break;
                }

                bool retryable = request.result == UnityWebRequest.Result.ConnectionError
                    || code == 0
                    || code == 408
                    || code == 429
                    || code >= 500;

                string error = !string.IsNullOrEmpty(request.error)
                    ? request.error
                    : ("HTTP " + code);

                if (onComplete != null)
                {
                    onComplete(TransportResult.Fail(code, error, retryable));
                }
            }
        }

        private UnityWebRequest CreateRequest(string jsonBody)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody ?? "{}");
            var request = new UnityWebRequest(_endpointUrl, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = _timeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");

            foreach (KeyValuePair<string, string> kv in _headers)
            {
                request.SetRequestHeader(kv.Key, kv.Value);
            }

            return request;
        }

        private static Dictionary<string, string> BuildHeaders(TrackingConfig config)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (config == null)
            {
                return headers;
            }

            if (config.CustomHeaders != null)
            {
                foreach (KeyValuePair<string, string> kv in config.CustomHeaders)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key))
                    {
                        headers[kv.Key.Trim()] = kv.Value ?? string.Empty;
                    }
                }
            }

            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                string headerName = string.IsNullOrWhiteSpace(config.ApiKeyHeader) ? "X-API-Key" : config.ApiKeyHeader.Trim();
                headers[headerName] = config.ApiKey;
            }

            if (!string.IsNullOrEmpty(config.BearerToken))
            {
                headers["Authorization"] = "Bearer " + config.BearerToken;
            }

            return headers;
        }

        private static string SerializeHeaders(Dictionary<string, string> headers)
        {
            if (headers == null || headers.Count == 0)
            {
                return string.Empty;
            }

            var writer = new TrackingJsonWriter();
            writer.BeginObject();
            foreach (KeyValuePair<string, string> kv in headers)
            {
                writer.WriteProperty(kv.Key, kv.Value ?? string.Empty);
            }
            writer.EndObject();
            return writer.ToString();
        }
    }
}
