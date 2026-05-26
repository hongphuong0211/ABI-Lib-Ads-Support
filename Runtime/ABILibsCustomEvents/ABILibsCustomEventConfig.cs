using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
#if FIREBASE_REMOTE_CONFIG
using Firebase.RemoteConfig;
#endif
namespace ABILibsSDK
{
    [CreateAssetMenu(fileName = "ABILibsCustomEventConfig", menuName = "ABILibsSDK/CustomEventConfig")]
    public class ABILibsCustomEventConfig : ScriptableObject
    {
        private const string RESOURCE_PATH = "ABILibsCustomEventConfig";

        [Header("TROAS Purchase Event")]
        public string baseTROASPurchaeEventName = "abi_sdk_rise_iap";
        public TROASPurchaseEvent[] troasPurchaseEvents;
        public string exchangeRates = "{\"AED\": 3.6725, \"AFN\": 73.83, \"ALL\": 94.4847, \"AMD\": 395.1811, \"ANG\": 1.79, \"AOA\": 918.9906, \"ARS\": 1060.88, \"AUD\": 1.5735, \"AWG\": 1.79, \"AZN\": 1.7004, \"BAM\": 1.8684, \"BBD\": 2.0, \"BDT\": 121.5387, \"BGN\": 1.8684, \"BHD\": 0.376, \"BIF\": 2965.3861, \"BMD\": 1.0, \"BND\": 1.3384, \"BOB\": 6.919, \"BRL\": 5.7302, \"BSD\": 1.0, \"BTN\": 86.7355, \"BWP\": 13.7927, \"BYN\": 3.2687, \"BZD\": 2.0, \"CAD\": 1.424, \"CDF\": 2854.24, \"CHF\": 0.8976, \"CLP\": 943.0703, \"CNY\": 7.2523, \"COP\": 4098.4544, \"CRC\": 506.016, \"CUP\": 24.0, \"CVE\": 105.3341, \"CZK\": 23.9118, \"DJF\": 177.721, \"DKK\": 7.1276, \"DOP\": 62.3329, \"DZD\": 134.8959, \"EGP\": 50.6002, \"ERN\": 15.0, \"ETB\": 126.151, \"EUR\": 0.9553, \"FJD\": 2.2948, \"FKP\": 0.7917, \"FOK\": 7.127, \"GBP\": 0.7918, \"GEL\": 2.8177, \"GGP\": 0.7917, \"GHS\": 15.5156, \"GIP\": 0.7917, \"GMD\": 72.5859, \"GNF\": 8613.9696, \"GTQ\": 7.7201, \"GYD\": 209.2693, \"HKD\": 7.7746, \"HNL\": 25.5761, \"HRK\": 7.1976, \"HTG\": 131.1801, \"HUF\": 383.3373, \"IDR\": 16287.2463, \"ILS\": 3.5727, \"IMP\": 0.7917, \"INR\": 86.7398, \"IQD\": 1312.3244, \"IRR\": 41999.9353, \"ISK\": 138.6806, \"JEP\": 0.7917, \"JMD\": 157.7191, \"JOD\": 0.709, \"JPY\": 149.705, \"KES\": 129.5087, \"KGS\": 87.382, \"KHR\": 4005.9256, \"KID\": 1.5734, \"KMF\": 469.9678, \"KRW\": 1428.7938, \"KWD\": 0.3086, \"KYD\": 0.8333, \"KZT\": 500.7322, \"LAK\": 21879.4829, \"LBP\": 89500.0, \"LKR\": 295.5388, \"LRD\": 199.4805, \"LSL\": 18.3608, \"LYD\": 4.8945, \"MAD\": 9.949, \"MDL\": 18.637, \"MGA\": 4743.9563, \"MKD\": 58.8272, \"MMK\": 2099.0029, \"MNT\": 3485.2215, \"MOP\": 8.0079, \"MRU\": 40.0044, \"MUR\": 46.3354, \"MVR\": 15.4577, \"MWK\": 1742.8828, \"MXN\": 20.4642, \"MYR\": 4.4113, \"MZN\": 63.9144, \"NAD\": 18.3608, \"NGN\": 1502.5275, \"NIO\": 36.8113, \"NOK\": 11.1228, \"NPR\": 138.7767, \"NZD\": 1.7428, \"OMR\": 0.3845, \"PAB\": 1.0, \"PEN\": 3.6847, \"PGK\": 4.0777, \"PHP\": 57.8965, \"PKR\": 279.4721, \"PLN\": 3.9608, \"PYG\": 7903.758, \"QAR\": 3.64, \"RON\": 4.7527, \"RSD\": 111.9111, \"RUB\": 87.9209, \"RWF\": 1431.9757, \"SAR\": 3.75, \"SBD\": 8.6497, \"SCR\": 14.4205, \"SDG\": 454.2589, \"SEK\": 10.6596, \"SGD\": 1.3384, \"SHP\": 0.7917, \"SLE\": 22.8566, \"SLL\": 22856.6481, \"SOS\": 571.1682, \"SRD\": 35.7749, \"SSP\": 4401.9434, \"STN\": 23.4044, \"SYP\": 12928.4878, \"SZL\": 18.3608, \"THB\": 33.5022, \"TJS\": 10.9558, \"TMT\": 3.5001, \"TND\": 3.1677, \"TOP\": 2.3803, \"TRY\": 36.4627, \"TTD\": 6.7665, \"TVD\": 1.5734, \"TWD\": 32.7128, \"TZS\": 2581.2513, \"UAH\": 41.7116, \"UGX\": 3675.5882, \"USD\": 1, \"UYU\": 43.0891, \"UZS\": 12892.7139, \"VES\": 63.4938, \"VND\": 25462.0816, \"VUV\": 122.4594, \"WST\": 2.8067, \"XAF\": 626.6237, \"XCD\": 2.7, \"XDR\": 0.7614, \"XOF\": 626.6237, \"XPF\": 113.9957, \"YER\": 247.4834, \"ZAR\": 18.3621, \"ZMW\": 28.2034, \"ZWL\": 26.4879}";
        [Header("TROAS Ad Event")]
        [Tooltip("This method involves two phases based on the user's value threshold. Initially, once the user's accumulated value reaches the defined threshold, trigger the FIRST event using the total revenue accumulated up to that point. For EVERY ad impression thereafter, continue to log the event using the individual ad value provided by the callback.")]
        public string baseTROASEventName = "abi_sdk_taichi_";
        public float[] troasAdEvents = new float[]{0.1f, 0.5f, 1f};

