using System.Collections;
using UnityEngine;

namespace TrackingLogSDK
{
    /// <summary>
    /// Persistent DontDestroyOnLoad MonoBehaviour driving periodic ticks, focus/pause handling, and quit flushing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrackingRunner : MonoBehaviour
    {
        private TrackingEngine _engine;

        public void Bind(TrackingEngine engine)
        {
            _engine = engine;
        }

        public void RunCoroutine(IEnumerator routine)
        {
            if (routine != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(routine);
            }
        }

        private void Start()
        {
            TrackingWebGLBridge.RegisterLifecycleHooks(gameObject.name, "OnWebGLUnload", "OnWebGLVisibilityChanged");
        }

        private void Update()
        {
            if (_engine != null)
            {
                _engine.Tick(Time.unscaledDeltaTime);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (_engine != null)
            {
                _engine.SetApplicationBackground(pauseStatus);
                if (pauseStatus)
                {
                    _engine.Queue.PersistNow();
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_engine != null)
            {
                _engine.SetApplicationBackground(!hasFocus);
            }
        }

        private void OnApplicationQuit()
        {
            if (_engine != null)
            {
                _engine.HandleApplicationQuit();
            }
        }

        // Invoked by TrackingWebGL.jslib on pagehide / beforeunload.
        private void OnWebGLUnload(string unused)
        {
            if (_engine != null)
            {
                _engine.HandleApplicationQuit();
            }
        }

        // Invoked by TrackingWebGL.jslib on document.visibilitychange.
        private void OnWebGLVisibilityChanged(string state)
        {
            if (_engine != null)
            {
                _engine.SetApplicationBackground(state == "hidden");
            }
        }
    }
}
