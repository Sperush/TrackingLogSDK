using UnityEngine;

namespace TrackingLogSDK
{
    /// <summary>
    /// Drop-in component for initializing Tracking Log SDK in Awake from a TrackingConfigAsset
    /// ScriptableObject, an optional JSON config, or Resources/TrackingLogConfig.asset automatically.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrackingLogBootstrap : MonoBehaviour
    {
        [Tooltip("Primary ScriptableObject config asset. If empty, SDK auto-loads Resources/TrackingLogConfig.asset.")]
        [SerializeField]
        private TrackingConfigAsset configAsset = null;

        [Tooltip("Optional standalone tracking_config.json TextAsset. Used if configAsset is not assigned.")]
        [SerializeField]
        private TextAsset jsonConfigFile = null;

        [SerializeField]
        private bool dontDestroyOnLoad = true;

        private void Awake()
        {
            if (TrackingLog.IsInitialized)
            {
                return;
            }

            if (dontDestroyOnLoad && Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (configAsset != null)
            {
                TrackingLog.Initialize(configAsset);
            }
            else if (jsonConfigFile != null)
            {
                TrackingLog.Initialize(jsonConfigFile);
            }
            else
            {
                // Auto-loads Resources/TrackingLogConfig.asset, Resources/tracking_config.json, or defaults.
                TrackingLog.Initialize();
            }
        }
    }
}
