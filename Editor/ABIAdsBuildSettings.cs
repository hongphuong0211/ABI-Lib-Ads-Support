namespace ABI.Ads.UnityBridge.Editor
{
    internal sealed class ABIAdsBuildSettings
    {
        public int mediation_provider;
        public string admob_app_id = ABIAdsEditorPaths.DefaultGoogleMobileAdsAppId;
        public string max_sdk_key = string.Empty;

        internal static ABIAdsBuildSettings Load()
        {
            var globalConfig = ABIAdsConfigStore.LoadGlobalConfig();
            var settings = new ABIAdsBuildSettings
            {
                mediation_provider = globalConfig.mediation_provider,
                admob_app_id = globalConfig.admob_app_id,
                max_sdk_key = globalConfig.max_sdk_key
            };
            settings.EnsureDefaults();
            return settings;
        }

        private void EnsureDefaults()
        {
            if (string.IsNullOrWhiteSpace(admob_app_id))
            {
                admob_app_id = ABIAdsEditorPaths.DefaultGoogleMobileAdsAppId;
            }

            admob_app_id = admob_app_id.Trim();
            max_sdk_key = (max_sdk_key ?? string.Empty).Trim();
        }

        internal bool RequiresAdmobAppId()
        {
            // 0 = AdMob, 2 = Dual
            return mediation_provider != 1;
        }

        internal bool RequiresMaxSdkKey()
        {
            // 1 = MAX, 2 = Dual
            return mediation_provider != 0;
        }

        internal static string ResolveEnvironmentString(string key, string fallback)
        {
            var environmentValue = System.Environment.GetEnvironmentVariable(key);
            return string.IsNullOrWhiteSpace(environmentValue) ? fallback : environmentValue.Trim();
        }

        internal static bool ResolveEnvironmentBool(string key)
        {
            var raw = System.Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            raw = raw.Trim();
            return raw == "1" || raw.Equals("true", System.StringComparison.OrdinalIgnoreCase) ||
                   raw.Equals("yes", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
