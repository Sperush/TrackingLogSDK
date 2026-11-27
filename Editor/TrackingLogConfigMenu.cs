using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TrackingLogSDK.Editor
{
    /// <summary>
    /// Menu helpers to create or open centralized SDK config files in Assets/Resources.
    /// </summary>
    public static class TrackingLogConfigMenu
    {
        private const string ResourcesDir = "Assets/Resources";
        private const string AssetConfigPath = "Assets/Resources/TrackingLogConfig.asset";
        private const string JsonConfigPath = "Assets/Resources/tracking_config.json";

        [MenuItem("Window/Tracking Log SDK/Open or Create Config (ScriptableObject)", priority = 1)]
        public static void OpenOrCreateScriptableObjectConfig()
        {
            EnsureResourcesFolder();

            TrackingConfigAsset existing = AssetDatabase.LoadAssetAtPath<TrackingConfigAsset>(AssetConfigPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            var asset = ScriptableObject.CreateInstance<TrackingConfigAsset>();
            AssetDatabase.CreateAsset(asset, AssetConfigPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        [MenuItem("Window/Tracking Log SDK/Create JSON Config (Optional)", priority = 2)]
        public static void CreateJsonConfigInResources()
        {
            EnsureResourcesFolder();

            if (File.Exists(JsonConfigPath))
            {
                bool openExisting = EditorUtility.DisplayDialog(
                    "tracking_config.json exists",
                    "Assets/Resources/tracking_config.json already exists. Open existing file instead of overwriting?",
                    "Open Existing",
                    "Overwrite");

                if (openExisting)
                {
                    Object existing = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonConfigPath);
                    Selection.activeObject = existing;
                    EditorGUIUtility.PingObject(existing);
                    return;
                }
            }

            string json = TrackingConfigSerializer.CreateTemplateJson();
            File.WriteAllText(JsonConfigPath, json, new UTF8Encoding(false));
            AssetDatabase.Refresh();

            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonConfigPath);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void EnsureResourcesFolder()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesDir))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
        }
    }
}
