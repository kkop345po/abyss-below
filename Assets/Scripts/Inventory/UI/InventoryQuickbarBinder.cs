using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Inventory.Items;

namespace Game.Inventory.UI
{
    /// <summary>
    /// 從背包（InventoryManager）讀取前 N 個可使用道具，綁定到 Quickbar。
    /// 透過 id→IUsableItem 的映射，找到場景中的實例。
    /// 本物件會 DontDestroyOnLoad，但 Quickbar UI 留在各場景內，因此會記住每格的 itemId，
    /// 在場景載入後重新尋找 QuickbarView 並還原外觀與綁定。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class InventoryQuickbarBinder : MonoBehaviour
    {
        [System.Serializable]
        public class ItemMapping
        {
            public string itemId;
            public MonoBehaviour usableComponent; // 需實作 IUsableItem
        }

        public static InventoryQuickbarBinder Instance { get; private set; }

        /// <summary>小／大補血在 ItemDatabase 的 itemId（與 Land 場景 SmallHeal、FullHeal 一致）。</summary>
        public const string SmallHealItemId = "1";
        public const string FullHealItemId = "2";

        [SerializeField] private QuickbarView quickbar;
        [SerializeField] private List<ItemMapping> mappings = new List<ItemMapping>();

        /// <summary>每格對應的背包 itemId；空字串或 null 表示該格未配置。</summary>
        private readonly string[] _slotItemIds = new string[4];

        private Coroutine _delayedReapplyCoroutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void BootstrapOnLoad()
        {
            PersistentInventoryUsables.EnsureExists();
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnSceneLoaded;
                PersistentInventoryUsables.EnsureExists();
                Debug.Log("[InventoryQuickbarBinder] 初始化並設置為持久化");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        private void Start()
        {
            if (quickbar == null)
                ResolveQuickbarInScene(SceneManager.GetActiveScene());
            ReapplyQuickbarAfterSceneChange();
            CleanupInvalidSlots();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_delayedReapplyCoroutine != null)
            {
                StopCoroutine(_delayedReapplyCoroutine);
                _delayedReapplyCoroutine = null;
            }
            _delayedReapplyCoroutine = StartCoroutine(DelayedReapplyAfterSceneLoaded(scene));
        }

        private IEnumerator DelayedReapplyAfterSceneLoaded(Scene scene)
        {
            // 等本幀 Start 跑完再還原快捷欄
            yield return new WaitForEndOfFrame();
            ResolveQuickbarInScene(scene);
            ReapplyQuickbarAfterSceneChange();
            CleanupInvalidSlots();
            _delayedReapplyCoroutine = null;
        }

        private void ResolveQuickbarInScene(Scene scene)
        {
            QuickbarView found = null;
            if (scene.IsValid())
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var q = root.GetComponentInChildren<QuickbarView>(true);
                    if (q != null)
                    {
                        found = q;
                        break;
                    }
                }
            }

            if (found == null)
                found = Object.FindFirstObjectByType<QuickbarView>(FindObjectsInactive.Include);

