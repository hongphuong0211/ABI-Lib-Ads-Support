namespace ABI.Ads.UnityBridge.Editor
{
    /// <summary>Unity Android Gradle conventions shared by post-processors.</summary>
    internal static class ABIAdsUnityAndroidBuild
    {
        internal const string LifecycleVersion = "2.6.2";

        internal static bool IsUnity6OrNewer
        {
            get
            {
#if UNITY_6000_0_OR_NEWER
                return true;
#else
                return false;
#endif
            }
        }
    }
}
