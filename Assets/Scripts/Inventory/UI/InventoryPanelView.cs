using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Inventory.Items;

namespace Game.Inventory.UI
{
    /// <summary>
    /// 簡易背包面板：
    /// - 左側清單：從 InventoryManager 讀取並生成項目
    /// - 右側詳情：顯示圖示/名稱/數量/描述，提供「加入快捷欄」與「使用」
    /// </summary>
    public class InventoryPanelView : MonoBehaviour
    {
        [Header("List")]
        [SerializeField] private Transform listParent; // 物品列表的 Content
        [SerializeField] private GameObject listItemPrefab; // 單列 prefab
        [SerializeField] private MaterialDatabase materialDatabase; // 用於補齊素材圖示/名稱/描述
        [SerializeField] private ShopInventory shopInventory; // 用於補齊道具圖示/名稱/描述

        [Header("Detail")]
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailName;
        [SerializeField] private TextMeshProUGUI detailCount;
        [SerializeField] private TextMeshProUGUI detailDesc;
        [SerializeField] private Button addToQuickbarButton;
        [SerializeField] private Button useButton;

        private InventoryItem selectedItem;

        private void OnEnable()
        {
            RefreshList();
            BindButtons();
            ClearDetail();
            // 開啟背包時自動清理快捷欄（移除數量為0的物品）
            InventoryQuickbarBinder.Instance?.Refresh();
        }

        private void BindButtons()
        {
            if (addToQuickbarButton != null)
            {
                addToQuickbarButton.onClick.RemoveAllListeners();
                addToQuickbarButton.onClick.AddListener(AddSelectedToQuickbar);
            }
            if (useButton != null)
            {
                useButton.onClick.RemoveAllListeners();
                useButton.onClick.AddListener(UseSelected);
            }
        }

        public void RefreshList()
        {
            if (listParent == null || listItemPrefab == null || InventoryManager.Instance == null) return;
            foreach (Transform child in listParent) Destroy(child.gameObject);
            // 顯示所有類型（Material/Consumable/Weapon），但按鈕會依類型禁用
            foreach (var inv in InventoryManager.Instance.items)
            {
                MergeMaterialMeta(inv);
                MergeShopItemMeta(inv); // 補齊道具資訊
                var go = Instantiate(listItemPrefab, listParent);
                var ui = go.GetComponent<InventoryPanelViewItem>();
                if (ui != null) ui.Setup(inv, this);
            }
        }

        public void ShowDetail(InventoryItem item)
        {
            selectedItem = item;
            if (item == null)
            {
                ClearDetail();
                return;
            }
            // 顯示前先補齊素材/道具資訊（確保名稱/圖示正確）
            MergeMaterialMeta(item);
            MergeShopItemMeta(item);
            
            if (detailIcon != null)
            {
                if (item.icon != null)
                {
                    detailIcon.enabled = true;
                    detailIcon.sprite = item.icon;
                    detailIcon.color = Color.white;
                }
                else
                {
                    detailIcon.enabled = false;
                }
            }
            if (detailName != null) detailName.text = item.itemName;
            if (detailCount != null) detailCount.text = "x" + item.quantity;
            if (detailDesc != null) detailDesc.text = item.description;

            // 根據 itemType 禁用/啟用按鈕：只有 Consumable 可用
            bool isConsumable = item.itemType == ItemType.Consumable;
            if (addToQuickbarButton != null) addToQuickbarButton.interactable = isConsumable;
            if (useButton != null) useButton.interactable = isConsumable;
        }

        private void ClearDetail()
        {
            if (detailIcon != null) detailIcon.sprite = null;
            if (detailName != null) detailName.text = "";
            if (detailCount != null) detailCount.text = "";
            if (detailDesc != null) detailDesc.text = "";
            selectedItem = null;
            // 清空時也禁用按鈕
            if (addToQuickbarButton != null) addToQuickbarButton.interactable = false;
            if (useButton != null) useButton.interactable = false;
        }

        private void AddSelectedToQuickbar()
        {
            if (selectedItem == null)
            {
                Debug.LogWarning("[InventoryPanelView] 沒有選中物品");
                return;
            }
            // 只允許 Consumable 加入快捷欄
            if (selectedItem.itemType != ItemType.Consumable)
            {
                Debug.LogWarning($"[InventoryPanelView] 只有 Consumable 可以加入快捷欄，當前類型：{selectedItem.itemType}");
                return;
            }
            if (InventoryQuickbarBinder.Instance == null)
            {
                Debug.LogError("[InventoryPanelView] InventoryQuickbarBinder.Instance 為 null");
                return;
            }
            if (QuickbarTarget == null)
            {
                Debug.LogError("[InventoryPanelView] QuickbarTarget 未設置");
                return;
            }

            var usable = InventoryQuickbarBinder.Instance.GetUsableByItemId(selectedItem.itemId);
            if (usable == null)
            {
                Debug.LogWarning($"[InventoryPanelView] 找不到對應的 IUsableItem: {selectedItem.itemId}，請檢查 InventoryQuickbarBinder 的 mappings");
                return;
            }

            // 先找第一個空位，若無空位則替換第一個
            int index = FindFirstEmptyQuickbarSlot();
            if (index < 0)
            {
                Debug.Log("[InventoryPanelView] 快捷欄已滿，將替換第 1 格");
                index = 0; // 若無空位，替換第 0 格
            }
            
            Debug.Log($"[InventoryPanelView] 準備將 {selectedItem.itemName} (ID: {selectedItem.itemId}) 綁定到快捷欄第 {index + 1} 格");
            QuickbarTarget.BindSlot(index, usable);
            InventoryQuickbarBinder.Instance.RegisterSlotAssignment(index, selectedItem.itemId);

            // 更新快捷欄數量顯示
            if (usable is BaseUsableItem baseItem)
            {
                baseItem.SetCurrentCount(selectedItem.quantity);
                Debug.Log($"[InventoryPanelView] 已更新數量：{selectedItem.quantity}");
            }
            
            Debug.Log($"[InventoryPanelView] ✓ 已將 {selectedItem.itemName} 加入快捷欄第 {index + 1} 格");
        }

