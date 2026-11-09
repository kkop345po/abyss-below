using UnityEngine;
using UnityEngine.UI;

namespace UI.Map
{
    [RequireComponent(typeof(RawImage))]
    public class MapFogOfWar : MonoBehaviour
    {
        public enum FogFillMode
        {
            [Tooltip("半透明黑霧")]
            SolidColor,
            [Tooltip("使用 Fog Overlay Texture 當迷霧底圖，再依玩家位置擦掉")]
            OverlayImage
        }

        [Header("迷霧來源")]
        public FogFillMode fogFillMode = FogFillMode.SolidColor;

        [Tooltip("整張地圖的霧色")]
        public Color fogColor = new Color(0f, 0f, 0f, 0.82f);

        [Header("OverlayImage 才需要")]
        [Tooltip("黑霧")]
        public Texture2D fogOverlayTexture;
        public Sprite fogOverlaySprite;

        [Header("Fog Texture Size")]
        public int textureWidth = 1024;
        public int textureHeight = 1024;

        [Header("Save")]
        public string saveKey = "MapFog_PreBoss";

        [Header("Reveal Settings")]
        public float revealWorldRadius = 10f;

        [Range(0f, 1f)]
        public float revealEdgeSoftness = 0.35f;

        [Header("Map References")]
        public MapIconManager mapIconManager;

        private RawImage fogImage;
        private Texture2D fogTexture;
        private Color[] _pixelBuffer;
        private Vector3 lastRevealPos = new Vector3(99999, 99999, 99999);
        private bool _runtimeInitialized;

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            SyncTextureSizeFromMapManager();

            if (fogImage == null)
                fogImage = GetComponent<RawImage>();

            if (fogTexture == null)
                InitializeFogTexture();
            else if (fogImage != null && fogImage.texture != fogTexture)
                fogImage.texture = fogTexture;

            if (fogImage != null)
            {
                fogImage.color = Color.white;
                fogImage.texture = fogTexture;
            }

            if (!_runtimeInitialized)
            {
                if (PlayerPrefs.HasKey(saveKey))
                    LoadFogData();
                else
                    FillFogFromSource();

                _pixelBuffer = fogTexture.GetPixels();

                if (GetAverageAlpha() < 0.08f)
                {
                    Debug.LogWarning($"[MapFogOfWar] 迷霧幾乎全透明，已重設。Key={saveKey}（可右鍵 Clear Fog Save）");
                    FillFogFromSource();
                    _pixelBuffer = fogTexture.GetPixels();
                }

                _runtimeInitialized = true;
            }

            ApplyTextureToRawImage();
        }

        void ApplyTextureToRawImage()
        {
            if (fogImage != null && fogTexture != null)
                fogImage.texture = fogTexture;
        }

        void SyncTextureSizeFromMapManager()
        {
            if (mapIconManager == null) return;
            Vector2 size = mapIconManager.GetFogTexturePixelSize();
            int w = Mathf.Max(64, Mathf.RoundToInt(size.x));
            int h = Mathf.Max(64, Mathf.RoundToInt(size.y));
            if (w == textureWidth && h == textureHeight) return;
            textureWidth = w;
            textureHeight = h;
            _runtimeInitialized = false;
            if (fogTexture != null)
            {
                Destroy(fogTexture);
                fogTexture = null;
            }
            _pixelBuffer = null;
        }

        public void ForceRevealNow(Vector3 worldPosition)
        {
            EnsureInitialized();
            lastRevealPos = new Vector3(99999f, 99999f, 99999f);
            TryRevealFog(worldPosition);
        }

        [ContextMenu("Reset Fog From Source")]
        public void ResetFogFromSourceImage()
        {
            if (fogTexture == null)
                InitializeFogTexture();
            FillFogFromSource();
            _pixelBuffer = fogTexture.GetPixels();
            ApplyTextureToRawImage();
            PlayerPrefs.DeleteKey(saveKey);
        }

        [ContextMenu("Clear Fog Save")]
        public void ClearFogSave()
        {
            PlayerPrefs.DeleteKey(saveKey);
            ResetFogFromSourceImage();
        }

        void InitializeFogTexture()
        {
            fogTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            fogTexture.wrapMode = TextureWrapMode.Clamp;
            fogTexture.filterMode = FilterMode.Bilinear;
        }

        public void FillFogFromSource()
        {
            if (fogTexture == null)
                InitializeFogTexture();

            if (fogFillMode == FogFillMode.SolidColor)
            {
                FillSolidColor(fogColor);
                return;
            }

            Texture2D source = ResolveSourceTexture();
            if (source == null)
            {
                Debug.LogWarning("[MapFogOfWar] OverlayImage 模式但未指定迷霧圖，改用 SolidColor。");
                FillSolidColor(fogColor);
                return;
            }

            if (source.width == textureWidth && source.height == textureHeight && source.isReadable)
            {
                fogTexture.SetPixels(source.GetPixels());
                fogTexture.Apply();
                return;
            }

            BlitTextureToFog(source);
        }

        Texture2D ResolveSourceTexture()
        {
            if (fogOverlayTexture != null)
                return fogOverlayTexture;
            if (fogOverlaySprite != null)
                return fogOverlaySprite.texture;
            return null;
        }

