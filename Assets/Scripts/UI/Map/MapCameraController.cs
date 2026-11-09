using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Map
{
    public class MapCameraController : MonoBehaviour, IScrollHandler
    {
        [Header("References")]
        public RectTransform mapContent;
        public ScrollRect scrollRect;

        [Header("Fit To Screen")]
        [Tooltip("開地圖時自動縮放，讓整張 1450×2554 塞進視窗內（不會上面凸出）")]
        public bool fitToViewportOnOpen = true;

        [Tooltip("啟動時讓 Viewport 填滿 Scroll View")]
        public bool stretchViewportToParent = true;

        [Header("Zoom Settings")]
        public float zoomSpeed = 0.2f;
        public float minZoom = 0.4f;
        public float maxZoom = 2f;
        public float zoomSmoothness = 10f;

        private Vector3 _fitScale = Vector3.one;
        private float targetScaleFactor = 1f;

        private void Awake()
        {
            if (scrollRect == null)
                scrollRect = GetComponent<ScrollRect>();

            if (scrollRect != null && mapContent == null)
                mapContent = scrollRect.content;

            if (stretchViewportToParent)
                StretchViewport();

            if (fitToViewportOnOpen)
                FitMapToViewport();
        }

        void StretchViewport()
        {
            if (scrollRect == null || scrollRect.viewport == null) return;
            RectTransform vp = scrollRect.viewport;
            vp.anchorMin = Vector2.zero;
            vp.anchorMax = Vector2.one;
            vp.offsetMin = Vector2.zero;
            vp.offsetMax = Vector2.zero;
            vp.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>依視窗大小縮放地圖，與遊戲內白框（Scroll View）同範圍顯示。</summary>
        public void FitMapToViewport()
        {
            if (mapContent == null) return;
            RectTransform viewport = scrollRect != null ? scrollRect.viewport : null;
            if (viewport == null) return;

            Canvas.ForceUpdateCanvases();

            float mapW = mapContent.rect.width;
            float mapH = mapContent.rect.height;
            float vpW = viewport.rect.width;
            float vpH = viewport.rect.height;
            if (mapW < 1f || mapH < 1f || vpW < 1f || vpH < 1f) return;

            float scale = Mathf.Min(vpW / mapW, vpH / mapH);
            _fitScale = new Vector3(scale, scale, 1f);
            mapContent.localScale = _fitScale;
            mapContent.anchoredPosition = Vector2.zero;
            targetScaleFactor = scale;

            minZoom = scale * 0.5f;
            maxZoom = scale * 2.5f;

            if (scrollRect != null)
            {
                scrollRect.StopMovement();
                scrollRect.normalizedPosition = new Vector2(0.5f, 0.5f);
            }
        }

        private void Update()
        {
            if (mapContent == null) return;

            float currentScale = mapContent.localScale.x;
            if (!Mathf.Approximately(currentScale, targetScaleFactor))
            {
                float newScale = Mathf.Lerp(currentScale, targetScaleFactor, Time.unscaledDeltaTime * zoomSmoothness);
                mapContent.localScale = new Vector3(newScale, newScale, 1f);
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (mapContent == null) return;
            targetScaleFactor += eventData.scrollDelta.y * zoomSpeed;
            targetScaleFactor = Mathf.Clamp(targetScaleFactor, minZoom, maxZoom);
        }

        public void ResetZoom()
        {
            FitMapToViewport();
        }
    }
}
