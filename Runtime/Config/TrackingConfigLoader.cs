using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace TrackingLogSDK
{
    /// <summary>
    /// Loads TrackingConfig from a centralized ScriptableObject (Resources/TrackingLogConfig.asset),
    /// an optional JSON file (Resources/tracking_config.json), or a disk path.
    /// </summary>
    public static class TrackingConfigLoader
    {
        public const string DefaultAssetResourcePath = "TrackingLogConfig";
        public const string DefaultJsonResourcePath = "tracking_config";

        /// <summary>
        /// Loads config automatically in priority order:
        /// 1) Resources/TrackingLogConfig.asset (ScriptableObject)
        /// 2) Resources/tracking_config.json (Optional JSON)
        /// 3) Default TrackingConfig (https://bigame.ezwork.vn/logs)
        /// </summary>
        public static TrackingConfig LoadDefault()
        {
            TrackingConfigAsset asset = Resources.Load<TrackingConfigAsset>(DefaultAssetResourcePath);
            if (asset != null)
            {
                return asset.ToConfig();
            }

            TextAsset jsonAsset = Resources.Load<TextAsset>(DefaultJsonResourcePath);
            if (jsonAsset != null && !string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                return LoadFromJson(jsonAsset.text);
            }

            return new TrackingConfig();
        }

        public static TrackingConfig LoadAssetFromResources(string resourcePath = DefaultAssetResourcePath)
        {
            string path = string.IsNullOrWhiteSpace(resourcePath) ? DefaultAssetResourcePath : resourcePath;
            TrackingConfigAsset asset = Resources.Load<TrackingConfigAsset>(path);
            if (asset == null)
            {
                Debug.LogWarning("[TrackingLogSDK] TrackingConfigAsset not found at Resources/" + path + ". Using default config.");
                return new TrackingConfig();
            }

            return asset.ToConfig();
        }

        public static TrackingConfig LoadFromResources(string resourcePath = DefaultJsonResourcePath)
        {
            string path = string.IsNullOrWhiteSpace(resourcePath) ? DefaultJsonResourcePath : resourcePath;
            TextAsset jsonAsset = Resources.Load<TextAsset>(path);
            if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                Debug.LogWarning("[TrackingLogSDK] Config TextAsset not found at Resources/" + path + ". Using default config.");
                return new TrackingConfig();
            }

            return LoadFromJson(jsonAsset.text);
        }

        public static TrackingConfig LoadFromJson(string json)
        {
            TrackingConfig cfg = TrackingConfigSerializer.FromJson(json);
            if (string.IsNullOrWhiteSpace(cfg.GameVersion))
            {
                cfg.GameVersion = Application.version;
            }
            return cfg;
        }

        public static TrackingConfig LoadFromJsonFile(string filePath)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath, Encoding.UTF8);
                    return LoadFromJson(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TrackingLogSDK] Failed to read config file '" + filePath + "': " + ex.Message);
                }
            }
#endif
            return new TrackingConfig();
        }
    }
}
