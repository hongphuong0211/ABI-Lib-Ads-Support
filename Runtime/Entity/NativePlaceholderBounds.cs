using UnityEngine;

namespace ABI.Ads.UnityBridge
{
    public readonly struct NativePlaceholderBounds
    {
        public readonly float MinX;
        public readonly float MinY;
        public readonly float MaxX;
        public readonly float MaxY;

        public NativePlaceholderBounds(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public static NativePlaceholderBounds FullScreen => new NativePlaceholderBounds(0f, 0f, 1f, 1f);

        public static NativePlaceholderBounds BottomStrip(float heightPercent)
        {
            float h = Mathf.Clamp01(heightPercent);
            return new NativePlaceholderBounds(0f, 1f - h, 1f, 1f);
        }
    }
}
