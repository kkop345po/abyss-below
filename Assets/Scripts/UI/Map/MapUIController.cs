using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace UI.Map
{
    public class MapUIController : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject mapPanel;
        
        [Header("Map References")]
        public Image mapImage;
        public Sprite preBossMapSprite;
        public Sprite postBossMapSprite;

        [Header("Fog of War References")]
        public MapFogOfWar preBossFog;
        public MapFogOfWar postBossFog;

        [Header("Settings")]
        public string bigBossSaveKey = "BigBoss_Dead"; // PlayerPrefs 或者是你的存檔 key
        public KeyCode mapKey = KeyCode.M;

        private bool isMapOpen = false;
        private Transform playerTransform;
        private MapCameraController mapCameraController;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            playerTransform = null;
            FindPlayer();
            EnsureFogBackendsInitialized();
            RevealFogAtPlayer(force: true);
        }

        private void Start()
        {
            if (mapPanel != null)
                mapPanel.SetActive(false);
                
            FindPlayer();
            // mapPanel 內的迷霧預設關閉時不會 Awake，延遲初始化並立刻依玩家位置抹霧
            Invoke(nameof(DelayedFogBootstrap), 0.2f);
        }

        void DelayedFogBootstrap()
        {
            FindPlayer();
            EnsureFogBackendsInitialized();
            RevealFogAtPlayer(force: true);
        }
        
        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }
        }

        private void Update()
        {
            // 持續記錄迷霧 (就算地圖面板隱藏、Fog組件被隱藏，我們依然可以主動在背景運算貼圖的透明度)
            if (playerTransform != null)
            {
                bool isBossDefeated = PlayerPrefs.GetInt(bigBossSaveKey, 0) == 1;
                if (isBossDefeated && postBossFog != null)
                {
                    postBossFog.EnsureInitialized();
                    postBossFog.TryRevealFog(playerTransform.position);
                }
                else if (!isBossDefeated && preBossFog != null)
                {
                    preBossFog.EnsureInitialized();
                    preBossFog.TryRevealFog(playerTransform.position);
                }
            }
            else
            {
                FindPlayer();
            }

            if (Input.GetKeyDown(mapKey))
            {
                ToggleMap();
            }
        }

        void EnsureFogBackendsInitialized()
        {
            preBossFog?.EnsureInitialized();
            postBossFog?.EnsureInitialized();
        }

        void RevealFogAtPlayer(bool force)
        {
            if (playerTransform == null) return;
            var pos = playerTransform.position;
            bool isBossDefeated = PlayerPrefs.GetInt(bigBossSaveKey, 0) == 1;
            if (isBossDefeated && postBossFog != null)
            {
                postBossFog.EnsureInitialized();
                if (force) postBossFog.ForceRevealNow(pos);
                else postBossFog.TryRevealFog(pos);
            }
            else if (!isBossDefeated && preBossFog != null)
            {
                preBossFog.EnsureInitialized();
                if (force) preBossFog.ForceRevealNow(pos);
                else preBossFog.TryRevealFog(pos);
            }
        }

        public void ToggleMap()
        {
            isMapOpen = !isMapOpen;

            if (isMapOpen)
            {
                OpenMap();
            }
            else
            {
                CloseMap();
            }
        }

        private void OpenMap()
        {
            UpdateMapState();
            FindPlayer();

            if (mapPanel != null)
            {
                mapPanel.SetActive(true);
            }

            EnsureFogBackendsInitialized();
            RevealFogAtPlayer(force: true);

            if (mapCameraController == null && mapPanel != null)
                mapCameraController = mapPanel.GetComponentInChildren<MapCameraController>(true);
            mapCameraController?.FitMapToViewport();

            if (mapPanel != null && mapPanel.GetComponent<PauseOnEnable>() == null)
                mapPanel.AddComponent<PauseOnEnable>();
        }

        public void CloseMap()
        {
            SaveActiveFogToDisk();

            if (mapPanel != null)
                mapPanel.SetActive(false);

            isMapOpen = false;
        }

        void SaveActiveFogToDisk()
        {
            bool isBossDefeated = PlayerPrefs.GetInt(bigBossSaveKey, 0) == 1;
            if (isBossDefeated && postBossFog != null)
                postBossFog.SaveFogData();
            else if (!isBossDefeated && preBossFog != null)
                preBossFog.SaveFogData();
        }

        private void UpdateMapState()
        {
            bool isBossDefeated = PlayerPrefs.GetInt(bigBossSaveKey, 0) == 1;

            if (isBossDefeated)
            {
                // 顯示擊敗 Boss 後的地圖與迷霧
                if (mapImage != null && postBossMapSprite != null)
                    mapImage.sprite = postBossMapSprite;

                if (preBossFog != null) preBossFog.gameObject.SetActive(false);
                if (postBossFog != null) postBossFog.gameObject.SetActive(true);
            }
            else
            {
                // 顯示擊敗 Boss 前的地圖與迷霧
                if (mapImage != null && preBossMapSprite != null)
                    mapImage.sprite = preBossMapSprite;

                if (preBossFog != null) preBossFog.gameObject.SetActive(true);
                if (postBossFog != null) postBossFog.gameObject.SetActive(false);
            }
        }
    }
}
