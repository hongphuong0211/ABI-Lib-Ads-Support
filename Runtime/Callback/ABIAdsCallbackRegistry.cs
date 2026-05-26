using System;
using System.Collections.Generic;

namespace ABI.Ads.UnityBridge
{
    internal static class ABIAdsCallbackRegistry
    {
        private static readonly Dictionary<string, ABIAdsPlacementCallbacks> PlacementCallbacks =
            new Dictionary<string, ABIAdsPlacementCallbacks>(StringComparer.OrdinalIgnoreCase);

        private static string _activeBannerPlacement;
        private static string _activeNativePlacement;

        internal static void Register(string placement, ABIAdsPlacementCallbacks callbacks)
        {
            if (string.IsNullOrWhiteSpace(placement))
            {
                throw new ArgumentException("Placement name is required.", nameof(placement));
            }

            if (callbacks == null)
            {
                throw new ArgumentNullException(nameof(callbacks));
            }

            PlacementCallbacks[placement.Trim()] = callbacks;
        }

        internal static bool Unregister(string placement)
        {
            if (string.IsNullOrWhiteSpace(placement))
            {
                return false;
            }

            var key = placement.Trim();
            if (string.Equals(_activeBannerPlacement, key, StringComparison.OrdinalIgnoreCase))
            {
                _activeBannerPlacement = null;
            }

            if (string.Equals(_activeNativePlacement, key, StringComparison.OrdinalIgnoreCase))
            {
                _activeNativePlacement = null;
            }

            return PlacementCallbacks.Remove(key);
        }

        internal static void Clear()
        {
            PlacementCallbacks.Clear();
            _activeBannerPlacement = null;
            _activeNativePlacement = null;
        }

        internal static void Dispatch(ABIAdsEvent adsEvent)
        {
            if (adsEvent == null || string.IsNullOrEmpty(adsEvent.eventName))
            {
                return;
            }

            TrackActivePlacements(adsEvent);

            if (!TryResolvePlacement(adsEvent, out string placement))
            {
                return;
            }

            if (PlacementCallbacks.TryGetValue(placement, out ABIAdsPlacementCallbacks handlers))
            {
                handlers.Invoke(adsEvent.eventName, adsEvent);
            }
        }

        private static void TrackActivePlacements(ABIAdsEvent adsEvent)
        {
            if (!string.IsNullOrWhiteSpace(adsEvent.placement))
            {
                if (string.Equals(adsEvent.eventName, ABIAdsEventNames.BannerRequested, StringComparison.OrdinalIgnoreCase))
                {
                    _activeBannerPlacement = adsEvent.placement;
                }
                else if (string.Equals(adsEvent.eventName, ABIAdsEventNames.NativeRequested, StringComparison.OrdinalIgnoreCase))
                {
                    _activeNativePlacement = adsEvent.placement;
                }
            }

            if (string.Equals(adsEvent.eventName, ABIAdsEventNames.BannerDestroyed, StringComparison.OrdinalIgnoreCase))
            {
                _activeBannerPlacement = null;
            }
            else if (string.Equals(adsEvent.eventName, ABIAdsEventNames.NativeDestroyed, StringComparison.OrdinalIgnoreCase))
            {
                _activeNativePlacement = null;
            }
        }

        private static bool TryResolvePlacement(ABIAdsEvent adsEvent, out string placement)
        {
            if (!string.IsNullOrWhiteSpace(adsEvent.placement))
            {
                placement = adsEvent.placement.Trim();
                return true;
            }

            switch (adsEvent.eventName)
            {
                case ABIAdsEventNames.BannerHidden:
                case ABIAdsEventNames.BannerDestroyed:
                    placement = _activeBannerPlacement;
                    return !string.IsNullOrEmpty(placement);

                case ABIAdsEventNames.NativeHidden:
                case ABIAdsEventNames.NativeDestroyed:
                    placement = _activeNativePlacement;
                    return !string.IsNullOrEmpty(placement);

                default:
                    placement = null;
                    return false;
            }
        }
    }
}
