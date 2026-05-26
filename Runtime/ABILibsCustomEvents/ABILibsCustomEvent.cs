using Firebase.Analytics;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
namespace ABILibsSDK
{
    /// <summary>
    /// Entry points for Firebase Analytics custom events used in TROAS / Bamboo / purchase flows.
    /// All public methods marshal work to the Unity main thread; safe to call from AppLovin MAX callbacks or background threads.
    /// Requires <see cref="ABILibsCustomEventConfig.Instance"/> to be initialized; otherwise calls no-op.
    /// </summary>
    public class ABILibsCustomEvent
    {
        #region  TROAS Ad Event
        private const string TROAS_CACHE_KEY_BANNER = "[ABILibsSDK]troas_cache_banner";
        private const string TROAS_CACHE_KEY = "[ABILibsSDK]troas_cache";
        private const string IS_FIRST_TIME_CACHE_KEY = "[ABILibsSDK]is_first_time_cache_";
        /// <summary>
        /// Records ad impression revenue for TROAS-style tiered events (cumulative cache + first-hit semantics).
        /// </summary>
        /// <param name="adUnitId">Ad unit identifier from MAX (passed through for API symmetry; not used in current implementation).</param>
        /// <param name="adInfo">MAX ad info; <see cref="MaxSdkBase.AdInfo.Revenue"/> and <see cref="MaxSdkBase.AdInfo.AdFormat"/> are read on the calling thread then applied on the main thread.</param>
        /// <remarks>
        /// Banner/MREC: revenue is accumulated until it exceeds <c>minRevenueThresholdForBannerAndMrec</c>, then flushed.
        /// Other formats: revenue is added to a shared cache. When cache crosses thresholds in <c>troasAdEvents</c>,
        /// Firebase events named <c>baseTROASEventName</c> + index are logged with parameters <c>value</c> (USD) and <c>currency</c> ("USD").
        /// </remarks>
        public static void TROASEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            double impressionRevenue = adInfo.Revenue;
            string snapshotFormat = adInfo.AdFormat != null ? adInfo.AdFormat.ToLowerInvariant() : string.Empty;
            MainThreadDispatcher.RunOnMainThread(() => TROASEventOnMainThread(impressionRevenue, snapshotFormat));
        }

        private static void TROASEventOnMainThread(double impressionRevenue, string adFormatLower)
        {
            if (ABILibsCustomEventConfig.Instance == null) return;
            double revenue = impressionRevenue;
            if (adFormatLower == "banner" || adFormatLower == "mrec")
            {
                // With Banner and Mrec, we should track until revenue reach the min threshold
                float troasCacheBanner = PlayerPrefs.GetFloat(TROAS_CACHE_KEY_BANNER, 0);
                float currentTroasCacheBanner = troasCacheBanner + (float)revenue;
                PlayerPrefs.SetFloat(TROAS_CACHE_KEY_BANNER, currentTroasCacheBanner);
                if (currentTroasCacheBanner <= ABILibsCustomEventConfig.Instance.minRevenueThresholdForBannerAndMrec)
                {
                    return;
                }
                else
                {
                    revenue = currentTroasCacheBanner;
                    PlayerPrefs.SetFloat(TROAS_CACHE_KEY_BANNER, 0);
                }
            }

            float troasCache = PlayerPrefs.GetFloat(TROAS_CACHE_KEY, 0);
            float currentTroasCache = troasCache + (float)revenue;
            PlayerPrefs.SetFloat(TROAS_CACHE_KEY, currentTroasCache);
            for (int i = 0; i < ABILibsCustomEventConfig.Instance.troasAdEvents.Length; i++)
            {
                if (currentTroasCache >= ABILibsCustomEventConfig.Instance.troasAdEvents[i])
                {
                    bool isFirstTimeCache = PlayerPrefs.GetInt(IS_FIRST_TIME_CACHE_KEY + i, 0) == 0;
                    float eventVal = (float)impressionRevenue;
                    if (isFirstTimeCache)
                    {
                        eventVal = currentTroasCache;
                        PlayerPrefs.SetInt(IS_FIRST_TIME_CACHE_KEY + i, 1);
                    }
                    Parameter[] parameters = new Parameter[2];
                    parameters[0] = new Parameter("value", (double)eventVal);
                    parameters[1] = new Parameter("currency", "USD");
                    FirebaseAnalytics.LogEvent(ABILibsCustomEventConfig.Instance.baseTROASEventName + i.ToString(), parameters);
                }
            }
        }
        private const string TROAS_CACHE_KEY_2 = "[ABILibsSDK]troas_cache_2";
        private const string PREFIX_LAST_SEND_EVENT_CACHE_2 = "[ABILibsSDK]last_send_event_cache_2_";
        /// <summary>
        /// TROAS ad revenue variant: logs when cumulative revenue since the last fired threshold increases by at least each tier in <c>troasAdEvents2</c>.
        /// </summary>
        /// <param name="adUnitId">Ad unit identifier from MAX (reserved; not used in current implementation).</param>
        /// <param name="adInfo">MAX ad info; revenue and ad format are snapshotted before main-thread work.</param>
        /// <remarks>
        /// Banner/MREC handling matches <see cref="TROASEvent"/>.
        /// For each tier, <c>value</c> is the revenue delta since that tier’s last log; only one matching tier fires per call (first match wins, then loop breaks).
        /// Event names: <c>baseTROASEventName2</c> + index.
        /// </remarks>
        public static void TROASEvent2(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            double impressionRevenue = adInfo.Revenue;
            string snapshotFormat = adInfo.AdFormat != null ? adInfo.AdFormat.ToLowerInvariant() : string.Empty;
            MainThreadDispatcher.RunOnMainThread(() => TROASEvent2OnMainThread(impressionRevenue, snapshotFormat));
        }