        void FillSolidColor(Color color)
        {
            int count = textureWidth * textureHeight;
            Color[] pixels = new Color[count];
            for (int i = 0; i < count; i++)
                pixels[i] = color;
            fogTexture.SetPixels(pixels);
            fogTexture.Apply();
        }

        void BlitTextureToFog(Texture source)
        {
            RenderTexture rt = RenderTexture.GetTemporary(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture prev = RenderTexture.active;
            try
            {
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                fogTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
                fogTexture.Apply();
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        float GetAverageAlpha()
        {
            if (_pixelBuffer == null || _pixelBuffer.Length == 0) return 1f;
            float sum = 0f;
            for (int i = 0; i < _pixelBuffer.Length; i++)
                sum += _pixelBuffer[i].a;
            return sum / _pixelBuffer.Length;
        }

        public void TryRevealFog(Vector3 worldPosition)
        {
            if (mapIconManager == null) return;
            EnsureInitialized();

            if (Vector3.Distance(lastRevealPos, worldPosition) < revealWorldRadius * 0.2f)
                return;

            lastRevealPos = worldPosition;
            RevealFog(worldPosition);
        }

        void RevealFog(Vector3 worldPosition)
        {
            if (mapIconManager == null || fogTexture == null) return;

            if (_pixelBuffer == null || _pixelBuffer.Length != textureWidth * textureHeight)
                _pixelBuffer = fogTexture.GetPixels();

            Vector2 normalizedPos = mapIconManager.WorldToNormalizedMapPosition(worldPosition);
            int pixelX = Mathf.RoundToInt(normalizedPos.x * (textureWidth - 1));
            int pixelY = Mathf.RoundToInt(normalizedPos.y * (textureHeight - 1));

            float pixelRadius = revealWorldRadius;
            MapZone currentZone = mapIconManager.currentZone;
            if (currentZone != null)
            {
                float zoneWorldWidth = Mathf.Abs(currentZone.worldMaxBounds.x - currentZone.worldMinBounds.x);
                mapIconManager.GetResolvedDisplayMapUIBounds(currentZone, out Vector2 uiMin, out Vector2 uiMax);
                float zonePixelWidth = (uiMax.x - uiMin.x) * textureWidth;
                if (zoneWorldWidth > 0.01f)
                    pixelRadius = (revealWorldRadius / zoneWorldWidth) * zonePixelWidth;
            }

            int radiusInt = Mathf.Max(2, Mathf.RoundToInt(pixelRadius));
            float innerRadius = radiusInt * (1f - revealEdgeSoftness);
            float radiusSq = radiusInt * radiusInt;
            float innerRadiusSq = innerRadius * innerRadius;

            int startX = Mathf.Clamp(pixelX - radiusInt, 0, textureWidth - 1);
            int endX = Mathf.Clamp(pixelX + radiusInt, 0, textureWidth - 1);
            int startY = Mathf.Clamp(pixelY - radiusInt, 0, textureHeight - 1);
            int endY = Mathf.Clamp(pixelY + radiusInt, 0, textureHeight - 1);

            bool textureModified = false;

            for (int y = startY; y <= endY; y++)
            {
                int row = y * textureWidth;
                for (int x = startX; x <= endX; x++)
                {
                    float dx = x - pixelX;
                    float dy = y - pixelY;
                    float distSq = dx * dx + dy * dy;
                    if (distSq > radiusSq) continue;

                    int i = row + x;
                    ref Color c = ref _pixelBuffer[i];
                    if (c.a < 0.02f) continue;

                    float erase = 1f;
                    if (revealEdgeSoftness > 0.001f && distSq > innerRadiusSq)
                    {
                        float t = (Mathf.Sqrt(distSq) - innerRadius) / Mathf.Max(0.001f, radiusInt - innerRadius);
                        erase = 1f - Mathf.Clamp01(t);
                    }

                    c.a *= 1f - erase;
                    if (c.a < 0.02f)
                    {
                        c.a = 0f;
                        c.r = c.g = c.b = 0f;
                    }
                    textureModified = true;
                }
            }

            if (textureModified)
            {
                fogTexture.SetPixels(_pixelBuffer);
                fogTexture.Apply();
                ApplyTextureToRawImage();
            }
        }

        public void SaveFogData()
        {
            if (fogTexture == null) return;
            byte[] bytes = fogTexture.GetRawTextureData();
            PlayerPrefs.SetString(saveKey, System.Convert.ToBase64String(bytes));
            PlayerPrefs.Save();
        }

        public void LoadFogData()
        {
            if (fogTexture == null)
                InitializeFogTexture();

            if (!PlayerPrefs.HasKey(saveKey))
            {
                FillFogFromSource();
                return;
            }

            try
            {
                byte[] bytes = System.Convert.FromBase64String(PlayerPrefs.GetString(saveKey));
                if (bytes.Length != fogTexture.GetRawTextureData().Length)
                {
                    FillFogFromSource();
                    return;
                }
                fogTexture.LoadRawTextureData(bytes);
                fogTexture.Apply();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[MapFogOfWar] 讀取迷霧失敗: " + e.Message);
                FillFogFromSource();
            }
        }

        private void OnDestroy()
        {
            SaveFogData();
        }
    }
}