        [Header("TROAS Ad Event 2: Reset the cache value")]
        [Tooltip("In this alternative approach, once the user satisfies the required criteria and the FIRST event is triggered using the total revenue accumulated at that time, reset the cache value. Proceed to fire the event EVERY TIME the user reaches the defined threshold again.")]
        public string baseTROASEventName2 = "abi_sdk_taichi2_";
        public float[] troasAdEvents2 = new float[]{0.1f, 0.5f, 1f};

        [Header("Bamboo Ad Event Inter + Rewarded + App Open")]
        public string baseBambooAdEventName = "abi_sdk_watch_ad_";
        public int[] bambooCountAdEvents = new int[]{5, 10, 15};

        [Header("Bamboo Ad Event Rewarded")]
        public string baseBambooRewardedEventName = "abi_sdk_watch_rewarded_ad_";
        public int[] bambooCountRewardedEvents = new int[]{5, 10, 15};
        
        [Header("Minimum Revenue Threshold for Banner and Mrec")]
        public float minRevenueThresholdForBannerAndMrec = 0.1f;

        /// <summary>HTTP portal + Firebase Remote Config key names — fixed in code (not serialized, not overridable from Inspector/JSON).</summary>
        public static readonly bool UseHttpRemoteConfig = true;
        public const string HttpConfigBaseUrl = "https://config.abigames.com.vn";
        public const string HttpConfigPath = "/api/v1/config";
        public const string HttpConfigApiKey = "";
        public const string HttpAppIdentifierOverride = "";

        /// <summary>When false (default), <see cref="Instance"/> uses asset defaults until <see cref="FetchAndApplyHttpAsync"/> applies JSON. Set true to read overrides from Firebase Remote Config on first access / via <see cref="FetchAndApplyRemoteConfigAsync"/>.</summary>
        public static readonly bool UseFirebaseRemoteConfig = false;