        private static void TROASEvent2OnMainThread(double impressionRevenue, string adFormatLower)
        {
            if (ABILibsCustomEventConfig.Instance == null) return;
            double revenue = impressionRevenue;
            if (adFormatLower == "banner" || adFormatLower == "mrec")
            {
                // With Banner and Mrec, we should track until revenue reach the min threshold
                float troasCacheBanner = PlayerPrefs.GetFloat(TROAS_CACHE_KEY_BANNER, 0);
                float currentTroasCacheBanner = troasCacheBanner + (float)revenue;
                PlayerPrefs.SetFloat(TROAS_CACHE_KEY_BANNER, currentTroasCacheBanner);
                if (currentTroasCacheBanner <= ABILibsCustomEventConfig.Instance.minRevenueThresholdForBannerAndMrec)
                {
                    return;
                }
                else
                {
                    revenue = currentTroasCacheBanner;
                    PlayerPrefs.SetFloat(TROAS_CACHE_KEY_BANNER, 0);
                }
            }

            float troasCache2 = PlayerPrefs.GetFloat(TROAS_CACHE_KEY_2, 0);
            float currentTroasCache2 = troasCache2 + (float)revenue;
            PlayerPrefs.SetFloat(TROAS_CACHE_KEY_2, currentTroasCache2);
            for (int i = 0; i < ABILibsCustomEventConfig.Instance.troasAdEvents2.Length; i++)
            {
                float lastSendEventCache2 = PlayerPrefs.GetFloat(PREFIX_LAST_SEND_EVENT_CACHE_2 + i, 0);
                if (currentTroasCache2 - lastSendEventCache2 >= ABILibsCustomEventConfig.Instance.troasAdEvents2[i])
                {
                    Parameter[] parameters2 = new Parameter[2];
                    parameters2[0] = new Parameter("value", (double)(currentTroasCache2 - lastSendEventCache2));
                    parameters2[1] = new Parameter("currency", "USD");
                    FirebaseAnalytics.LogEvent(ABILibsCustomEventConfig.Instance.baseTROASEventName2 + i.ToString(), parameters2);
                    PlayerPrefs.SetFloat(PREFIX_LAST_SEND_EVENT_CACHE_2 + i, currentTroasCache2);
                    break;
                }
            }
        }
        #endregion

