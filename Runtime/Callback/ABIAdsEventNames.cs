namespace ABI.Ads.UnityBridge
{
    public static class ABIAdsEventNames
    {
        public const string BridgeReady = "bridge_ready";
        public const string Initialized = "initialized";
        public const string ViewControllerUpdated = "view_controller_updated";
        public const string ConfigApplied = "config_applied";

        public const string Loaded = "loaded";
        public const string Failed = "failed";
        public const string Impression = "impression";
        public const string Clicked = "clicked";
        public const string Closed = "closed";
        public const string DisplayFailed = "display_failed";
        public const string Revenue = "revenue";

        public const string RewardGranted = "reward_granted";
        public const string RewardCompleted = "reward_completed";

        public const string BannerRequested = "banner_requested";
        public const string BannerHidden = "banner_hidden";
        public const string BannerDestroyed = "banner_destroyed";

        public const string NativeRequested = "native_requested";
        public const string NativeHidden = "native_hidden";
        public const string NativeDestroyed = "native_destroyed";
    }
}
