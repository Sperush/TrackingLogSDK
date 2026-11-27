using System;
using System.Collections.Generic;

namespace TrackingLogSDK
{
    /// <summary>
    /// Persists and increments per-level attempt counters.
    /// </summary>
    public interface IAttemptStore
    {
        int NextAttempt(string gameId, string levelId);
        int GetCurrentAttempt(string gameId, string levelId);
        void ResetAttempt(string gameId, string levelId);
    }

    /// <summary>
    /// Non-persistent attempt store useful for unit tests or single-run sessions.
    /// </summary>
    public sealed class InMemoryAttemptStore : IAttemptStore
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>(StringComparer.Ordinal);

        public int NextAttempt(string gameId, string levelId)
        {
            string key = BuildKey(gameId, levelId);
            int current;
            _counts.TryGetValue(key, out current);
            int next = current + 1;
            _counts[key] = next;
            return next;
        }

        public int GetCurrentAttempt(string gameId, string levelId)
        {
            int current;
            return _counts.TryGetValue(BuildKey(gameId, levelId), out current) ? current : 0;
        }

        public void ResetAttempt(string gameId, string levelId)
        {
            _counts.Remove(BuildKey(gameId, levelId));
        }

        private static string BuildKey(string gameId, string levelId)
        {
            return (gameId ?? string.Empty) + "::" + (levelId ?? string.Empty);
        }
    }
}