        #region  Bamboo Ad Event
        private const string BAMBOO_COUNT_CACHE_KEY = "[ABILibsSDK]bamboo_count_cache";
        private const string PREFIX_CACHE_REVENUE_BAMBOO = "[ABILibsSDK]cache_revenue_bamboo_";
        private const string PREFIX_LAST_SEND_EVENT_CACHE_BAMBOO = "[ABILibsSDK]last_send_event_cache_bamboo_";
        /// <summary>
        /// Increments global interstitial (non-rewarded) ad impression count and accumulates revenue for Bamboo-style tiered events.
        /// </summary>
        /// <param name="adUnitId">Ad unit identifier from MAX (reserved; not used in current implementation).</param>
        /// <param name="adInfo">MAX ad info; only <see cref="MaxSdkBase.AdInfo.Revenue"/> is used.</param>
        /// <remarks>
        /// When impression count reaches thresholds in <c>bambooCountAdEvents</c>, logs Firebase events
        /// <c>baseBambooAdEventName</c> + index with <c>value</c> / <c>currency</c> ("USD"). First fire per tier uses accumulated revenue since tracking started for that tier.
        /// </remarks>
        public static void BambooAdEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            double impressionRevenue = adInfo.Revenue;
            MainThreadDispatcher.RunOnMainThread(() => BambooAdEventOnMainThread(impressionRevenue));
        }

        private static void BambooAdEventOnMainThread(double impressionRevenue)
        {
            if (ABILibsCustomEventConfig.Instance == null) return;
            int bambooCount = PlayerPrefs.GetInt(BAMBOO_COUNT_CACHE_KEY, 0);
            bambooCount++;
            PlayerPrefs.SetInt(BAMBOO_COUNT_CACHE_KEY, bambooCount);

            for (int i = 0; i < ABILibsCustomEventConfig.Instance.bambooCountAdEvents.Length; i++)
            {
                float cacheRevenue = PlayerPrefs.GetFloat(PREFIX_CACHE_REVENUE_BAMBOO + i, 0);
                cacheRevenue += (float)impressionRevenue;
                PlayerPrefs.SetFloat(PREFIX_CACHE_REVENUE_BAMBOO + i, cacheRevenue);
                if (bambooCount >= ABILibsCustomEventConfig.Instance.bambooCountAdEvents[i])
                {
                    float eventVal = (float)impressionRevenue;
                    bool isFirstTimeCache = PlayerPrefs.GetInt(PREFIX_LAST_SEND_EVENT_CACHE_BAMBOO + i, 0) == 0;
                    if (isFirstTimeCache)
                    {
                        eventVal = cacheRevenue;
                        PlayerPrefs.SetInt(PREFIX_LAST_SEND_EVENT_CACHE_BAMBOO + i, 1);
                    }
                    Parameter[] parameters = new Parameter[2];
                    parameters[0] = new Parameter("value", (double)eventVal);
                    parameters[1] = new Parameter("currency", "USD");
                    FirebaseAnalytics.LogEvent(ABILibsCustomEventConfig.Instance.baseBambooAdEventName + i.ToString(), parameters);
                }
            }
        }
        private const string BAMBOO_COUNT_CACHE_KEY_REWARDED = "[ABILibsSDK]bamboo_count_cache_rewarded";
        private const string PREFIX_CACHE_REVENUE_BAMBOO_REWARDED = "[ABILibsSDK]cache_revenue_bamboo_rewarded_";
        private const string PREFIX_LAST_SEND_EVENT_CACHE_BAMBOO_REWARDED = "[ABILibsSDK]last_send_event_cache_bamboo_rewarded_";
        /// <summary>
        /// Same pattern as <see cref="BambooAdEvent"/> but uses a separate impression counter and config arrays for rewarded ads.
        /// </summary>
        /// <param name="adUnitId">Ad unit identifier from MAX (reserved; not used in current implementation).</param>
        /// <param name="adInfo">MAX ad info; only revenue is used.</param>
        /// <remarks>
        /// Thresholds and event name prefix come from <c>bambooCountRewardedEvents</c> and <c>baseBambooRewardedEventName</c>.
        /// </remarks>
        public static void BambooRewardedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            double impressionRevenue = adInfo.Revenue;
            MainThreadDispatcher.RunOnMainThread(() => BambooRewardedEventOnMainThread(impressionRevenue));
        }

        private static void BambooRewardedEventOnMainThread(double impressionRevenue)
        {
            if (ABILibsCustomEventConfig.Instance == null) return;
            int bambooCount = PlayerPrefs.GetInt(BAMBOO_COUNT_CACHE_KEY_REWARDED, 0);
            bambooCount++;
            PlayerPrefs.SetInt(BAMBOO_COUNT_CACHE_KEY_REWARDED, bambooCount);

            for (int i = 0; i < ABILibsCustomEventConfig.Instance.bambooCountRewardedEvents.Length; i++)
            {
                float cacheRevenue = PlayerPrefs.GetFloat(PREFIX_CACHE_REVENUE_BAMBOO_REWARDED + i, 0);
                cacheRevenue += (float)impressionRevenue;
                PlayerPrefs.SetFloat(PREFIX_CACHE_REVENUE_BAMBOO_REWARDED + i, cacheRevenue);
                if (bambooCount >= ABILibsCustomEventConfig.Instance.bambooCountRewardedEvents[i])
                {
                    float eventVal = (float)impressionRevenue;
                    bool isFirstTimeCache = PlayerPrefs.GetInt(PREFIX_LAST_SEND_EVENT_CACHE_BAMBOO_REWARDED + i, 0) == 0;
                    if (isFirstTimeCache)
                    {
                        eventVal = cacheRevenue;
                        PlayerPrefs.SetInt(PREFIX_LAST_SEND_EVENT_CACHE_BAMBOO_REWARDED + i, 1);
                    }
                    Parameter[] parameters = new Parameter[2];
                    parameters[0] = new Parameter("value", (double)eventVal);
                    parameters[1] = new Parameter("currency", "USD");
                    FirebaseAnalytics.LogEvent(ABILibsCustomEventConfig.Instance.baseBambooRewardedEventName + i.ToString(), parameters);
                }
            }
        }
        #endregion

        #region  Purchase Event
        /// <summary>
        /// Logs purchase value in USD to Firebase for TROAS purchase ranges defined in config.
        /// </summary>
        /// <param name="productId">Store product identifier (currently not sent to Firebase in this implementation).</param>
        /// <param name="price">Price in <paramref name="currency"/> units.</param>
        /// <param name="currency">ISO currency code (must exist in <c>ABILibsCustomEventConfig.exchangeRates</c> JSON for non-USD conversion).</param>
        /// <remarks>
        /// Converts <paramref name="price"/> to USD using <c>exchangeRates</c>, finds the first matching range in <c>troasPurchaseEvents</c>,
        /// then logs <c>logCount</c> events named <c>baseTROASPurchaeEventName</c>, each with <c>value</c> = USD price / <c>logCount</c> and <c>currency</c> "USD".
        /// </remarks>
        public static void TROASPurchaseEvent(string productId, float price, string currency)
        {
            string currencySnapshot = currency;
            MainThreadDispatcher.RunOnMainThread(() => TROASPurchaseEventOnMainThread(productId, price, currencySnapshot));
        }

        private static void TROASPurchaseEventOnMainThread(string productId, float price, string currency)
        {
            if (ABILibsCustomEventConfig.Instance == null) return;
            double exchangeRate = GetExchangeRate(currency);
            double priceInUSD = exchangeRate > 0 ? price / exchangeRate : price;
            for (int i = 0; i < ABILibsCustomEventConfig.Instance.troasPurchaseEvents.Length; i++)
            {
                if (priceInUSD >= ABILibsCustomEventConfig.Instance.troasPurchaseEvents[i].minRange && priceInUSD <= ABILibsCustomEventConfig.Instance.troasPurchaseEvents[i].maxRange)
                {
                    int logCount = ABILibsCustomEventConfig.Instance.troasPurchaseEvents[i].logCount;
                    if (logCount > 0)
                    {
                        double eventVal = priceInUSD / logCount;
                        for (int j = 0; j < logCount; j++)
                        {
                            Parameter[] parameters = new Parameter[2];
                            parameters[0] = new Parameter("value", (double)eventVal);
                            parameters[1] = new Parameter("currency", "USD");
                            FirebaseAnalytics.LogEvent(ABILibsCustomEventConfig.Instance.baseTROASPurchaeEventName, parameters);
                        }
                    }
                    break;
                }
            }
        }
        private static double GetExchangeRate(string currency)
        {
            var exchangeRates = ParseExchangeRatesJson(ABILibsCustomEventConfig.Instance.exchangeRates);
            return exchangeRates[currency];
        }

        private static Dictionary<string, double> ParseExchangeRatesJson(string json)
        {
            var result = new Dictionary<string, double>();
            var matches = Regex.Matches(json, @"\""([^""]+)\""\s*:\s*([\d.]+)");
            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                result[m.Groups[1].Value] = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            }
            return result;
        }
        #endregion
        /// <summary>
        /// Forwards a generic Firebase Analytics event with string parameters on the main thread.
        /// </summary>
        /// <param name="eventName">Firebase event name (must comply with Firebase naming rules).</param>
        /// <param name="parameters">Key/value pairs; copied immediately so the caller can mutate the original dictionary after this returns.</param>
        /// <remarks>
        /// If <paramref name="parameters"/> is <c>null</c>, the event is logged with no parameters (empty array).
        /// </remarks>
        public static void LogEvent(string eventName, Dictionary<string, string> parameters)
        {
            var snapshot = parameters != null ? new Dictionary<string, string>(parameters) : new Dictionary<string, string>();
            MainThreadDispatcher.RunOnMainThread(() =>
            {
                var parametersArray = new Parameter[snapshot.Count];
                int i = 0;
                foreach (var parameter in snapshot)
                {
                    parametersArray[i] = new Parameter(parameter.Key, parameter.Value);
                    i++;
                }
                FirebaseAnalytics.LogEvent(eventName, parametersArray);
            });
        }
    }
}
