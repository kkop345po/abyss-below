using UnityEngine;

namespace UI.Map
{
    /// <summary>
    /// 小地圖標註尺寸（由下而上：深海 988 → 中海 914 → 淺海 652，總圖 1450×2554）。
    /// Y 比例一律用「該層像素高 ÷ 2554」累加。
    /// </summary>
    public static class MapCompositeLayout
    {
        public const int TotalWidth = 1450;
        public const int TotalHeight = 2554;

        public const int DefaultDisplayWidth = TotalWidth;
        public const int DefaultDisplayHeight = TotalHeight;

        public const int DeepWidth = 1450;
        public const int DeepHeight = 988;
        public const int MidWidth = 1450;
        public const int MidHeight = 914;
        public const int ShallowWidth = 1450;
        public const int ShallowHeight = 652;

        public const float DeepYMax = (float)DeepHeight / TotalHeight;
        public const float MidYMin = DeepYMax;
        public const float MidYMax = (float)(DeepHeight + MidHeight) / TotalHeight;
        public const float ShallowYMin = MidYMax;
        public const float ShallowYMax = (float)(DeepHeight + MidHeight + ShallowHeight) / TotalHeight;

        public static Vector2 CompositeSize => new Vector2(TotalWidth, TotalHeight);

        public static bool TryGetStripNormalizedBounds(string zoneName, out float x0, out float x1, out float y0, out float y1)
        {
            switch (zoneName)
            {
                case "Deep":
                    GetHorizontalBounds(DeepWidth, out x0, out x1);
                    y0 = 0f;
                    y1 = DeepYMax;
                    return true;
                case "Mid":
                    GetHorizontalBounds(MidWidth, out x0, out x1);
                    y0 = MidYMin;
                    y1 = MidYMax;
                    return true;
                case "Shallow":
                    GetHorizontalBounds(ShallowWidth, out x0, out x1);
                    y0 = ShallowYMin;
                    y1 = ShallowYMax;
                    return true;
                default:
                    x0 = x1 = y0 = y1 = 0f;
                    return false;
            }
        }

        static void GetHorizontalBounds(int stripWidth, out float x0, out float x1)
        {
            if (stripWidth >= TotalWidth)
            {
                x0 = 0f;
                x1 = 1f;
                return;
            }
            x0 = (TotalWidth - stripWidth) * 0.5f / TotalWidth;
            x1 = (TotalWidth + stripWidth) * 0.5f / TotalWidth;
        }

        public static bool TryGetMapUIBounds(string zoneName, float edgeInset, out Vector2 uiMin, out Vector2 uiMax)
        {
            if (!TryGetStripNormalizedBounds(zoneName, out float x0, out float x1, out float y0, out float y1))
            {
                uiMin = uiMax = Vector2.zero;
                return false;
            }

            InsetRect(x0, x1, y0, y1, edgeInset, out uiMin, out uiMax);
            return true;
        }

        public static void ApplyInsetToZone(MapZone zone, float edgeInset)
        {
            if (zone == null || !TryGetMapUIBounds(zone.zoneName, edgeInset, out Vector2 uiMin, out Vector2 uiMax))
                return;
            zone.mapUIMinBounds = uiMin;
            zone.mapUIMaxBounds = uiMax;
        }

        static void InsetRect(float x0, float x1, float y0, float y1, float inset, out Vector2 min, out Vector2 max)
        {
            float w = x1 - x0;
            float h = y1 - y0;
            inset = Mathf.Clamp(inset, 0f, 0.45f);
            min = new Vector2(x0 + w * inset, y0 + h * inset);
            max = new Vector2(x1 - w * inset, y1 - h * inset);
        }

        public static void GetCompositePlacementOnDisplay(Vector2 displayPixelSize, out Vector2 offsetNormalized, out Vector2 scaleNormalized)
        {
            float dw = Mathf.Max(1f, displayPixelSize.x);
            float dh = Mathf.Max(1f, displayPixelSize.y);
            offsetNormalized = new Vector2(
                (dw - TotalWidth) * 0.5f / dw,
                (dh - TotalHeight) * 0.5f / dh);
            scaleNormalized = new Vector2(TotalWidth / dw, TotalHeight / dh);
        }

        public static Vector2 LogicNormalizedToDisplay(Vector2 logicNormalized, Vector2 offsetNormalized, Vector2 scaleNormalized)
        {
            return new Vector2(
                offsetNormalized.x + logicNormalized.x * scaleNormalized.x,
                offsetNormalized.y + logicNormalized.y * scaleNormalized.y);
        }
    }
}
