using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ABILibsSDK
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private static readonly Queue<Action> _actionQueue = new Queue<Action>();
        private static readonly object _lock = new object();
        private static int _unityMainThreadManagedId;
        private static bool _unityMainThreadIdReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureUnityMainThreadId()
        {
            _unityMainThreadManagedId = Thread.CurrentThread.ManagedThreadId;
            _unityMainThreadIdReady = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstanceExists()
        {
            if (_instance != null) return;
            var found = UnityEngine.Object.FindAnyObjectByType<MainThreadDispatcher>(FindObjectsInactive.Include);
            if (found != null) return;

            var go = new GameObject("[ABILibsSDK] MainThreadDispatcher");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<MainThreadDispatcher>();
        }

        public static bool IsUnityMainThread()
        {
            return _unityMainThreadIdReady && Thread.CurrentThread.ManagedThreadId == _unityMainThreadManagedId;
        }

        /// <summary>
        /// Runs immediately on the Unity main thread, or queues for the next frame if called from a worker thread
        /// (e.g. AppLovin MAX background callbacks on iOS/Android).
        /// </summary>
        public static void RunOnMainThread(Action action)
        {
            if (action == null) return;

            if (IsUnityMainThread())
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            else
            {
                Enqueue(action);
            }
        }

        public static void Enqueue(Action action)
        {
            if (action == null) return;

            lock (_lock)
            {
                _actionQueue.Enqueue(action);
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Update()
        {
            if (_instance == null) return;

            lock (_lock)
            {
                while (_actionQueue.Count > 0)
                {
                    var action = _actionQueue.Dequeue();
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }
    }
}
