using UnityEditor;

namespace ABI.Ads.UnityBridge.Editor
{
    internal static class ABIAdsConfigWindow
    {
        [MenuItem("ABI Ads/Configs/Edit Ads Config")]
        private static void OpenBoth()
        {
            ABIAdsGlobalConfigWindow.Open();
            ABIAdsPlacementConfigWindow.Open();
        }
    }
}
