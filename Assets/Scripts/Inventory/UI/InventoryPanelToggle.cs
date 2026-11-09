using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Inventory.UI
{
    /// <summary>
    /// 用於按鍵開關背包面板（預設 B）。
    /// </summary>
    public class InventoryPanelToggle : MonoBehaviour
    {
        public static InventoryPanelToggle Instance;

        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private KeyCode toggleKey = KeyCode.B;

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
            ResolveInventoryPanelReference(force: true);
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[InventoryPanelToggle] 初始化並設置為持久化");
            }
            else
            {
                Destroy(gameObject);
            }

            ResolveInventoryPanelReference(force: true);
        }

        private void Update()
        {
            if (inventoryPanel == null || !inventoryPanel.scene.IsValid() || !inventoryPanel.scene.isLoaded)
                ResolveInventoryPanelReference(force: true);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (inventoryPanel != null && inventoryPanel.activeSelf)
                {
                    inventoryPanel.SetActive(false);
                    return;
                }
            }

            if (Input.GetKeyDown(toggleKey))
            {
                if (inventoryPanel != null)
                {
                    inventoryPanel.SetActive(!inventoryPanel.activeSelf);
                }
                else
                {
                    Debug.LogWarning("[InventoryPanelToggle] 找不到 InventoryPanel，請確認目前場景有名為 InventoryPanel 的物件");
                }
            }
        }

        public void Open()
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(true);
        }

        public void Close()
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
        }

        private void ResolveInventoryPanelReference(bool force = false)
        {
            if (!force && inventoryPanel != null) return;

            // 先找啟用中的（快速）
            var activeFound = GameObject.Find("InventoryPanel");
            if (activeFound != null)
            {
                inventoryPanel = activeFound;
                return;
            }

            // 再找所有已載入場景中「包含未啟用」的同名物件
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null || t.name != "InventoryPanel") continue;
                var go = t.gameObject;
                if (!go.scene.IsValid() || !go.scene.isLoaded) continue;
                inventoryPanel = go;
                return;
            }
        }
    }
}


