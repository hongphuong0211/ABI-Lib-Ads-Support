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

            ABIAdFormat adFormat = ResolveAdFormat(adsEvent);

            try
            {
                _troasEventMethod?.Invoke(null, new object[] { revenue, adFormat });
                _troasEvent2Method?.Invoke(null, new object[] { revenue, adFormat });

                if (adFormat.IsBambooRewardAdType())
                {
                    _bambooRewardedEventMethod?.Invoke(null, new object[] { revenue });
                }

                if (adFormat.IsBambooAdEventType())
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

        private static ABIAdFormat ResolveAdFormat(ABIAdsEvent adsEvent)
        {
            if (!string.IsNullOrWhiteSpace(adsEvent.adType))
            {
                return ABIAdFormatExtensions.Parse(adsEvent.adType);
            }

            if (!string.IsNullOrWhiteSpace(adsEvent.placement) && PlacementTypes.TryGetValue(adsEvent.placement, out string adType))
            {
                return ABIAdFormatExtensions.Parse(adType);
            }

            return ABIAdFormat.Unknown;
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
