using System.Runtime.InteropServices;

namespace TrackingLogSDK
{
    /// <summary>
    /// Managed P/Invoke wrapper for TrackingWebGL.jslib. Safe no-op outside WebGL player builds.
    /// </summary>
    public static class TrackingWebGLBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string TrackingLog_GetUserAgent();

        [DllImport("__Internal")]
        private static extern int TrackingLog_SendImmediate(string url, string body, string headersJson);

        [DllImport("__Internal")]
        private static extern void TrackingLog_RegisterLifecycleHooks(string goName, string unloadMethod, string visibilityMethod);
#endif

        public static string GetUserAgent()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return TrackingLog_GetUserAgent() ?? string.Empty;
#else
            return string.Empty;
#endif
        }

        public static bool SendImmediate(string url, string body, string headersJson)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return TrackingLog_SendImmediate(url, body, headersJson) == 1;
#else
            return false;
#endif
        }

        public static void RegisterLifecycleHooks(string gameObjectName, string unloadMethod, string visibilityMethod)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            TrackingLog_RegisterLifecycleHooks(gameObjectName, unloadMethod, visibilityMethod);
#endif
        }
    }
}
