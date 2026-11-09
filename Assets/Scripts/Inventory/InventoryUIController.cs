using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIController : MonoBehaviour
{
    [Header("主面板")]
    public GameObject inventoryPanel;
    public GameObject slotPrefab;

    [Header("分類面板")]
    public GameObject pageMaterials;
    public GameObject pageConsumables;
    public GameObject pageWeapons;

    public Transform slotParentMaterials;
    public Transform slotParentConsumables;
    public Transform slotParentWeapons;

    [Header("提示面板")]
    public TextMeshProUGUI tooltipNameText;
    public TextMeshProUGUI tooltipDescriptionText;
    public Image itemIconImage;

    public MaterialDatabase materialDatabase;

    private void Start()
    {
        // 先確保 InventoryPanel 是啟用的，讓自動尋找能成功
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);

        FindUIReferences();

        // 綁定完再關閉
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    private ItemType currentTab = ItemType.Material; // 追蹤當前分頁

    private void OnEnable()
    {
        FindUIReferences();
        // 背包面板開啟即暫停
        if (inventoryPanel != null && inventoryPanel.GetComponent<PauseOnEnable>() == null)
            inventoryPanel.AddComponent<PauseOnEnable>();
            
        InventoryManager.OnInventoryChanged += RefreshCurrentTab; // 訂閱事件
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
             RefreshCurrentTab();
        }
    }

    private void OnDisable()
    {
        InventoryManager.OnInventoryChanged -= RefreshCurrentTab; // 取消訂閱
    }

    private void Update()
    {
        // ESC 關閉背包（只關背包，不處理其他UI）
        if (inventoryPanel != null &&
            inventoryPanel.activeSelf &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInventory();
        }
    }

    // 當背包內容改變時，刷新當前分頁
    private void RefreshCurrentTab()
    {
        if (inventoryPanel == null || !inventoryPanel.activeSelf) return;

        switch (currentTab)
        {
            case ItemType.Material: ShowMaterials(); break;
            case ItemType.Consumable: ShowConsumables(); break;
            case ItemType.Weapon: ShowWeapons(); break;
        }
    }

    private void FindUIReferences()
    {
        // 主面板
        if (inventoryPanel == null)
            inventoryPanel = GameObject.Find("InventoryPanel");

        if (inventoryPanel != null)
        {
            // 分頁面板
            if (pageMaterials == null)
                pageMaterials = inventoryPanel.transform.Find("All/Page_Materials")?.gameObject;
            if (pageConsumables == null)
                pageConsumables = inventoryPanel.transform.Find("All/Page_Consumables")?.gameObject;
            if (pageWeapons == null)
                pageWeapons = inventoryPanel.transform.Find("All/Page_Weapons")?.gameObject;

            // 分頁父物件
            if (slotParentMaterials == null)
                slotParentMaterials = inventoryPanel.transform.Find("LeftPanel/ItemSlot");
            if (slotParentConsumables == null)
                slotParentConsumables = inventoryPanel.transform.Find("LeftPanel/ItemSlot");
            if (slotParentWeapons == null)
                slotParentWeapons = inventoryPanel.transform.Find("LeftPanel/ItemSlot");

            // 右側提示
            if (tooltipNameText == null)
                tooltipNameText = inventoryPanel.transform.Find("RightPanel/ItemNameText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (tooltipDescriptionText == null)
                tooltipDescriptionText = inventoryPanel.transform.Find("RightPanel/ItemDescriptionText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (itemIconImage == null)
                itemIconImage = inventoryPanel.transform.Find("RightPanel/ItemIconImage")?.GetComponent<UnityEngine.UI.Image>();
        }
        else
        {
            Debug.LogWarning("找不到 InventoryPanel，請確認 Canvas 結構正確！");
        }
    }

    // ... (Update -> B Key 省略) ...

    public void ToggleInventory()
    {
        FindUIReferences(); // 每次開關背包時檢查UI引用

        if (inventoryPanel != null)
        {
            bool shouldOpen = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(shouldOpen);
            
            if (shouldOpen)
            {
                // 開啟時預設顯示上次的分頁，或預設素材頁
                RefreshCurrentTab();
            }
        }
        else
        {
            Debug.LogWarning("找不到背包面板！請確認場景中有名為 'InventoryPanel' 的物件");
        }
    }

    public void ShowMaterials()
    {
        currentTab = ItemType.Material; // 更新狀態
        FindUIReferences(); 
        
        if (pageMaterials != null) pageMaterials.SetActive(true);
        if (pageConsumables != null) pageConsumables.SetActive(false);
        if (pageWeapons != null) pageWeapons.SetActive(false);
        
        if (slotParentMaterials != null)
            RefreshCategory(ItemType.Material, slotParentMaterials);
        else
            Debug.LogWarning("找不到素材欄位父物件！");
    }

    public void ShowConsumables()
    {
        currentTab = ItemType.Consumable; // 更新狀態
        FindUIReferences(); 
        
        if (pageMaterials != null) pageMaterials.SetActive(false);
        if (pageConsumables != null) pageConsumables.SetActive(true);
        if (pageWeapons != null) pageWeapons.SetActive(false);
        
        if (slotParentConsumables != null)
            RefreshCategory(ItemType.Consumable, slotParentConsumables);
        else
            Debug.LogWarning("找不到消耗品欄位父物件！");
    }

    public void ShowWeapons()
    {
        currentTab = ItemType.Weapon; // 更新狀態
        FindUIReferences(); 
        
        if (pageMaterials != null) pageMaterials.SetActive(false);
        if (pageConsumables != null) pageConsumables.SetActive(false);
        if (pageWeapons != null) pageWeapons.SetActive(true);
        
        if (slotParentWeapons != null)
            RefreshCategory(ItemType.Weapon, slotParentWeapons);
        else
            Debug.LogWarning("找不到武器欄位父物件！");
    }

    private void RefreshCategory(ItemType type, Transform slotParent)
    {
        if (slotParent == null || slotPrefab == null)
        {
            Debug.LogWarning($"無法重整{type}欄位：缺少必要元件");
            return;
        }

        // 清除現有的物品格
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        // 檢查 InventoryManager 是否存在
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("找不到 InventoryManager！");
            return;
        }

        foreach (var item in InventoryManager.Instance.items)
        {
            if (item.itemType != type) continue;

            // 取得素材資料
            MaterialData matData = null;
            if (type == ItemType.Material && materialDatabase != null)
                matData = materialDatabase.GetMaterial(item.itemId);

            GameObject slot = Instantiate(slotPrefab, slotParent);
            if (slot == null)
            {
                Debug.LogWarning("無法創建物品格！");
                continue;
            }

            var iconTransform = slot.transform.Find("Icon");
            var quantityTransform = slot.transform.Find("QuantityText");
            
            if (iconTransform == null || quantityTransform == null)
            {
                Debug.LogWarning("物品格預製體缺少必要元件！");
                Destroy(slot);
                continue;
            }

            Image iconImage = iconTransform.GetComponent<Image>();
            TextMeshProUGUI quantityText = quantityTransform.GetComponent<TextMeshProUGUI>();

            if (iconImage != null && quantityText != null)
            {
                // 優先用資料表資料
                iconImage.sprite = matData != null ? matData.icon : item.icon;
                quantityText.text = item.quantity.ToString();

                var hover = slot.AddComponent<InventorySlotHover>();
                if (hover != null)
                {
                    hover.Setup(
                        matData != null ? matData.materialName : item.itemName,
                        matData != null ? matData.description : item.description,
                        matData != null ? matData.icon : item.icon,
                        this
                    );
                }

                if (item.itemType != ItemType.Material)
                {
                    var drag = slot.AddComponent<DraggableItem>();
                    if (drag != null)
                    {
                        drag.itemId = item.itemId;
                        drag.icon = item.icon;
                        drag.itemType = item.itemType;
                    }
                }

                bool isEquipped = item.itemId == PlayerDataManager.EquippedWeapon1
                               || item.itemId == PlayerDataManager.EquippedWeapon2
                               || item.itemId == PlayerDataManager.EquippedItem;

                iconImage.color = isEquipped ? new Color(1, 1, 1, 0.4f) : Color.white;
            }
            else
            {
                Debug.LogWarning("物品格預製體元件獲取失敗！");
                Destroy(slot);
            }
        }
    }

    public void CloseInventory()
    {
        FindUIReferences(); // 確保UI引用存在
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    public void ShowTooltip(string name, string description, Sprite icon = null)
    {
        FindUIReferences(); // 確保UI引用存在
        
        if (tooltipNameText != null)
            tooltipNameText.text = name;
            
        if (tooltipDescriptionText != null)
            tooltipDescriptionText.text = description;
            
        if (itemIconImage != null)
        {
            itemIconImage.sprite = icon;
            itemIconImage.color = icon != null ? Color.white : new Color(1, 1, 1, 0);
        }
    }

    public void HideTooltip()
    {
        FindUIReferences(); // 確保UI引用存在
        
        if (tooltipNameText != null)
            tooltipNameText.text = "";
            
        if (tooltipDescriptionText != null)
            tooltipDescriptionText.text = "";
            
        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.color = new Color(1, 1, 1, 0);
        }
    }
}
