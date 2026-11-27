using System.Collections.Generic;
using UnityEngine;

namespace TrackingLogSDK.Samples
{
    /// <summary>
    /// Minimal generic integration example showing how to initialize TrackingLog SDK
    /// from a centralized TrackingConfigAsset ScriptableObject (or automatic Resources/TrackingLogConfig.asset lookup).
    /// </summary>
    public sealed class QuickStartDemo : MonoBehaviour
    {
        [Header("Standalone ScriptableObject Config (Recommended)")]
        [Tooltip("Optional TrackingConfigAsset. If null, SDK auto-loads Resources/TrackingLogConfig.asset or falls back to inline fields.")]
        [SerializeField] private TrackingConfigAsset configAsset = null;

        [Header("Fallback Inline Configuration")]
        [SerializeField] private string endpointUrl = TrackingConfig.DefaultEndpointUrl;
        [SerializeField] private string gameId = "physic_game";
        [SerializeField] private string collection = "B1_Quang";
        [SerializeField] private string buildId = "dev_01";
        [SerializeField] private string userId = "player_1001";

        private int _score;
        private int _hp = 100;

        private void Awake()
        {
            if (configAsset != null)
            {
                TrackingLog.Initialize(configAsset);
            }
            else if (Resources.Load<TrackingConfigAsset>(TrackingConfigLoader.DefaultAssetResourcePath) != null)
            {
                TrackingLog.Initialize();
            }
            else
            {
                var config = new TrackingConfig
                {
                    EndpointUrl = endpointUrl,
                    GameId = gameId,
                    Collection = collection,
                    GameVersion = Application.version,
                    BuildId = buildId,
                    InProgressIntervalSeconds = 5f,
                    EnableHttpTransport = !string.IsNullOrEmpty(endpointUrl),
                    EnableConsoleTransport = true
                };
                TrackingLog.Initialize(config);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                TrackingLog.SetUserId(userId);
            }
        }

        private void Start()
        {
            StartSampleLevel("Lv_0001");
        }

        public void StartSampleLevel(string levelId)
        {
            _score = 0;
            _hp = 100;

            TrackingLog.StartLevel(levelId, "normal");
            TrackingLog.SetLevelResultProvider(() => new Dictionary<string, object>
            {
                { "score", _score },
                { "hp", _hp }
            });
        }

        public void OnReachCheckpoint(string checkpointId)
        {
            _score += 100;
            TrackingLog.TrackLevelEvent("checkpoint_reached", checkpointId);
        }

        public void OnPauseClicked()
        {
            TrackingLog.PauseLevel("menu");
        }

        public void OnResumeClicked()
        {
            TrackingLog.ResumeLevel();
        }

        public void OnLevelVictory()
        {
            TrackingLog.EndLevelWin(new Dictionary<string, object>
            {
                { "score", _score },
                { "hp", _hp },
                { "stars", 3 }
            });
        }

        public void OnLevelDefeat(string reason)
        {
            TrackingLog.EndLevelLose(reason, new Dictionary<string, object>
            {
                { "score", _score },
                { "hp", 0 }
            });
        }

        public void OnRetryClicked()
        {
            TrackingLog.RetryCurrentLevel();
        }

        public void OnQuitToMenuClicked()
        {
            TrackingLog.QuitLevel(new Dictionary<string, object>
            {
                { "score", _score },
                { "hp", _hp }
            });
        }
    }
}
