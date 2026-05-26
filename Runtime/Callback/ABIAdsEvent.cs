using System;
using UnityEngine;

namespace ABI.Ads.UnityBridge
{
    [Serializable]
    public sealed class ABIAdsEvent
    {
        public string eventName;
        public string placement;
        public string error;
        public string rewardType;
        public int rewardAmount;
        public string revenue;
        public string currency;
        public string adUnitId;
        public string adType;
        public string network;
        public int mediationProvider;
        public bool ready;
        public bool remoteApplied;
        public string platform;

        [NonSerialized] public string rawJson;

        public static ABIAdsEvent FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new ABIAdsEvent
                {
                    eventName = "empty_payload",
                    rawJson = string.Empty
                };
            }

            try
            {
                var result = JsonUtility.FromJson<ABIAdsEvent>(json);
                if (result == null)
                {
                    return new ABIAdsEvent
                    {
                        eventName = "invalid_payload",
                        rawJson = json
                    };
                }

                result.rawJson = json;
                if (string.IsNullOrEmpty(result.eventName))
                {
                    result.eventName = "unknown";
                }

                return result;
            }
            catch (Exception)
            {
                return new ABIAdsEvent
                {
                    eventName = "invalid_json",
                    rawJson = json
                };
            }
        }
    }
}
