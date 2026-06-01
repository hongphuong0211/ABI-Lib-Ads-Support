using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ABI.Ads.UnityBridge
{
    public static class ABIAds
    {
        public const string CallbackGameObjectName = "ABIAdsBridgeListener";
        public const string CallbackMethodName = "OnABIAdsEvent";
        public const string DefaultGlobalConfigResourcePath = "Configs/global_config";
        public const string DefaultPlacementsResourcePath = "Configs/placements";
        public const string DefaultGlobalConfigEncryptedResourcePath = "Configs/2";
        public const string DefaultPlacementsEncryptedResourcePath = "Configs/1";

        private const string AndroidBridgeClass = "com.abi.ads.modules.unity.ABIUnityAdsBridge";
        private static bool _receiverReady;
        private static bool _customEventForwardingEnabled = true;

        /// <summary>Global events without a placement (e.g. bridge_ready, initialized).</summary>
        public static event Action<ABIAdsEvent> EventReceived;

        public static event Action<ABIAdsEvent> OnBridgeReady;
        public static event Action<ABIAdsEvent> OnInitialized;
        public static event Action<ABIAdsEvent> OnViewControllerUpdated;
        public static event Action<ABIAdsEvent> OnConfigApplied;

        /// <summary>
        /// Register detailed callbacks for a placement name (key). Replaces any previous registration for the same placement.
        /// </summary>
        public static void RegisterPlacement(string placement, ABIAdsPlacementCallbacks callbacks)
        {
            ABIAdsCallbackRegistry.Register(placement, callbacks);
        }

        /// <summary>
        /// Create, register, and return callbacks for a placement. Useful for fluent configuration in Start().
        /// </summary>
        public static ABIAdsPlacementCallbacks RegisterPlacement(string placement)
        {
            var callbacks = new ABIAdsPlacementCallbacks();
            RegisterPlacement(placement, callbacks);
            return callbacks;
        }

        public static bool UnregisterPlacement(string placement)
        {
            return ABIAdsCallbackRegistry.Unregister(placement);
        }

        public static void ClearPlacementCallbacks()
        {
            ABIAdsCallbackRegistry.Clear();
        }

        public static void Initialize(string globalConfigJson = null, string placementsJson = null)
        {
            EnsureReceiver();
            globalConfigJson = ResolveConfigJson(
                globalConfigJson,
                DefaultGlobalConfigEncryptedResourcePath,
                DefaultGlobalConfigResourcePath,
                "global config");
            placementsJson = ResolveConfigJson(
                placementsJson,
                DefaultPlacementsEncryptedResourcePath,
                DefaultPlacementsResourcePath,
                "placements config");
            ABIAdsCustomEventForwarder.ConfigurePlacements(placementsJson);

#if UNITY_ANDROID && !UNITY_EDITOR
            var resolvedGlobalConfig = globalConfigJson ?? string.Empty;
            var resolvedPlacements = placementsJson ?? string.Empty;
            RunOnAndroidUiThread(() =>
            {
                using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
                {
                    bridge.CallStatic(
                        "initialize",
                        CallbackGameObjectName,
                        CallbackMethodName,
                        resolvedGlobalConfig,
                        resolvedPlacements);
                }
            });
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_SetCallbackTarget(CallbackGameObjectName, CallbackMethodName);
            ABIUnityAds_Initialize(globalConfigJson, placementsJson);
#else
            Debug.Log("ABIAds.Initialize is a no-op on this platform.");
            DispatchInitialized("ABIAds.Initialize is not available on this platform.");
#endif
        }

        public static void Initialize(TextAsset globalConfigAsset, TextAsset placementsAsset)
        {
            Initialize(
                globalConfigAsset != null ? globalConfigAsset.text : null,
                placementsAsset != null ? placementsAsset.text : null
            );
        }

        public static void InitializeFromResources(
            string globalConfigResourcePath = DefaultGlobalConfigResourcePath,
            string placementsResourcePath = DefaultPlacementsResourcePath)
        {
            Initialize(
                LoadResourceText(globalConfigResourcePath, "global config"),
                LoadResourceText(placementsResourcePath, "placements config")
            );
        }

        public static void SetCurrentViewController()
        {
            EnsureReceiver();

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("refreshCurrentActivity");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_SetCurrentViewController();
#endif
        }

        public static bool IsReady()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                return bridge.CallStatic<bool>("isReady");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            return ABIUnityAds_IsReady() != 0;
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns true when the placement has a loaded ad in the pool and <see cref="Show"/> can
        /// display immediately (without load-and-show fallback). Requires <see cref="Initialize"/>.
        /// Banner placements may return true while show still requests a fresh banner load.
        /// </summary>
        public static bool IsPlacementReady(string placement)
        {
            if (string.IsNullOrWhiteSpace(placement))
            {
                return false;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                return bridge.CallStatic<bool>("isPlacementReady", placement.Trim());
            }
#elif UNITY_IOS && !UNITY_EDITOR
            return ABIUnityAds_IsPlacementReady(placement.Trim()) != 0;
#else
            return false;
#endif
        }

        public static void Load(string placement)
        {
            EnsureReceiver();

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("load", placement);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_Load(placement);
#else
            Debug.Log($"ABIAds.Load skipped for placement `{placement}` in editor.");
            DispatchEditorFailed(placement, "ABIAds.Load is not available in editor.");
#endif
        }

        public static void Show(string placement)
        {
            Show(placement, 0);
        }

        public static void Show(string placement, int timeoutLoadingAds)
        {
            EnsureReceiver();

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("show", placement, timeoutLoadingAds);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_ShowWithTimeout(placement, timeoutLoadingAds);
#else
            Debug.Log($"ABIAds.Show skipped for placement `{placement}` in editor.");
            DispatchEditorFailed(placement, "ABIAds.Show is not available in editor.");
#endif
        }

        public static void LoadAndShow(string placement)
        {
            EnsureReceiver();

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("loadAndShow", placement);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_LoadAndShow(placement);
#else
            Debug.Log($"ABIAds.LoadAndShow skipped for placement `{placement}` in editor.");
            DispatchEditorFailed(placement, "ABIAds.LoadAndShow is not available in editor.");
#endif
        }

        /// <summary>
        /// Splash flow: load + show with timeout and minimum delay before show (parity loadandShowWithTimeDalay).
        /// </summary>
        public static void LoadAndShowWithTimeDelay(string placement, long timeoutMs, long timeDelayMs)
        {
            EnsureReceiver();

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("loadAndShowWithTimeDelay", placement, timeoutMs, timeDelayMs);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_LoadAndShowWithTimeDelay(placement, timeoutMs, timeDelayMs);
#else
            Debug.Log($"ABIAds.LoadAndShowWithTimeDelay skipped for placement `{placement}` in editor.");
            DispatchEditorFailed(placement, "ABIAds.LoadAndShowWithTimeDelay is not available in editor.");
#endif
        }

        public static void LoadRewarded(string placement)
        {
            EnsureReceiver();

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("loadRewarded", placement);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_LoadRewarded(placement);
#else
            Debug.Log($"ABIAds.LoadRewarded skipped for placement `{placement}` in editor.");
            DispatchEditorFailed(placement, "ABIAds.LoadRewarded is not available in editor.");
#endif
        }

        public static void ShowRewarded(string placement)
        {
            ShowRewarded(placement, 0);
        }

        public static void ShowRewarded(string placement, int timeoutLoadingAds)
        {
            EnsureReceiver();

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("showRewarded", placement, timeoutLoadingAds);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_ShowRewardedWithTimeout(placement, timeoutLoadingAds);
#else
            Debug.Log($"ABIAds.ShowRewarded skipped for placement `{placement}` in editor.");
            DispatchEditorFailed(placement, "ABIAds.ShowRewarded is not available in editor.");
#endif
        }

        public static void LoadAndShowRewarded(string placement)
        {
            EnsureReceiver();

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("loadAndShowRewarded", placement);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_LoadAndShowRewarded(placement);
#else
            Debug.Log($"ABIAds.LoadAndShowRewarded skipped for placement `{placement}` in editor.");
            DispatchEditorFailed(placement, "ABIAds.LoadAndShowRewarded is not available in editor.");
#endif
        }

        public static void ShowBanner(string placement, string position = "bottom")
        {
            EnsureReceiver();

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("showBanner", placement, position);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_ShowBanner(placement, position);
#else
            Debug.Log($"ABIAds.ShowBanner skipped for placement `{placement}` in editor.");
            DispatchEditorFailed(placement, "ABIAds.ShowBanner is not available in editor.");
#endif
        }

        public static void HideBanner()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("hideBanner");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_HideBanner();
#endif
        }

        public static void DestroyBanner()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("destroyBanner");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_DestroyBanner();
#endif
        }

        /// <summary>
        /// Sets default native slot bounds on screen (normalized 0..1). Applies to new placements without
        /// per-placement bounds and to existing slots that were not customized via
        /// <see cref="SetNativePlaceholderBounds(string,float,float,float,float)"/>.
        /// </summary>
        public static void SetNativePlaceholderBounds(
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("setNativePlaceholderBounds", minX, minY, maxX, maxY);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_SetNativePlaceholderBoundsForPlacement(placement, minX, minY, maxX, maxY);
#else
            Debug.Log($"ABIAds.SetNativePlaceholderBounds({minX},{minY},{maxX},{maxY}) skipped on this platform.");
#endif
        }

        /// <summary>
        /// Sets bounds for a specific native placement (normalized 0..1). Call before or after
        /// <see cref="ShowNative"/> for the same <paramref name="placement"/>.
        /// Android only; iOS uses the global <see cref="SetNativePlaceholderBounds(float,float,float,float)"/>.
        /// </summary>
        public static void SetNativePlaceholderBounds(
            string placement,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            if (string.IsNullOrWhiteSpace(placement))
            {
                SetNativePlaceholderBounds(minX, minY, maxX, maxY);
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic(
                    "setNativePlaceholderBounds",
                    placement,
                    minX,
                    minY,
                    maxX,
                    maxY);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_SetNativePlaceholderBoundsForPlacement(placement, minX, minY, maxX, maxY);
#else
            Debug.Log(
                $"ABIAds.SetNativePlaceholderBounds({placement},{minX},{minY},{maxX},{maxY}) skipped on this platform.");
#endif
        }

        public static void ShowNative(
            string placement,
            string templateName = null,
            NativeSize size = NativeSize.Medium,
            NativePosition position = NativePosition.Bottom,
            int duration = 0)
        {
            ShowNative(placement, templateName, size, position, duration, null);
        }

        public static void ShowNative(
            string placement,
            string templateName,
            string size,
            string position,
            int duration = 0)
        {
            ShowNative(placement, templateName, size, position, duration, null);
        }

        /// <summary>
        /// Show a native ad as a fullscreen Google Native-style overlay.
        /// Pass the placement name from placements.json; the bridge uses the full screen bounds automatically.
        /// </summary>
        public static void ShowNativeFullScreen(
            string placement,
            int countDownSec = 3,
            string templateName = null)
        {
            ShowNativeFullScreen(new GoogleNativeFullScreenConfig(
                placement,
                templateName,
                countDownSec
            ));
        }

        /// <summary>
        /// Show a native ad as fullscreen using a small config object, similar to FSN/Google setup params.
        /// </summary>
        public static void ShowNativeFullScreen(GoogleNativeFullScreenConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Placement))
            {
                Debug.LogWarning("ABIAds.ShowNativeFullScreen skipped because placement is empty.");
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("prepareNativeFullScreenShow", config.Placement, true);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_PrepareNativeFullScreenShow(config.Placement, 1);
#endif

            ShowNative(
                config.Placement,
                config.TemplateName,
                NativeSize.FreeSize,
                NativePosition.Center,
                Mathf.Max(0, config.CountDownSec),
                NativePlaceholderBounds.FullScreen
            );
        }

        /// <summary>
        /// Show native with optional screen bounds (min/max X/Y as 0..1 fractions of screen size).
        /// </summary>
        public static void ShowNative(
            string placement,
            string templateName,
            NativeSize size,
            NativePosition position,
            int duration,
            NativePlaceholderBounds? bounds)
        {
            ShowNative(
                placement,
                templateName,
                ToNativeSizeValue(size),
                ToNativePositionValue(position),
                duration,
                bounds
            );
        }

        /// <summary>
        /// Show native with raw string values for compatibility with existing integrations.
        /// Prefer the enum overload for new code.
        /// </summary>
        public static void ShowNative(
            string placement,
            string templateName,
            string size,
            string position,
            int duration,
            NativePlaceholderBounds? bounds)
        {
            EnsureReceiver();

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                if (bounds.HasValue)
                {
                    var b = bounds.Value;
                    bridge.CallStatic(
                        "showNative",
                        placement,
                        templateName,
                        size,
                        position,
                        duration,
                        b.MinX,
                        b.MinY,
                        b.MaxX,
                        b.MaxY);
                }
                else
                {
                    bridge.CallStatic("showNative", placement, templateName, size, position, duration);
                }
            }