        private int FindFirstEmptyQuickbarSlot()
        {
            if (QuickbarTarget == null) return -1;
            var slots = QuickbarTarget.GetSlots();
            if (slots == null) return 0;
            
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) return i;
                // 使用 GetBoundItem() 檢查槽位是否為空（更可靠）
                var boundItem = slots[i].GetBoundItem();
                if (boundItem == null) return i;
            }
            return -1; // 沒有空位
        }

        private void UseSelected()
        {
            if (selectedItem == null) return;
            // 只允許 Consumable 使用
            if (selectedItem.itemType != ItemType.Consumable) return;
            var usable = ResolveUsableForItemId(selectedItem.itemId);
            if (usable == null)
            {
                Debug.LogWarning($"[InventoryPanelView] 找不到對應的 IUsableItem: {selectedItem.itemId}，請檢查 InventoryQuickbarBinder 的 mappings，或場景內是否有對應的 BaseUsableItem（如 SquidBallUsableItem）。");
                return;
            }
            usable.TryUse();
            ShowDetail(selectedItem); // 更新數量顯示
            RefreshList();
            // 使用後刷新快捷欄（清理數量為0的物品）
            InventoryQuickbarBinder.Instance?.Refresh();
        }

        [Header("External")]
        [SerializeField] private QuickbarView QuickbarTarget;

        private void MergeMaterialMeta(InventoryItem item)
        {
            if (item == null) return;
            if (item.itemType != ItemType.Material && item.itemType != ItemType.Quest) return;

            if (materialDatabase != null)
            {
                var matData = materialDatabase.GetMaterial(item.itemId);
                if (matData != null)
                {
                    if (matData.icon != null) item.icon = matData.icon;
                    if (!string.IsNullOrEmpty(matData.materialName)) item.itemName = matData.materialName;
                    if (!string.IsNullOrEmpty(matData.description)) item.description = matData.description;
                    return;
                }
            }

            // 任務道具／ItemDatabase：與 InventoryManager.ResolveItemData 一致
            var itemData = InventoryManager.Instance != null ? InventoryManager.Instance.ResolveItemData(item.itemId) : null;
            if (itemData != null)
            {
                if (itemData.icon != null) item.icon = itemData.icon;
                if (!string.IsNullOrEmpty(itemData.itemName)) item.itemName = itemData.itemName;
                if (!string.IsNullOrEmpty(itemData.description)) item.description = itemData.description;
                return;
            }

            if (materialDatabase == null && InventoryManager.Instance == null)
                Debug.LogWarning("[InventoryPanelView] MaterialDatabase 與 InventoryManager 皆未就緒，無法補齊素材顯示");
        }

        private void MergeShopItemMeta(InventoryItem item)
        {
            if (item == null) return;
            if (item.itemType != ItemType.Consumable && item.itemType != ItemType.Weapon) return;
            // 優先從 ShopInventory 補齊（若有），否則退回 ItemDatabase（InventoryManager.ResolveItemData）

            ShopItemData found = null;
            if (shopInventory != null && shopInventory.items != null)
            {
                foreach (var shopItem in shopInventory.items)
                {
                    if (shopItem != null && shopItem.itemId == item.itemId)
                    {
                        found = shopItem;
                        break;
                    }
                }
            }
            // 若沒找到，從 weapons 陣列找（武器）
            if (found == null && shopInventory != null && shopInventory.weapons != null)
            {
                foreach (var shopItem in shopInventory.weapons)
                {
                    if (shopItem != null && shopItem.itemId == item.itemId)
                    {
                        found = shopItem;
                        break;
                    }
                }
            }

            if (found == null)
            {
                // ShopInventory 沒有就回退 ItemDatabase（避免花枝丸等非商店道具無法顯示資訊/一直噴 warning）
                var itemData = InventoryManager.Instance != null ? InventoryManager.Instance.ResolveItemData(item.itemId) : null;
                if (itemData != null)
                {
                    if (itemData.icon != null) item.icon = itemData.icon;
                    if (!string.IsNullOrEmpty(itemData.itemName)) item.itemName = itemData.itemName;
                    if (!string.IsNullOrEmpty(itemData.description)) item.description = itemData.description;
                    return;
                }

                // 仍找不到才警告（代表 ItemDatabase 也沒有）
                Debug.LogWarning($"[InventoryPanelView] 找不到商店資料/ItemDatabase 資料: {item.itemId}");
                return;
            }

            // 強制更新：從 ShopItemData 覆蓋
            if (found.icon != null) item.icon = found.icon;
            if (!string.IsNullOrEmpty(found.itemName)) item.itemName = found.itemName;
            if (!string.IsNullOrEmpty(found.description)) item.description = found.description;
        }

        private Game.Inventory.IUsableItem ResolveUsableForItemId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            // 1) 先走 Inspector mappings（原本設計）
            var mapped = InventoryQuickbarBinder.Instance != null ? InventoryQuickbarBinder.Instance.GetUsableByItemId(itemId) : null;
            if (mapped != null) return mapped;

            // 2) 退回：從場景內找 BaseUsableItem（如 SquidBallUsableItem）
            // 註：InventoryPanelView 本身就會按「Consumable 才能使用」，因此只需找可用道具實例。
            var all = Object.FindObjectsByType<BaseUsableItem>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].GetItemId() == itemId)
                    return all[i];
            }
            return null;
        }
    }
}