            quickbar = found;
        }

        /// <summary>玩家從背包放入快捷欄時呼叫，寫入跨場景記憶。</summary>
        public void RegisterSlotAssignment(int slotIndex, string itemId)
        {
            if (slotIndex < 0 || slotIndex >= _slotItemIds.Length) return;
            _slotItemIds[slotIndex] = string.IsNullOrEmpty(itemId) ? null : itemId;
        }

        private void ClearSlotPersistence(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _slotItemIds.Length)
                _slotItemIds[slotIndex] = null;
        }

        private bool HasAnyPersistedSlot()
        {
            for (int i = 0; i < _slotItemIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(_slotItemIds[i]))
                    return true;
            }
            return false;
        }

        private void SyncMemoryFromQuickbar()
        {
            if (quickbar == null) return;
            var slots = quickbar.GetSlots();
            if (slots == null) return;
            int n = Mathf.Min(slots.Length, _slotItemIds.Length);
            for (int i = 0; i < n; i++)
            {
                var bound = slots[i] != null ? slots[i].GetBoundItem() : null;
                _slotItemIds[i] = GetItemIdFromUsable(bound);
            }
        }

        private void ReapplyQuickbarAfterSceneChange()
        {
            bool anyPersisted = HasAnyPersistedSlot();

            if (quickbar == null)
                return;

            var slotViews = quickbar.GetSlots();
            if (slotViews == null)
                return;

            if (anyPersisted)
            {
                for (int i = 0; i < slotViews.Length; i++)
                    quickbar.ClearSlot(i);

                for (int i = 0; i < _slotItemIds.Length; i++)
                {
                    if (string.IsNullOrEmpty(_slotItemIds[i]))
                        continue;
                    var usable = GetUsableByItemId(_slotItemIds[i]);
                    if (usable == null)
                    {
                        Debug.LogWarning($"[InventoryQuickbarBinder] 場景中尚無可使用元件: {_slotItemIds[i]}，圖示待該道具進場後由 Refresh 還原。");
                        continue;
                    }
                    quickbar.BindSlot(i, usable);
                    ApplyInventoryCountToUsable(_slotItemIds[i], usable);
                }
            }
            else
                SyncMemoryFromQuickbar();
        }

        private static void ApplyInventoryCountToUsable(string itemId, Game.Inventory.IUsableItem usable)
        {
            if (InventoryManager.Instance == null || usable == null)
                return;
            var invItem = InventoryManager.Instance.GetItemById(itemId);
            if (invItem != null && usable is BaseUsableItem baseItem)
                baseItem.SetCurrentCount(invItem.quantity);
        }

        private void OnEnable()
        {
            InventoryManager.OnInventoryChanged += Refresh;
        }

        private void OnDisable()
        {
            InventoryManager.OnInventoryChanged -= Refresh;
        }

        /// <summary>
        /// 刷新快捷欄：只清理無效物品並更新數量，不自動填充新物品（由玩家手動選擇）
        /// </summary>
        public void Refresh()
        {
            if (quickbar == null)
                ResolveQuickbarInScene(SceneManager.GetActiveScene());
            if (quickbar == null || InventoryManager.Instance == null)
                return;

            CleanupInvalidSlots();
        }

        private void CleanupInvalidSlots()
        {
            if (quickbar == null || InventoryManager.Instance == null)
                return;
            var slots = quickbar.GetSlots();
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                var boundItem = slots[i].GetBoundItem();
                bool bindingDead = boundItem is MonoBehaviour deadMb && deadMb == null;
                if ((boundItem == null || bindingDead) && !string.IsNullOrEmpty(_slotItemIds[i]))
                {
                    var usable = GetUsableByItemId(_slotItemIds[i]);
                    if (usable != null)
                    {
                        quickbar.BindSlot(i, usable);
                        ApplyInventoryCountToUsable(_slotItemIds[i], usable);
                        boundItem = usable;
                    }
                }

                if (boundItem == null || boundItem is MonoBehaviour deadMb2 && deadMb2 == null)
                    continue;

                var invItem = InventoryManager.Instance.GetItemById(GetItemIdFromUsable(boundItem));
                if (invItem == null || invItem.quantity <= 0)
                {
                    quickbar.ClearSlot(i);
                    ClearSlotPersistence(i);
                    Debug.Log($"[InventoryQuickbarBinder] 已清空快捷欄第 {i + 1} 格（物品不存在或數量為0）");
                }
                else
                {
                    if (boundItem is BaseUsableItem baseItem)
                        baseItem.SetCurrentCount(invItem.quantity);
                }
            }
        }

        private string GetItemIdFromUsable(Game.Inventory.IUsableItem usable)
        {
            if (usable == null)
                return null;
            if (usable is BaseUsableItem baseItem)
                return baseItem.GetItemId();
            foreach (var map in mappings)
            {
                if (map.usableComponent == usable as MonoBehaviour)
                    return map.itemId;
            }
            return null;
        }

        public Game.Inventory.IUsableItem GetUsableByItemId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            // 補血包：一律用 DontDestroy 常駐實例（Land/Menu 場景卸載後 Inspector 映射會失效）
            if (id == SmallHealItemId || id == FullHealItemId)
            {
                var persistentHeal = TryGetPersistentUsable(id);
                if (persistentHeal != null)
                    return persistentHeal;
            }

            var fromPersistent = TryGetPersistentUsable(id);
            if (fromPersistent != null)
                return fromPersistent;

            var map = mappings.Find(m => m.itemId == id);
            if (map != null && IsMappingUsableAlive(map))
            {
                var fromMap = map.usableComponent as Game.Inventory.IUsableItem;
                if (fromMap != null)
                    return fromMap;
            }

            var all = Object.FindObjectsByType<BaseUsableItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].GetItemId() == id)
                    return all[i];
            }
            return null;
        }

        static bool IsMappingUsableAlive(ItemMapping map)
        {
            if (map?.usableComponent == null)
                return false;
            var mb = map.usableComponent as MonoBehaviour;
            return mb != null;
        }

        static BaseUsableItem TryGetPersistentUsable(string id)
        {
            if (PersistentInventoryUsables.Instance == null)
                PersistentInventoryUsables.EnsureExists();
            if (PersistentInventoryUsables.Instance == null)
                return null;

            var persistents = PersistentInventoryUsables.Instance.GetComponentsInChildren<BaseUsableItem>(true);
            for (int i = 0; i < persistents.Length; i++)
            {
                if (persistents[i] != null && persistents[i].GetItemId() == id)
                    return persistents[i];
            }
            return null;
        }

        public InventorySlotView[] GetSlotViews()
        {
            return quickbar != null ? quickbar.GetSlots() : null;
        }

        /// <summary>快捷欄仍綁著已卸載場景的元件時，依記憶的 itemId 重新綁定。</summary>
        public bool TryRebindSlot(int slotIndex)
        {
            if (quickbar == null || slotIndex < 0 || slotIndex >= _slotItemIds.Length)
                return false;
            if (string.IsNullOrEmpty(_slotItemIds[slotIndex]))
                return false;

            var usable = GetUsableByItemId(_slotItemIds[slotIndex]);
            if (usable == null)
                return false;

            quickbar.BindSlot(slotIndex, usable);
            ApplyInventoryCountToUsable(_slotItemIds[slotIndex], usable);
            return true;
        }
    }
}
