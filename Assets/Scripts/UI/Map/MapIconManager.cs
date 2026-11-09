using UnityEngine;

using UnityEngine.SceneManagement;

using System.Collections.Generic;



namespace UI.Map

{

    [System.Serializable]

    public class MapZone

    {

        public string zoneName; // 須與場景名稱一致：Shallow / Mid / Deep



        [Header("World Space bounds for this zone")]

        public Vector2 worldMinBounds;

        public Vector2 worldMaxBounds;



        [Header("運算用 Map UI (0~1，三層拼圖 1450×2554)")]

        [Tooltip("useThreeLayerCompositeLayout 開啟時會依 1+2+3 圖尺寸自動覆寫")]

        public Vector2 mapUIMinBounds;

        public Vector2 mapUIMaxBounds;



        [Header("展示用 Map UI（全貌圖，可選）")]

        [Tooltip("勾選後：圖示／迷霧在全貌上的位置用下方 Min/Max，不再用自動置中換算")]

        public bool useCustomDisplayMapBounds;

        public Vector2 displayMapUIMinBounds;

        public Vector2 displayMapUIMaxBounds;



        [Tooltip("勾選後：世界 Y 變小時圖示往大地圖「上」移。三層拼圖對齊後通常不必勾")]

        public bool invertMapVertical;

    }



    public class MapIconManager : MonoBehaviour

    {

        [Header("Map Zones")]

        public List<MapZone> mapZones = new List<MapZone>();



        [Header("運算：三層拼圖（1 淺海 + 2 中海 + 3 深海）")]

        [Tooltip("世界座標 → 0~1 時，依小地圖三層像素（1450×2554）計算")]

        public bool useThreeLayerCompositeLayout = true;



        [Range(0f, 0.2f)]

        public float mapUIEdgeInset = 0f;



        [Tooltip("僅在未分離展示圖時：MapImage 設成 1450×2554")]

        public bool syncMapRectToCompositeSize = false;



        [Header("展示 vs 運算（兩張圖）")]

        [Tooltip("開啟：MapImage 可綁「全貌」大圖；座標仍用三層拼圖運算，再映射到全貌上")]

        public bool useSeparateDisplayMap = true;



        public Vector2 displayMapPixelSize = new Vector2(

            MapCompositeLayout.DefaultDisplayWidth,

            MapCompositeLayout.DefaultDisplayHeight);



        [Tooltip("展示圖比運算圖大時：將 1450×2554 置中對齊到展示圖上；兩張同尺寸可關閉")]

        public bool centerCompositeOnDisplayMap = true;



        [Tooltip("開啟分離展示圖時：MapImage Rect 設成 displayMapPixelSize")]

        public bool syncDisplayMapSize = true;



        [Header("Bounds Source")]

        public bool preferWorldBoundsInstance = true;



        [Header("Map UI RectTransform")]

        public RectTransform mapRect;



        [Header("Icons")]

        public RectTransform playerIcon;

        private Transform playerTransform;



        private Vector2 _displayOffsetNorm;

        private Vector2 _displayScaleNorm;

        private bool _displayTransformCached;



        public MapZone currentZone { get; private set; }



        private void Awake()

        {

            SceneManager.sceneLoaded += OnSceneLoaded;

            ApplyMapRectSize();

            CacheDisplayTransform();

            if (useThreeLayerCompositeLayout)

                ApplyCompositeLayoutToAllZones();

        }



        private void OnDestroy()

        {

            SceneManager.sceneLoaded -= OnSceneLoaded;

        }



        private void Start()

        {

            FindPlayer();

            UpdateCurrentZone();

        }



        private void OnEnable()

        {

            FindPlayer();

            if (playerTransform != null && playerIcon != null)

            {

                UpdateCurrentZone();

                UpdateIconPosition(playerIcon, playerTransform.position);

            }

        }



        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)

        {

            playerTransform = null;

            currentZone = null;

            FindPlayer();

            UpdateCurrentZone();

            if (playerTransform != null && playerIcon != null)

                UpdateIconPosition(playerIcon, playerTransform.position);

        }



        void ApplyMapRectSize()