        /// <summary>When true (default), automatically calls <see cref="FetchAndApplyHttpAsync"/> once at startup using the device region (<see cref="RegionInfo.CurrentRegion"/>). Set false if you call <see cref="FetchAndApplyHttpAsync"/> yourself (e.g. country from SIM or backend).</summary>
        public static readonly bool AutoFetchHttpConfigOnStartup = true;
        public const string KeyBaseTROASPurchaeEventName = "abi_base_troas_purchase_event_name";
        public const string KeyExchangeRates = "abi_exchange_rates";
        public const string KeyBaseTROASEventName = "abi_base_troas_event_name";
        public const string KeyBaseTROASEventName2 = "abi_base_troas_event_name_2";
        public const string KeyBaseBambooAdEventName = "abi_base_bamboo_ad_event_name";
        public const string KeyBaseBambooRewardedEventName = "abi_base_bamboo_rewarded_event_name";
        public const string KeyMinRevenueThresholdForBannerAndMrec = "abi_min_revenue_threshold_banner_mrec";
        public const string KeyTroasAdEvents = "abi_troas_ad_events";
        public const string KeyTroasAdEvents2 = "abi_troas_ad_events_2";
        public const string KeyBambooCountAdEvents = "abi_bamboo_count_ad_events";
        public const string KeyBambooCountRewardedEvents = "abi_bamboo_count_rewarded_events";
        public const string KeyTroasPurchaseEvents = "abi_troas_purchase_events";

        private static ABILibsCustomEventConfig _instance;
        private bool _remoteConfigApplied;

        public static ABILibsCustomEventConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ABILibsCustomEventConfig>(RESOURCE_PATH);
                    if (_instance == null)
                    {
                        Debug.LogError($"[ABILibsSDK] Config not found at Resources/{RESOURCE_PATH}. " +
                                       "Create one via Assets > Create > ABILibsSDK > CustomEventConfig and place it in a Resources folder (any folder named Resources, including inside a package).");
                    }
                }

                if (_instance != null && !_instance._remoteConfigApplied)
                {
                    _instance.ApplyFromActivatedRemoteConfig();
                }

                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoFetchHttpConfigOnLoad()
        {
            if (!UseHttpRemoteConfig || !AutoFetchHttpConfigOnStartup)
            {
                return;
            }

            StartAutoHttpFetch();
        }

