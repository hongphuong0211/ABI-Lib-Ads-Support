using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using ABILibsSDK;
using UnityEngine;

namespace ABI.Ads.UnityBridge
{
    internal static class ABIAdsCustomEventForwarder
    {
        private const string RevenueEventName = "revenue";

        private static readonly Dictionary<string, string> PlacementTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static MethodInfo _troasEventMethod;
        private static MethodInfo _troasEvent2Method;
        private static MethodInfo _bambooAdEventMethod;
        private static MethodInfo _bambooRewardedEventMethod;
        private static bool _lookupComplete;
        private static bool _reportedMissingMethods;

        internal static void ConfigurePlacements(string placementsJson)
        {
            PlacementTypes.Clear();
            if (string.IsNullOrWhiteSpace(placementsJson))
            {
                return;
            }

            var trimmed = placementsJson.TrimStart();
            if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
            {
                return;
            }

            try
            {
                var root = JsonUtility.FromJson<PlacementsRoot>(placementsJson);
                if (root?.placements == null)
                {
                    return;
                }

                for (int i = 0; i < root.placements.Length; i++)
                {
                    var placement = root.placements[i];
                    if (!string.IsNullOrWhiteSpace(placement.ad_name) && !string.IsNullOrWhiteSpace(placement.ads_type))
                    {
                        PlacementTypes[placement.ad_name] = placement.ads_type;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ABIAds custom event forwarding could not parse placement config: {ex.Message}");
            }
        }

        internal static void TryForward(ABIAdsEvent adsEvent)
        {
            if (adsEvent == null || !string.Equals(adsEvent.eventName, RevenueEventName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!TryParseRevenue(adsEvent.revenue, out double revenue) || revenue <= 0)
            {
                return;
            }

            if (!EnsureMethods())
            {
                return;
            }

            string adType = ResolveAdType(adsEvent);
            string adFormat = NormalizeAdFormat(adType);

            try
            {
                _troasEventMethod?.Invoke(null, new object[] { revenue, adFormat });
                _troasEvent2Method?.Invoke(null, new object[] { revenue, adFormat });

                if (IsRewarded(adType))
                {
                    _bambooRewardedEventMethod?.Invoke(null, new object[] { revenue });
                }
                else if (IsBambooNonRewarded(adType))
                {
                    _bambooAdEventMethod?.Invoke(null, new object[] { revenue });
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ABIAds custom event forwarding failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private static bool EnsureMethods()
        {
            if (_lookupComplete)
            {
                return _troasEventMethod != null;
            }

            _lookupComplete = true;
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            Type t = typeof(ABILibsCustomEvent);
            _troasEventMethod = t.GetMethod("TROASEventOnMainThread", flags);
            _troasEvent2Method = t.GetMethod("TROASEvent2OnMainThread", flags);
            _bambooAdEventMethod = t.GetMethod("BambooAdEventOnMainThread", flags);
            _bambooRewardedEventMethod = t.GetMethod("BambooRewardedEventOnMainThread", flags);

            if (_troasEventMethod == null || _troasEvent2Method == null || _bambooAdEventMethod == null || _bambooRewardedEventMethod == null)
            {
                ReportMissingMethodsOnce();
                return false;
            }

            return true;
        }

        private static void ReportMissingMethodsOnce()
        {
            if (_reportedMissingMethods)
            {
                return;
            }

            _reportedMissingMethods = true;
            Debug.LogWarning("ABIAds custom event forwarding: ABILibsCustomEvent revenue entry points are missing (unexpected embedded ABI version).");
        }

        private static bool TryParseRevenue(string revenue, out double value)
        {
            return double.TryParse(revenue, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                   || double.TryParse(revenue, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static string ResolveAdType(ABIAdsEvent adsEvent)
        {
            if (!string.IsNullOrWhiteSpace(adsEvent.adType))
            {
                return adsEvent.adType;
            }

            return !string.IsNullOrWhiteSpace(adsEvent.placement) && PlacementTypes.TryGetValue(adsEvent.placement, out string adType)
                ? adType
                : string.Empty;
        }

        private static string NormalizeAdFormat(string adType)
        {
            if (string.IsNullOrWhiteSpace(adType))
            {
                return string.Empty;
            }

            string normalized = adType.Trim().ToLowerInvariant();
            return normalized == "mrec" ? "mrec" : normalized;
        }

        private static bool IsRewarded(string adType)
        {
            if (string.IsNullOrWhiteSpace(adType))
            {
                return false;
            }

            string normalized = adType.Trim().ToLowerInvariant();
            return normalized == "rewarded" || normalized == "rewarded_interstitial";
        }

        private static bool IsBambooNonRewarded(string adType)
        {
            if (string.IsNullOrWhiteSpace(adType))
            {
                return true;
            }

            string normalized = adType.Trim().ToLowerInvariant();
            return normalized == "interstitial" || normalized == "app_open";
        }

        [Serializable]
        private sealed class PlacementsRoot
        {
            public PlacementConfig[] placements;
        }

        [Serializable]
        private sealed class PlacementConfig
        {
            public string ad_name;
            public string ads_type;
        }
    }
}
