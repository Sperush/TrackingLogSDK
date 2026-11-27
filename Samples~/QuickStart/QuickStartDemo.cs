using System.Collections.Generic;
using UnityEngine;

namespace TrackingLogSDK.Samples
{
    /// <summary>
    /// Minimal generic integration example showing how to initialize TrackingLog SDK
    /// and report session, level, component event, and result snapshot telemetry.
    /// </summary>
    public sealed class QuickStartDemo : MonoBehaviour
    {
        [Header("SDK Configuration")]
        [SerializeField] private string endpointUrl = "https://your-api.example.com/logs";
        [SerializeField] private string gameId = "my_unity_game";
        [SerializeField] private string buildId = "dev_01";
        [SerializeField] private string userId = "player_1001";

        private int _score;
        private int _hp = 100;

        private void Awake()
        {
            var config = new TrackingConfig
            {
                EndpointUrl = endpointUrl,
                GameId = gameId,
                GameVersion = Application.version,
                BuildId = buildId,
                InProgressIntervalSeconds = 5f,
                EnableHttpTransport = !string.IsNullOrEmpty(endpointUrl),
                EnableConsoleTransport = true
            };

            TrackingLog.Initialize(config);
            TrackingLog.SetUserId(userId);
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
