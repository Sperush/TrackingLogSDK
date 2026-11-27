using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TrackingLogSDK
{
    /// <summary>
    /// Static entry point for the Tracking Log SDK inside Unity projects.
    /// </summary>
    public static class TrackingLog
    {
        private static TrackingEngine _engine;
        private static TrackingRunner _runner;
        private static CompositeLogTransport _compositeTransport;

        public static bool IsInitialized
        {
            get { return _engine != null; }
        }

        public static TrackingEngine Engine
        {
            get { return _engine; }
        }

        public static string SessionId
        {
            get { return _engine != null ? _engine.Metadata.SessionId : string.Empty; }
        }

        /// <summary>
        /// Initializes the SDK using a TrackingConfig and optional custom ILogTransport.
        /// </summary>
        public static void Initialize(TrackingConfig config, ILogTransport customTransport = null)
        {
            if (_engine != null)
            {
                Shutdown();
            }

            TrackingConfig cfg = config != null ? config.Clone() : new TrackingConfig();
            EnsureRunner();

            _compositeTransport = new CompositeLogTransport();
            if (customTransport != null)
            {
                _compositeTransport.Add(customTransport);
            }
            else
            {
                if (cfg.EnableHttpTransport && !string.IsNullOrWhiteSpace(cfg.EndpointUrl))
                {
                    _compositeTransport.Add(new HttpLogTransport(cfg, _runner.RunCoroutine));
                }
                if (cfg.EnableConsoleTransport)
                {
                    _compositeTransport.Add(new ConsoleLogTransport());
                }
                if (cfg.EnableFileTransport)
                {
                    _compositeTransport.Add(new FileLogTransport(cfg.LogFilePath));
                }
            }

            TrackingMetadata metadata = UnityEnvironmentCollector.Collect(cfg);
            IAttemptStore attemptStore = cfg.PersistAttemptCounts
                ? (IAttemptStore)new PlayerPrefsAttemptStore()
                : new InMemoryAttemptStore();

            ILogQueueStorage storage = null;
#if !UNITY_WEBGL || UNITY_EDITOR
            if (cfg.EnableOfflinePersistence)
            {
                string queuePath = !string.IsNullOrEmpty(cfg.OfflineQueueFilePath)
                    ? cfg.OfflineQueueFilePath
                    : Path.Combine(Application.persistentDataPath, "tracking_offline_queue.jsonl");
                storage = new FileLogQueueStorage(queuePath);
            }
#endif

            _engine = new TrackingEngine(
                cfg,
                metadata,
                _compositeTransport,
                attemptStore,
                new SystemClock(),
                storage);

            _runner.Bind(_engine);
        }

        /// <summary>
        /// Initializes the SDK from a TrackingConfigAsset ScriptableObject.
        /// </summary>
        public static void Initialize(TrackingConfigAsset configAsset, ILogTransport customTransport = null)
        {
            Initialize(configAsset != null ? configAsset.ToConfig() : new TrackingConfig(), customTransport);
        }

        /// <summary>
        /// Registers an additional transport after initialization.
        /// </summary>
        public static void AddTransport(ILogTransport transport)
        {
            if (_compositeTransport != null && transport != null)
            {
                _compositeTransport.Add(transport);
            }
        }

        public static void SetUserId(string userId)
        {
            if (_engine != null) _engine.SetUserId(userId);
        }

        public static void SetCustomMetadata(string key, object value)
        {
            if (_engine != null) _engine.SetCustomMetadata(key, value);
        }

        public static void RemoveCustomMetadata(string key)
        {
            if (_engine != null) _engine.RemoveCustomMetadata(key);
        }

        public static void StartSession()
        {
            if (_engine != null) _engine.StartSession();
        }

        public static void EndSession()
        {
            if (_engine != null) _engine.EndSession();
        }

        public static void StartLevel(string levelId, string levelType = "normal")
        {
            if (_engine != null) _engine.StartLevel(levelId, levelType);
        }

        public static void StartLevel(int levelId, string levelType = "normal")
        {
            if (_engine != null) _engine.StartLevel(levelId, levelType);
        }

        public static void RetryLevel(string levelId, string levelType = "normal")
        {
            if (_engine != null) _engine.RetryLevel(levelId, levelType);
        }

        public static void RetryLevel(int levelId, string levelType = "normal")
        {
            if (_engine != null) _engine.RetryLevel(levelId, levelType);
        }

        public static void RetryCurrentLevel()
        {
            if (_engine != null) _engine.RetryCurrentLevel();
        }

        public static void TrackLevelEvent(string eventName, string value = "")
        {
            if (_engine != null) _engine.TrackLevelEvent(eventName, value);
        }

        public static void SetLevelResultProvider(Func<IDictionary<string, object>> provider)
        {
            if (_engine != null) _engine.SetLevelResultProvider(provider);
        }

        public static void SetLevelResultValue(string key, object value)
        {
            if (_engine != null) _engine.SetLevelResultValue(key, value);
        }

        public static void PauseLevel(string reason = "menu")
        {
            if (_engine != null) _engine.PauseLevel(reason);
        }

        public static void ResumeLevel()
        {
            if (_engine != null) _engine.ResumeLevel();
        }

        public static void ResumeLevelFromAppReturn()
        {
            if (_engine != null) _engine.ResumeLevelFromAppReturn();
        }

        public static void EndLevelWin(IDictionary<string, object> finalResult = null)
        {
            if (_engine != null) _engine.EndLevelWin(finalResult);
        }

        public static void EndLevelLose(string loseReason, IDictionary<string, object> finalResult = null)
        {
            if (_engine != null) _engine.EndLevelLose(loseReason, finalResult);
        }

        public static void QuitLevel(IDictionary<string, object> finalResult = null)
        {
            if (_engine != null) _engine.QuitLevel(finalResult);
        }

        public static void EndLevel(string actionType, string loseReason = null, IDictionary<string, object> finalResult = null)
        {
            if (_engine != null) _engine.EndLevel(actionType, loseReason, finalResult);
        }

        public static void TrackCustomEvent(string collection, IDictionary<string, object> fields, string actionName = "")
        {
            if (_engine != null) _engine.TrackCustomEvent(collection, fields, actionName);
        }

        public static void Flush()
        {
            if (_engine != null) _engine.Flush();
        }

        public static void Shutdown()
        {
            if (_engine != null)
            {
                _engine.HandleApplicationQuit();
                _engine = null;
            }
            _compositeTransport = null;
            if (_runner != null)
            {
                _runner.Bind(null);
            }
        }

        private static void EnsureRunner()
        {
            if (_runner != null)
            {
                return;
            }

            var go = new GameObject("[TrackingLogRunner]");
            go.hideFlags = HideFlags.HideAndDontSave;
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
            _runner = go.AddComponent<TrackingRunner>();
        }
    }
}
