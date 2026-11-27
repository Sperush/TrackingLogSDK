using UnityEngine;

namespace TrackingLogSDK
{
    /// <summary>
    /// PlayerPrefs-backed attempt store that persists attempt counts across reloads.
    /// </summary>
    public sealed class PlayerPrefsAttemptStore : IAttemptStore
    {
        private const string KeyPrefix = "TrackingLog_Attempt_";

        public int NextAttempt(string gameId, string levelId)
        {
            string key = BuildKey(gameId, levelId);
            int next = PlayerPrefs.GetInt(key, 0) + 1;
            PlayerPrefs.SetInt(key, next);
            PlayerPrefs.Save();
            return next;
        }

        public int GetCurrentAttempt(string gameId, string levelId)
        {
            return PlayerPrefs.GetInt(BuildKey(gameId, levelId), 0);
        }

        public void ResetAttempt(string gameId, string levelId)
        {
            PlayerPrefs.DeleteKey(BuildKey(gameId, levelId));
            PlayerPrefs.Save();
        }

        private static string BuildKey(string gameId, string levelId)
        {
            return KeyPrefix + (gameId ?? "default") + "_" + (levelId ?? "default");
        }
    }
}
