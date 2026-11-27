using UnityEngine;

namespace TrackingLogSDK
{
    /// <summary>
    /// Collects device, OS, platform, and browser metadata from the active Unity runtime.
    /// </summary>
    public static class UnityEnvironmentCollector
    {
        public static TrackingMetadata Collect(TrackingConfig config)
        {
            var meta = new TrackingMetadata
            {
                GameId = config != null ? (config.GameId ?? string.Empty) : string.Empty,
                GameVersion = config != null && !string.IsNullOrEmpty(config.GameVersion)
                    ? config.GameVersion
                    : Application.version,
                BuildId = config != null ? (config.BuildId ?? string.Empty) : string.Empty,
                DeviceModel = SystemInfo.deviceModel ?? string.Empty,
                OsVersion = SystemInfo.operatingSystem ?? string.Empty,
                Platform = NormalizePlatform(Application.platform),
                Browser = string.Empty
            };

#if UNITY_WEBGL && !UNITY_EDITOR
            string userAgent = TrackingWebGLBridge.GetUserAgent();
            if (!string.IsNullOrEmpty(userAgent))
            {
                ParsedUserAgent parsed = UserAgentParser.Parse(userAgent);
                meta.Browser = parsed.Browser;
                if (!string.IsNullOrEmpty(parsed.Platform))
                {
                    meta.Platform = parsed.Platform;
                }
                if (!string.IsNullOrEmpty(parsed.OsVersion))
                {
                    meta.OsVersion = parsed.OsVersion;
                }
            }
            if (string.IsNullOrEmpty(meta.DeviceModel) || meta.DeviceModel == "System Product Name")
            {
                meta.DeviceModel = "Browser";
            }
#endif

            if (config != null)
            {
                if (!string.IsNullOrEmpty(config.PlatformOverride))
                {
                    meta.Platform = config.PlatformOverride;
                }
                if (!string.IsNullOrEmpty(config.BrowserOverride))
                {
                    meta.Browser = config.BrowserOverride;
                }
            }

            return meta;
        }

        public static string NormalizePlatform(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "Windows";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "MacOS";
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return "Linux";
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                default:
                    return platform.ToString();
            }
        }
    }
}
