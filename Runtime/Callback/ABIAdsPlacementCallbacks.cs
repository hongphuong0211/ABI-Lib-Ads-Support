using System;

namespace ABI.Ads.UnityBridge
{
    /// <summary>
    /// Per-placement ad callbacks. Register with <see cref="ABIAds.RegisterPlacement"/> using the placement name from config.
    /// </summary>
    public sealed class ABIAdsPlacementCallbacks
    {
        public Action<ABIAdsEvent> OnLoaded;
        public Action<ABIAdsEvent> OnFailed;
        public Action<ABIAdsEvent> OnImpression;
        public Action<ABIAdsEvent> OnClicked;
        public Action<ABIAdsEvent> OnClosed;
        public Action<ABIAdsEvent> OnDisplayFailed;
        public Action<ABIAdsEvent> OnRevenue;

        public Action<ABIAdsEvent> OnRewardGranted;
        public Action<ABIAdsEvent> OnRewardCompleted;

        public Action<ABIAdsEvent> OnBannerRequested;
        public Action<ABIAdsEvent> OnBannerHidden;
        public Action<ABIAdsEvent> OnBannerDestroyed;

        public Action<ABIAdsEvent> OnNativeRequested;
        public Action<ABIAdsEvent> OnNativeHidden;
        public Action<ABIAdsEvent> OnNativeDestroyed;

        internal void Invoke(string eventName, ABIAdsEvent adsEvent)
        {
            switch (eventName)
            {
                case ABIAdsEventNames.Loaded:
                    OnLoaded?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.Failed:
                    OnFailed?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.Impression:
                    OnImpression?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.Clicked:
                    OnClicked?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.Closed:
                    OnClosed?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.DisplayFailed:
                    OnDisplayFailed?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.Revenue:
                    OnRevenue?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.RewardGranted:
                    OnRewardGranted?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.RewardCompleted:
                    OnRewardCompleted?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.BannerRequested:
                    OnBannerRequested?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.BannerHidden:
                    OnBannerHidden?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.BannerDestroyed:
                    OnBannerDestroyed?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.NativeRequested:
                    OnNativeRequested?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.NativeHidden:
                    OnNativeHidden?.Invoke(adsEvent);
                    break;
                case ABIAdsEventNames.NativeDestroyed:
                    OnNativeDestroyed?.Invoke(adsEvent);
                    break;
            }
        }
    }
}