        {

            if (mapRect == null) return;

            if (useSeparateDisplayMap && syncDisplayMapSize)

                mapRect.sizeDelta = displayMapPixelSize;

            else if (syncMapRectToCompositeSize)

                mapRect.sizeDelta = MapCompositeLayout.CompositeSize;

        }



        void CacheDisplayTransform()

        {

            _displayTransformCached = false;

            if (!useSeparateDisplayMap || !centerCompositeOnDisplayMap) return;

            MapCompositeLayout.GetCompositePlacementOnDisplay(displayMapPixelSize, out _displayOffsetNorm, out _displayScaleNorm);

            _displayTransformCached = true;

        }



        /// <summary>迷霧貼圖應使用的像素尺寸（與展示圖一致）</summary>

        public Vector2 GetFogTexturePixelSize()

        {

            if (useSeparateDisplayMap)

                return displayMapPixelSize;

            return MapCompositeLayout.CompositeSize;

        }



        private void FindPlayer()

        {

            if (playerTransform == null)

            {

                GameObject player = GameObject.FindGameObjectWithTag("Player");

                if (player != null)

                    playerTransform = player.transform;

                else

                    Debug.LogWarning("[MapIconManager] 找不到 Tag 為 Player 的物件！");

            }

        }



        private void Update()

        {

            if (playerTransform != null && playerIcon != null && playerIcon.gameObject.activeInHierarchy)

            {

                UpdateCurrentZone();

                UpdateIconPosition(playerIcon, playerTransform.position);

            }

        }



        private void UpdateCurrentZone()

        {

            string sceneName = SceneManager.GetActiveScene().name;

            var foundZone = mapZones.Find(z => z.zoneName == sceneName);

            if (foundZone != null)

                currentZone = foundZone;

            else

                Debug.LogWarning($"[MapIconManager] 找不到 MapZone：'{sceneName}'（須為 Shallow / Mid / Deep）");

        }



        /// <summary>運算用三層拼圖上的 0~1 區塊</summary>

        public void GetResolvedLogicMapUIBounds(MapZone zone, out Vector2 uiMin, out Vector2 uiMax)

        {

            if (zone != null && useThreeLayerCompositeLayout &&

                MapCompositeLayout.TryGetMapUIBounds(zone.zoneName, mapUIEdgeInset, out uiMin, out uiMax))

                return;



            uiMin = new Vector2(

                Mathf.Min(zone.mapUIMinBounds.x, zone.mapUIMaxBounds.x),

                Mathf.Min(zone.mapUIMinBounds.y, zone.mapUIMaxBounds.y));

            uiMax = new Vector2(

                Mathf.Max(zone.mapUIMinBounds.x, zone.mapUIMaxBounds.x),

                Mathf.Max(zone.mapUIMinBounds.y, zone.mapUIMaxBounds.y));

        }



        /// <summary>向後相容：等同運算區塊</summary>

        public void GetResolvedMapUIBounds(MapZone zone, out Vector2 uiMin, out Vector2 uiMax)

            => GetResolvedLogicMapUIBounds(zone, out uiMin, out uiMax);



        public void GetResolvedDisplayMapUIBounds(MapZone zone, out Vector2 uiMin, out Vector2 uiMax)

        {

            if (zone != null && zone.useCustomDisplayMapBounds)

            {

                uiMin = new Vector2(

                    Mathf.Min(zone.displayMapUIMinBounds.x, zone.displayMapUIMaxBounds.x),

                    Mathf.Min(zone.displayMapUIMinBounds.y, zone.displayMapUIMaxBounds.y));

                uiMax = new Vector2(

                    Mathf.Max(zone.displayMapUIMinBounds.x, zone.displayMapUIMaxBounds.x),

                    Mathf.Max(zone.displayMapUIMinBounds.y, zone.displayMapUIMaxBounds.y));

                return;

            }



            GetResolvedLogicMapUIBounds(zone, out uiMin, out uiMax);

        }



        [ContextMenu("Apply Composite Layout To All Zones")]

        public void ApplyCompositeLayoutToAllZones()

        {

            foreach (var zone in mapZones)

                MapCompositeLayout.ApplyInsetToZone(zone, mapUIEdgeInset);

        }



        bool TryGetLocalNormalizedInZone(Vector3 worldPosition, out float localX, out float localY)