#elif UNITY_IOS && !UNITY_EDITOR
                if (bounds.HasValue)
                {
                    var b = bounds.Value;
                    ABIUnityAds_ShowNativeWithDurationAndBounds(
                        placement,
                        templateName,
                        size,
                        position,
                        duration,
                        b.MinX,
                        b.MinY,
                        b.MaxX,
                        b.MaxY);
                }
                else
                {
                    ABIUnityAds_ShowNativeWithDuration(placement, templateName, size, position, duration);
                }
#else
            Debug.Log($"ABIAds.ShowNative skipped for placement `{placement}` in editor.");
            DispatchEditorFailed(placement, "ABIAds.ShowNative is not available in editor.");
#endif
        }

        private static string ToNativeSizeValue(NativeSize size)
        {
            switch (size)
            {
                case NativeSize.Small:
                    return "small";
                case NativeSize.FreeSize:
                    return "free_size";
                case NativeSize.Medium:
                default:
                    return "medium";
            }
        }

        private static string ToNativePositionValue(NativePosition position)
        {
            switch (position)
            {
                case NativePosition.Top:
                    return "top";
                case NativePosition.Center:
                    return "center";
                case NativePosition.Bottom:
                default:
                    return "bottom";
            }
        }

        /// <summary>Hides all native overlay slots.</summary>
        public static void HideNative()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("hideNative");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_HideNative();
