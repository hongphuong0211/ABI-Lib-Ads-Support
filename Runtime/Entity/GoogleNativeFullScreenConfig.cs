namespace ABI.Ads.UnityBridge
{
    public readonly struct GoogleNativeFullScreenConfig
    {
        public readonly string Placement;
        public readonly string TemplateName;
        public readonly int CountDownSec;

        public GoogleNativeFullScreenConfig(
            string placement,
            string templateName = null,
            int countDownSec = 3)
        {
            Placement = placement;
            TemplateName = templateName;
            CountDownSec = countDownSec;
        }
    }
}
