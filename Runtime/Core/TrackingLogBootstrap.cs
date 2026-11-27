using UnityEngine;

namespace TrackingLogSDK
{
    /// <summary>
    /// Drop-in component for initializing Tracking Log SDK from a TrackingConfigAsset in Awake.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrackingLogBootstrap : MonoBehaviour
    {
        [SerializeField]
        private TrackingConfigAsset configAsset = null;

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
            else
            {
                Debug.LogWarning("[TrackingLogBootstrap] TrackingConfigAsset is not assigned. Using default config (Console only).");
                TrackingLog.Initialize(new TrackingConfig
                {
                    EnableHttpTransport = false,
                    EnableConsoleTransport = true
                });
            }
        }
    }
}