        private static async void StartAutoHttpFetch()
        {
            try
            {
                var cfg = Instance;
                if (cfg == null)
                {
                    return;
                }

                string country = TryGetDeviceRegionCountryCode();
                if (string.IsNullOrEmpty(country))
                {
                    Debug.LogWarning("[ABILibsCustomEventConfig] Auto HTTP config skipped: could not resolve device region country code.");
                    return;
                }

                await cfg.FetchAndApplyHttpAsync(country);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ABILibsCustomEventConfig] Auto HTTP config failed: {ex.Message}");
            }
        }

        private static string TryGetDeviceRegionCountryCode()
        {
            try
            {
                return RegionInfo.CurrentRegion.TwoLetterISORegionName;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

#if FIREBASE_REMOTE_CONFIG
        public async Task<bool> FetchAndApplyRemoteConfigAsync(TimeSpan? cacheExpiration = null)
        {
            if (!UseFirebaseRemoteConfig)
            {
                return false;
            }

            var expiration = cacheExpiration ?? TimeSpan.FromHours(1);
            await FirebaseRemoteConfig.DefaultInstance.FetchAsync(expiration);
            await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
            return ApplyFromActivatedRemoteConfig();
        }
#else
        public Task<bool> FetchAndApplyRemoteConfigAsync(TimeSpan? cacheExpiration = null)
        {
            return Task.FromResult(false);
        }
#endif

        /// <summary>
        /// Loads config JSON from the portal (GET with query country + appId).
        /// appIdentifier: Android package name, iOS bundle id, or iOS App Store id string — must match a game on the server.
        /// Pass null/empty to use Application.identifier as appId.
        /// </summary>
        public async Task<bool> FetchAndApplyHttpAsync(string country, string appIdentifier = null, int timeoutSeconds = 30)
        {
            if (!UseHttpRemoteConfig)
            {
                return false;
            }

            string appId = !string.IsNullOrWhiteSpace(appIdentifier)
                ? appIdentifier.Trim()
                : (!string.IsNullOrWhiteSpace(HttpAppIdentifierOverride) ? HttpAppIdentifierOverride.Trim() : Application.identifier);

            if (string.IsNullOrWhiteSpace(appId))
            {
                Debug.LogError("[ABILibsCustomEventConfig] appId is empty; pass appIdentifier or set a valid Application.identifier.");
                return false;
            }

            string cc = NormalizeCountryCode(country);
            if (cc == null)
            {
                Debug.LogError("[ABILibsCustomEventConfig] country is invalid; use a 2-letter ISO code (e.g. US, VN).");
                return false;
            }

            string baseUrl = HttpConfigBaseUrl.TrimEnd('/');
            string path = string.IsNullOrWhiteSpace(HttpConfigPath) ? "/api/v1/config" : HttpConfigPath;
            if (!path.StartsWith("/", StringComparison.Ordinal))
            {
                path = "/" + path;
            }

            string url = $"{baseUrl}{path}?country={UnityWebRequest.EscapeURL(cc)}&appId={UnityWebRequest.EscapeURL(appId)}";
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = timeoutSeconds;
                if (!string.IsNullOrWhiteSpace(HttpConfigApiKey))
                {
                    req.SetRequestHeader("X-ABILibs-Config-Key", HttpConfigApiKey.Trim());
                }

                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    await Task.Yield();
                }

#if UNITY_2020_1_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    Debug.LogError($"[ABILibsCustomEventConfig] HTTP config failed: {req.error} ({req.responseCode}) body: {req.downloadHandler?.text}");
                    return false;
                }

                string json = req.downloadHandler?.text;
                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogError("[ABILibsCustomEventConfig] HTTP config returned empty body.");
                    return false;
                }

                return ApplyFromJsonString(json.Trim());
            }
        }

        private static string NormalizeCountryCode(string country)
        {
            if (string.IsNullOrWhiteSpace(country))
            {
                return null;
            }

            string c = country.Trim();
            if (c.Length == 2)
            {
                return c.ToUpperInvariant();
            }

            try
            {
                var region = new RegionInfo(c);
                return region.TwoLetterISORegionName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Applies JSON matching this ScriptableObject's serialized fields (same shape as JsonUtility export).
        /// </summary>
        public bool ApplyFromJsonString(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                JsonUtility.FromJsonOverwrite(json.Trim(), this);
                _remoteConfigApplied = true;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ABILibsCustomEventConfig] ApplyFromJsonString failed: {ex.Message}");
                return false;
            }
        }

        public bool ApplyFromActivatedRemoteConfig()
        {
#if FIREBASE_REMOTE_CONFIG
            if (!UseFirebaseRemoteConfig)
            {
                _remoteConfigApplied = true;
                return false;
            }

            bool changed = false;
            changed |= TryApplyString(KeyBaseTROASPurchaeEventName, value => baseTROASPurchaeEventName = value);
            changed |= TryApplyString(KeyExchangeRates, value => exchangeRates = value);
            changed |= TryApplyString(KeyBaseTROASEventName, value => baseTROASEventName = value);
            changed |= TryApplyString(KeyBaseTROASEventName2, value => baseTROASEventName2 = value);
            changed |= TryApplyString(KeyBaseBambooAdEventName, value => baseBambooAdEventName = value);
            changed |= TryApplyString(KeyBaseBambooRewardedEventName, value => baseBambooRewardedEventName = value);
            changed |= TryApplyFloat(KeyMinRevenueThresholdForBannerAndMrec, value => minRevenueThresholdForBannerAndMrec = value);
            changed |= TryApplyFloatArray(KeyTroasAdEvents, values => troasAdEvents = values);
            changed |= TryApplyFloatArray(KeyTroasAdEvents2, values => troasAdEvents2 = values);
            changed |= TryApplyIntArray(KeyBambooCountAdEvents, values => bambooCountAdEvents = values);
            changed |= TryApplyIntArray(KeyBambooCountRewardedEvents, values => bambooCountRewardedEvents = values);
            changed |= TryApplyPurchaseEvents(KeyTroasPurchaseEvents, values => troasPurchaseEvents = values);

            _remoteConfigApplied = true;
            return changed;
#else
            _remoteConfigApplied = true;
            return false;
#endif
        }
        [ContextMenu("Export to Json")]
        private void ExportToJson()
        {
            string json = JsonUtility.ToJson(this);
            Debug.Log(json);
            GUIUtility.systemCopyBuffer = json;
        }
        [ContextMenu("Apply From Json in clipboard")]
        private void ApplyFromJsonInClipboard()
        {
            string json = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            if (ApplyFromJsonString(json.Trim()))
            {
                Debug.Log("Applied from clipboard");
            }
        }
