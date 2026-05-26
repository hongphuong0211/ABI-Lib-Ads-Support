#if UNITY_EDITOR
namespace ABI.Ads.UnityBridge.Editor
{
    internal static class ABIAdsConfigFileProbe
    {
        internal static bool LooksLikeEncryptedPayload(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            var parts = content.Trim().Split(':');
            return parts.Length == 3 && parts[0].Length > 0 && parts[1].Length > 0 && parts[2].Length > 0;
        }
    }
}
#endif
