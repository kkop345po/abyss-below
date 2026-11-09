using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Inventory.UI
{
    /// <summary>
    /// 背包清單中的單列 UI。
    /// </summary>
    public class InventoryPanelViewItem : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Button selectButton;

        private InventoryItem bound;
        private InventoryPanelView owner;

        public void Setup(InventoryItem item, InventoryPanelView panel)
        {
            bound = item;
            owner = panel;
            if (icon != null)
            {
                if (item.icon != null)
                {
                    icon.enabled = true;
                    icon.sprite = item.icon;
                    icon.color = Color.white;
                }
                else
                {
                    // 沒有圖示時隱藏，避免顯示成一塊白色
                    icon.enabled = false;
                }
            }
            if (nameText != null) nameText.text = item.itemName;
            if (countText != null) countText.text = "x" + item.quantity;

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => owner.ShowDetail(bound));
            }
        }
    }
}


