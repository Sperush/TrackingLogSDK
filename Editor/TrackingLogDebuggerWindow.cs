using System;
using UnityEditor;
using UnityEngine;

namespace TrackingLogSDK.Editor
{
    /// <summary>
    /// Live Inspector/Debugger window for inspecting session, level state, dispatch queue, and captured payloads.
    /// </summary>
    public sealed class TrackingLogDebuggerWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private string _filterText = string.Empty;
        private bool _autoScroll = true;
        private int _lastEntryCount = -1;

        [MenuItem("Window/Tracking Log SDK/Live Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<TrackingLogDebuggerWindow>("Tracking Log Debugger");
            window.minSize = new Vector2(520f, 380f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (TrackingLog.IsInitialized)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawHeaderToolbar();
            EditorGUILayout.Space(4f);

            if (!TrackingLog.IsInitialized || TrackingLog.Engine == null)
            {
                EditorGUILayout.HelpBox(
                    "TrackingLog SDK is not initialized yet. Enter Play Mode with a TrackingLogBootstrap or call TrackingLog.Initialize(config).",
                    MessageType.Info);
                return;
            }

            TrackingEngine engine = TrackingLog.Engine;
            DrawStatusSection(engine);
            EditorGUILayout.Space(6f);
            DrawLogsSection(engine);
        }

        private void DrawHeaderToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Open/Create Config", EditorStyles.toolbarButton, GUILayout.Width(125f)))
            {
                TrackingLogConfigMenu.OpenOrCreateScriptableObjectConfig();
            }

            if (GUILayout.Button("Save Asset As...", EditorStyles.toolbarButton, GUILayout.Width(100f)))
            {
                CreateConfigAsset();
            }

            GUI.enabled = TrackingLog.IsInitialized;
            if (GUILayout.Button("Force Flush", EditorStyles.toolbarButton, GUILayout.Width(85f)))
            {
                TrackingLog.Flush();
            }
            if (GUILayout.Button("Clear History", EditorStyles.toolbarButton, GUILayout.Width(90f)))
            {
                if (TrackingLog.Engine != null)
                {
                    TrackingLog.Engine.ClearRecentLogs();
                }
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            _autoScroll = GUILayout.Toggle(_autoScroll, "Auto-scroll", EditorStyles.toolbarButton, GUILayout.Width(80f));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawStatusSection(TrackingEngine engine)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Live Telemetry State", EditorStyles.boldLabel);

            SessionTracker session = engine.Session;
            LevelTracker level = engine.Level;
            TrackingMetadata meta = engine.Metadata;

            EditorGUILayout.LabelField("Session",
                session.IsActive
                    ? string.Format("Active | session_id={0} | user_id={1} | duration={2:0.0}s | bg={3}",
                        meta.SessionId, meta.UserId, session.ActiveDurationSec, session.IsInBackground)
                    : "Inactive");

            EditorGUILayout.LabelField("Level",
                level.IsActive
                    ? string.Format("Active | level_id={0} ({1}) | attempt={2} | duration={3:0.0}s | paused={4} | events={5}",
                        level.LevelKey, level.LevelType, level.AttemptId, level.ActiveDurationSec, level.IsPaused, level.BufferedEventCount)
                    : "Idle");

            EditorGUILayout.LabelField("Queue",
                string.Format("Pending={0} | Sending={1} | Endpoint={2}",
                    engine.Queue.PendingCount,
                    engine.Queue.IsSending,
                    string.IsNullOrEmpty(engine.Config.EndpointUrl) ? "(none)" : engine.Config.EndpointUrl));

            EditorGUILayout.EndVertical();
        }

        private void DrawLogsSection(TrackingEngine engine)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filter:", GUILayout.Width(42f));
            _filterText = EditorGUILayout.TextField(_filterText);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                _filterText = string.Empty;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            CapturedLogEntry[] logs = engine.GetRecentLogs();
            if (_autoScroll && logs.Length != _lastEntryCount)
            {
                _lastEntryCount = logs.Length;
                _scrollPos.y = float.MaxValue;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            string filter = _filterText != null ? _filterText.Trim() : string.Empty;

            for (int i = 0; i < logs.Length; i++)
            {
                CapturedLogEntry entry = logs[i];
                if (!string.IsNullOrEmpty(filter))
                {
                    bool matches = (entry.Collection != null && entry.Collection.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        || (entry.ActionName != null && entry.ActionName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        || (entry.JsonBody != null && entry.JsonBody.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!matches)
                    {
                        continue;
                    }
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    string.Format("[{0}] {1}/{2}", entry.TimestampMs, entry.Collection, entry.ActionName),
                    EditorStyles.boldLabel);
                if (GUILayout.Button("Copy JSON", GUILayout.Width(80f)))
                {
                    EditorGUIUtility.systemCopyBuffer = entry.JsonBody;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.SelectableLabel(
                    entry.JsonBody,
                    EditorStyles.textArea,
                    GUILayout.MinHeight(46f),
                    GUILayout.MaxHeight(90f));
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private static void CreateConfigAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Tracking Log Config",
                "TrackingLogConfig",
                "asset",
                "Choose where to save TrackingLogConfig.asset");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var asset = CreateInstance<TrackingConfigAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
