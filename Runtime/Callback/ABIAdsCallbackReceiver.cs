using UnityEngine;

namespace ABI.Ads.UnityBridge
{
    internal sealed class ABIAdsCallbackReceiver : MonoBehaviour
    {
        public void OnABIAdsEvent(string json)
        {
            ABIAds.DispatchNativeEvent(json);
        }
    }
}