        {

            localX = localY = 0.5f;

            if (currentZone == null) return false;



            Vector2 worldMin = currentZone.worldMinBounds;

            Vector2 worldMax = currentZone.worldMaxBounds;

            if (preferWorldBoundsInstance && WorldBounds.Instance != null)

            {

                worldMin = new Vector2(WorldBounds.Instance.minX, WorldBounds.Instance.minY);

                worldMax = new Vector2(WorldBounds.Instance.maxX, WorldBounds.Instance.maxY);

            }



            float minX = Mathf.Min(worldMin.x, worldMax.x);

            float maxX = Mathf.Max(worldMin.x, worldMax.x);

            float minY = Mathf.Min(worldMin.y, worldMax.y);

            float maxY = Mathf.Max(worldMin.y, worldMax.y);



            localX = Mathf.InverseLerp(minX, maxX, worldPosition.x);

            localY = Mathf.InverseLerp(minY, maxY, worldPosition.y);

            if (currentZone.invertMapVertical)

                localY = 1f - localY;

            return true;

        }



        /// <summary>運算用：小地圖三層 1450×2554 上的 0~1 座標</summary>

        public Vector2 WorldToLogicNormalizedMapPosition(Vector3 worldPosition)

        {

            UpdateCurrentZone();

            if (currentZone == null) return new Vector2(0.5f, 0.5f);

            if (!TryGetLocalNormalizedInZone(worldPosition, out float localX, out float localY))

                return new Vector2(0.5f, 0.5f);



            GetResolvedLogicMapUIBounds(currentZone, out Vector2 uiMin, out Vector2 uiMax);

            return new Vector2(

                Mathf.Lerp(uiMin.x, uiMax.x, localX),

                Mathf.Lerp(uiMin.y, uiMax.y, localY));

        }



        /// <summary>展示用：全貌（或其它展示圖）上的 0~1 座標，給圖示與迷霧用</summary>

        public Vector2 WorldToDisplayNormalizedMapPosition(Vector3 worldPosition)

        {

            UpdateCurrentZone();

            if (currentZone == null) return new Vector2(0.5f, 0.5f);



            if (!useSeparateDisplayMap)

                return WorldToLogicNormalizedMapPosition(worldPosition);



            if (!TryGetLocalNormalizedInZone(worldPosition, out float localX, out float localY))

                return new Vector2(0.5f, 0.5f);



            if (currentZone.useCustomDisplayMapBounds)

            {

                GetResolvedDisplayMapUIBounds(currentZone, out Vector2 dMin, out Vector2 dMax);

                return new Vector2(

                    Mathf.Lerp(dMin.x, dMax.x, localX),

                    Mathf.Lerp(dMin.y, dMax.y, localY));

            }



            Vector2 logic = WorldToLogicNormalizedMapPosition(worldPosition);

            if (!_displayTransformCached)

                CacheDisplayTransform();

            if (_displayTransformCached)

                return MapCompositeLayout.LogicNormalizedToDisplay(logic, _displayOffsetNorm, _displayScaleNorm);



            return logic;

        }



        /// <summary>圖示／迷霧用（展示圖座標）</summary>

        public Vector2 WorldToNormalizedMapPosition(Vector3 worldPosition)

            => WorldToDisplayNormalizedMapPosition(worldPosition);



        public void UpdateIconPosition(RectTransform iconRect, Vector3 worldPosition)

        {

            if (mapRect == null) return;



            Vector2 normalizedPos = WorldToDisplayNormalizedMapPosition(worldPosition);

            float mapWidth = mapRect.rect.width;

            float mapHeight = mapRect.rect.height;

            Vector2 pivot = mapRect.pivot;



            float localX = Mathf.Lerp(-mapWidth * pivot.x, mapWidth * (1f - pivot.x), normalizedPos.x);

            float localY = Mathf.Lerp(-mapHeight * pivot.y, mapHeight * (1f - pivot.y), normalizedPos.y);

            iconRect.localPosition = new Vector3(localX, localY, 0f);

        }



#if UNITY_EDITOR

        private void OnValidate()

        {

            CacheDisplayTransform();

            if (useThreeLayerCompositeLayout && mapZones != null && mapZones.Count > 0)

                ApplyCompositeLayoutToAllZones();

        }

#endif

    }

}


