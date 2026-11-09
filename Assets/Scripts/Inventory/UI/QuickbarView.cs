using UnityEngine;

namespace Game.Inventory.UI
{
    /// <summary>
    /// 4 格快捷欄：滾輪切換選中，E／F 使用當前格（不再綁定數字鍵 1-4，避免與衝刺格 UI 混淆）。
    /// </summary>
    public class QuickbarView : MonoBehaviour
    {
        [SerializeField] private InventorySlotView[] slots = new InventorySlotView[4];
        [SerializeField] private int selectedIndex = 0;

        [Header("Selection Visuals (Optional)")]
        [SerializeField] private GameObject[] selectionHighlights; // 與 slots 對應，高亮當前選中

        private void Start()
        {
            ApplySelectionVisuals();
        }

        private void Update()
        {
            // 滾輪切換當前選中
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01f) SelectPrev();
            else if (scroll < -0.01f) SelectNext();

            // 使用當前選中：需求改為 E（保留 F 相容）；釣魚 QTE 期間不搶 E
            if (FishingUIController.Instance != null && FishingUIController.Instance.BlocksGameplayInput)
            {
                if (Input.GetKeyDown(KeyCode.F)) UseSelected();
            }
            else if (FishingManager.Instance != null && FishingManager.Instance.IsFishing)
            {
                if (Input.GetKeyDown(KeyCode.F)) UseSelected();
            }
            else if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
            {
                UseSelected();
            }
        }

        public void BindSlot(int index, Game.Inventory.IUsableItem item)
        {
            if (!IsValidIndex(index))
            {
                Debug.LogWarning($"[QuickbarView] 無效的索引: {index}");
                return;
            }
            if (slots[index] == null)
            {
                Debug.LogError($"[QuickbarView] Slot {index} 為 null，請檢查 Inspector 設置");
                return;
            }
            if (item == null)
            {
                Debug.LogWarning($"[QuickbarView] 嘗試綁定 null 物品到 Slot {index}");
                return;
            }
            Debug.Log($"[QuickbarView] 綁定物品到 Slot {index}，Icon: {(item.Icon != null ? item.Icon.name : "null")}");
            slots[index].Bind(item);
        }

        public void ClearSlot(int index)
        {
            if (!IsValidIndex(index)) return;
            slots[index]?.Unbind();
        }

        public InventorySlotView[] GetSlots()
        {
            return slots;
        }

        public void SelectNext()
        {
            selectedIndex = (selectedIndex + 1) % slots.Length;
            ApplySelectionVisuals();
        }

        public void SelectPrev()
        {
            selectedIndex = (selectedIndex - 1 + slots.Length) % slots.Length;
            ApplySelectionVisuals();
        }

        public void UseSelected()
        {
            UseSlot(selectedIndex);
        }

        public void UseSlot(int index)
        {
            if (!IsValidIndex(index)) return;
            var slot = slots[index];
            if (slot == null) return;
            // 直接模擬按鈕點擊：讓 SlotView 統一處理使用與冷卻
            var button = slot.GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
            selectedIndex = index;
            ApplySelectionVisuals();
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && slots != null && index < slots.Length;
        }

        private void ApplySelectionVisuals()
        {
            if (selectionHighlights == null) return;
            for (int i = 0; i < selectionHighlights.Length; i++)
            {
                if (selectionHighlights[i] != null)
                {
                    selectionHighlights[i].SetActive(i == selectedIndex);
                }
            }
        }
    }
}