#if FIREBASE_REMOTE_CONFIG
        private static bool TryApplyString(string key, Action<string> apply)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            string value = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            apply(value);
            return true;
        }

        private static bool TryApplyFloat(string key, Action<float> apply)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            string value = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return false;
            }

            apply(parsed);
            return true;
        }

        private static bool TryApplyFloatArray(string key, Action<float[]> apply)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            string value = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
            if (TryParseFloatArray(value, out float[] parsed))
            {
                apply(parsed);
                return true;
            }

            return false;
        }

        private static bool TryApplyIntArray(string key, Action<int[]> apply)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            string value = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
            if (TryParseIntArray(value, out int[] parsed))
            {
                apply(parsed);
                return true;
            }

            return false;
        }

        private static bool TryApplyPurchaseEvents(string key, Action<TROASPurchaseEvent[]> apply)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            string value = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
            if (TryParsePurchaseEvents(value, out TROASPurchaseEvent[] parsed))
            {
                apply(parsed);
                return true;
            }

            return false;
        }

        private static bool TryParseFloatArray(string input, out float[] result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            if (TryParseJsonFloatArray(input, out result))
            {
                return result.Length > 0;
            }

            if (TryParseCsvFloatArray(input, out result))
            {
                return result.Length > 0;
            }

            return false;
        }

        private static bool TryParseIntArray(string input, out int[] result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            if (TryParseJsonIntArray(input, out result))
            {
                return result.Length > 0;
            }

            if (TryParseCsvIntArray(input, out result))
            {
                return result.Length > 0;
            }

            return false;
        }

        private static bool TryParsePurchaseEvents(string input, out TROASPurchaseEvent[] result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            // Expected format:
            // {"items":[{"logCount":2,"minRange":0.99,"maxRange":4.99}]}
            try
            {
                var wrapper = JsonUtility.FromJson<TROASPurchaseEventArrayWrapper>(input);
                if (wrapper != null && wrapper.items != null && wrapper.items.Length > 0)
                {
                    result = wrapper.items;
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool TryParseJsonFloatArray(string input, out float[] result)
        {
            result = null;
            try
            {
                var wrapper = JsonUtility.FromJson<FloatArrayWrapper>(input);
                if (wrapper != null && wrapper.items != null)
                {
                    result = wrapper.items;
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool TryParseJsonIntArray(string input, out int[] result)
        {
            result = null;
            try
            {
                var wrapper = JsonUtility.FromJson<IntArrayWrapper>(input);
                if (wrapper != null && wrapper.items != null)
                {
                    result = wrapper.items;
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool TryParseCsvFloatArray(string input, out float[] result)
        {
            result = null;
            string[] parts = input.Split(',');
            var values = new List<float>(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i].Trim();
                if (token.Length == 0)
                {
                    continue;
                }

                if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                {
                    return false;
                }

                values.Add(parsed);
            }

            result = values.ToArray();
            return true;
        }

        private static bool TryParseCsvIntArray(string input, out int[] result)
        {
            result = null;
            string[] parts = input.Split(',');
            var values = new List<int>(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i].Trim();
                if (token.Length == 0)
                {
                    continue;
                }

                if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                {
                    return false;
                }

                values.Add(parsed);
            }

            result = values.ToArray();
            return true;
        }
#endif
    }

    [Serializable]
    internal class FloatArrayWrapper
    {
        public float[] items;
    }

    [Serializable]
    internal class IntArrayWrapper
    {
        public int[] items;
    }

    [Serializable]
    internal class TROASPurchaseEventArrayWrapper
    {
        public TROASPurchaseEvent[] items;
    }

    [Serializable]
    public class TROASPurchaseEvent
    {
        public int logCount;
        public float minRange;
        public float maxRange;
    }
}