#endif
        }

        /// <summary>Hides one native placement; other placements stay visible.</summary>
        public static void HideNative(string placement)
        {
            if (string.IsNullOrWhiteSpace(placement))
            {
                HideNative();
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("hideNative", placement);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_HideNativeForPlacement(placement);
#else
            Debug.Log($"ABIAds.HideNative({placement}) skipped on this platform.");
#endif
        }

        /// <summary>Destroys all native overlay slots and unregisters presentations.</summary>
        public static void DestroyNative()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("destroyNative");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_DestroyNative();
#endif
        }

        /// <summary>Destroys one native placement slot; others are unchanged.</summary>
        public static void DestroyNative(string placement)
        {
            if (string.IsNullOrWhiteSpace(placement))
            {
                DestroyNative();
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var bridge = new AndroidJavaClass(AndroidBridgeClass))
            {
                bridge.CallStatic("destroyNative", placement);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            ABIUnityAds_DestroyNativeForPlacement(placement);
#else
            Debug.Log($"ABIAds.DestroyNative({placement}) skipped on this platform.");
#endif
        }

        internal static void DispatchNativeEvent(string json)
        {
            var adsEvent = ABIAdsEvent.FromJson(json);
            if (_customEventForwardingEnabled)
            {
                ABIAdsCustomEventForwarder.TryForward(adsEvent);
            }

            DispatchGlobalEvent(adsEvent);
            ABIAdsCallbackRegistry.Dispatch(adsEvent);
            EventReceived?.Invoke(adsEvent);
        }

        private static void DispatchEditorFailed(string placement, string error)
        {
            var adsEvent = new ABIAdsEvent
            {
                eventName = ABIAdsEventNames.Failed,
                placement = placement,
                error = error,
                platform = "editor"
            };

            DispatchGlobalEvent(adsEvent);
            ABIAdsCallbackRegistry.Dispatch(adsEvent);
            EventReceived?.Invoke(adsEvent);
        }

        private static void DispatchInitialized(string message)
        {
            var adsEvent = new ABIAdsEvent
            {
                eventName = ABIAdsEventNames.Initialized,
                error = message,
                ready = false,
                platform = "editor"
            };

            DispatchGlobalEvent(adsEvent);
            EventReceived?.Invoke(adsEvent);
        }

        private static void DispatchGlobalEvent(ABIAdsEvent adsEvent)
        {
            if (adsEvent == null || string.IsNullOrEmpty(adsEvent.eventName))
            {
                return;
            }

            switch (adsEvent.eventName)
            {
                case ABIAdsEventNames.BridgeReady:
                    OnBridgeReady?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.Initialized:
                    OnInitialized?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.ViewControllerUpdated:
                    OnViewControllerUpdated?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.ConfigApplied:
                    OnConfigApplied?.Invoke(adsEvent);
                    break;
            }
        }

        public static void SetCustomEventForwardingEnabled(bool enabled)
        {
            _customEventForwardingEnabled = enabled;
        }

        private static void EnsureReceiver()
        {
            if (_receiverReady)
            {
                return;
            }

            var receiverObject = GameObject.Find(CallbackGameObjectName);
            if (receiverObject == null)
            {
                receiverObject = new GameObject(CallbackGameObjectName);
            }

            if (receiverObject.GetComponent<ABIAdsCallbackReceiver>() == null)
            {
                receiverObject.AddComponent<ABIAdsCallbackReceiver>();
            }

            UnityEngine.Object.DontDestroyOnLoad(receiverObject);
            _receiverReady = true;
        }

        private static string ResolveConfigJson(string json, string encryptedResourcePath, string plainResourcePath, string label)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                return json;
            }

            var encrypted = LoadResourceText(encryptedResourcePath, label, false);
            return !string.IsNullOrWhiteSpace(encrypted)
                ? encrypted
                : LoadResourceText(plainResourcePath, label, true);
        }

        private static string LoadResourceText(string resourcePath, string label, bool warnIfMissing = true)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset != null)
            {
                return asset.text;
            }

            if (warnIfMissing)
            {
                Debug.LogWarning($"ABIAds.Initialize could not load {label} from Resources/{resourcePath}.");
            }

            return string.Empty;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void RunOnAndroidUiThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity != null)
                {
                    activity.Call("runOnUiThread", new AndroidUiThreadRunnable(action));
                    return;
                }
            }

            action.Invoke();
        }

        private sealed class AndroidUiThreadRunnable : AndroidJavaProxy
        {
            readonly Action _action;

            public AndroidUiThreadRunnable(Action action) : base("java.lang.Runnable")
            {
                _action = action;
            }

            // Called from Android UI thread via JNI.
            public void run()
            {
                _action?.Invoke();
            }
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ABIUnityAds_SetCallbackTarget(string gameObjectName, string methodName);

        [DllImport("__Internal")]
        private static extern void ABIUnityAds_Initialize(string globalConfigJson, string placementsJson);

        [DllImport("__Internal")]
        private static extern int ABIUnityAds_IsReady();

        [DllImport("__Internal")]
        private static extern int ABIUnityAds_IsPlacementReady(string placement);

        [DllImport("__Internal")]
        private static extern void ABIUnityAds_SetCurrentViewController();

        [DllImport("__Internal")]
        private static extern void ABIUnityAds_Load(string placement);

        [DllImport("__Internal", EntryPoint = "ABIUnityAds_ShowWithTimeout")]
        private static extern void ABIUnityAds_ShowWithTimeout(string placement, int timeoutMs);

        [DllImport("__Internal")]
        private static extern void ABIUnityAds_LoadAndShow(string placement);

        [DllImport("__Internal", EntryPoint = "ABIUnityAds_LoadAndShowWithTimeDelay")]
        private static extern void ABIUnityAds_LoadAndShowWithTimeDelay(string placement, long timeoutMs, long timeDelayMs);

        [DllImport("__Internal")]
        private static extern void ABIUnityAds_LoadRewarded(string placement);

        [DllImport("__Internal", EntryPoint = "ABIUnityAds_ShowRewardedWithTimeout")]
        private static extern void ABIUnityAds_ShowRewardedWithTimeout(string placement, int timeoutMs);

        [DllImport("__Internal")]
        private static extern void ABIUnityAds_LoadAndShowRewarded(string placement);

        [DllImport("__Internal")]
        private static extern void ABIUnityAds_ShowBanner(string placement, string position);

        [DllImport("__Internal")]
        private static extern void ABIUnityAds_HideBanner();

        [DllImport("__Internal")]
        private static extern void ABIUnityAds_DestroyBanner();

        [DllImport("__Internal", EntryPoint = "ABIUnityAds_SetNativePlaceholderBounds")]
        private static extern void ABIUnityAds_SetNativePlaceholderBounds(
            float minX,
            float minY,
            float maxX,
            float maxY);

        [DllImport("__Internal", EntryPoint = "ABIUnityAds_SetNativePlaceholderBoundsForPlacement")]
        private static extern void ABIUnityAds_SetNativePlaceholderBoundsForPlacement(
            string placement,
            float minX,
            float minY,
            float maxX,
            float maxY);

        [DllImport("__Internal", EntryPoint = "ABIUnityAds_ShowNativeWithDuration")]
        private static extern void ABIUnityAds_ShowNativeWithDuration(
            string placement,
            string templateName,
            string size,
            string position,
            int duration);

        [DllImport("__Internal", EntryPoint = "ABIUnityAds_ShowNativeWithDurationAndBounds")]
        private static extern void ABIUnityAds_ShowNativeWithDurationAndBounds(
            string placement,
            string templateName,
            string size,
            string position,
            int duration,
            float minX,
            float minY,
            float maxX,
            float maxY);

        [DllImport("__Internal", EntryPoint = "ABIUnityAds_PrepareNativeFullScreenShow")]
        private static extern void ABIUnityAds_PrepareNativeFullScreenShow(string placement, int dismissOnAdClick);

        [DllImport("__Internal")]
        private static extern void ABIUnityAds_HideNative();

        [DllImport("__Internal", EntryPoint = "ABIUnityAds_HideNativeForPlacement")]
        private static extern void ABIUnityAds_HideNativeForPlacement(string placement);

        [DllImport("__Internal")]
        private static extern void ABIUnityAds_DestroyNative();

        [DllImport("__Internal", EntryPoint = "ABIUnityAds_DestroyNativeForPlacement")]
        private static extern void ABIUnityAds_DestroyNativeForPlacement(string placement);
#endif
    }
}
