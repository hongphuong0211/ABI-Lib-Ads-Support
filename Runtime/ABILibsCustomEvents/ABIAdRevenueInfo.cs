namespace ABILibsSDK
{
    /// <summary>
    /// Ad format aligned with placement <c>ads_type</c> values from ABI Ads config.
    /// </summary>
    public enum ABIAdFormat
    {
        Unknown = 0,
        Interstitial,
        Rewarded,
        Banner,
        Mrec,
        Native,
        AppOpen,
        RewardedInterstitial
    }

    public static class ABIAdFormatExtensions
    {
        public static ABIAdFormat Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ABIAdFormat.Unknown;
            }

            switch (value.Trim().ToLowerInvariant())
            {
                case "interstitial": return ABIAdFormat.Interstitial;
                case "rewarded": return ABIAdFormat.Rewarded;
                case "banner": return ABIAdFormat.Banner;
                case "mrec": return ABIAdFormat.Mrec;
                case "native": return ABIAdFormat.Native;
                case "app_open": return ABIAdFormat.AppOpen;
                case "rewarded_interstitial": return ABIAdFormat.RewardedInterstitial;
                default: return ABIAdFormat.Unknown;
            }
        }

        public static bool IsBannerOrMrec(this ABIAdFormat format) =>
            format == ABIAdFormat.Banner || format == ABIAdFormat.Mrec;

        /// <summary>Bamboo rewarded counter — <see cref="ABIAdFormat.Rewarded"/> only.</summary>
        public static bool IsBambooRewardAdType(this ABIAdFormat format) =>
            format == ABIAdFormat.Rewarded;

        /// <summary>Bamboo general ad counter — interstitial, rewarded, app open (and unknown fallback).</summary>
        public static bool IsBambooAdEventType(this ABIAdFormat format) =>
            format == ABIAdFormat.Unknown
            || format == ABIAdFormat.Interstitial
            || format == ABIAdFormat.Rewarded
            || format == ABIAdFormat.AppOpen;
    }

    /// <summary>
    /// Ad impression revenue snapshot for TROAS / Bamboo custom events.
    /// Decoupled from AppLovin MAX Unity SDK; native lib and <see cref="ABIAdsCustomEventForwarder"/> supply these values.
    /// </summary>
    public readonly struct ABIAdRevenueInfo
    {
        public double Revenue { get; }
        public ABIAdFormat AdFormat { get; }

        public ABIAdRevenueInfo(double revenue, ABIAdFormat adFormat)
        {
            Revenue = revenue;
            AdFormat = adFormat;
        }

        public ABIAdRevenueInfo(double revenue, string adFormat)
            : this(revenue, ABIAdFormatExtensions.Parse(adFormat))
        {
        }
    }
}
