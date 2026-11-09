using UnityEngine;
using TMPro;

public class ShopUIController : MonoBehaviour
{
    [Header("商品清單")]
    public ShopInventory inventoryData;
    public Transform itemContentParent;
    public GameObject shopSlotPrefab;

    [Header("金幣顯示")]
    public TextMeshProUGUI coinText;

    [Header("金幣即時同步／扣款演出（可選，與賭桌相同元件）")]
    [Tooltip("若指定：由元件訂閱金幣變化並可播 -X＋遞減；未指定則仍用上方 coinText 並訂閱 CoinsChanged")]
    [SerializeField] private CoinSpendFeedbackView coinSpendFeedback;

    [Header("面板")]
    public GameObject itemPanel;

    private void Start()
    {
        FindUIReferences();
    }

    private void OnEnable()
    {
        if (!gameObject.activeInHierarchy) return;

        FindUIReferences();
        // 確保本面板開啟時會暫停
        if (GetComponent<PauseOnEnable>() == null)
            gameObject.AddComponent<PauseOnEnable>();
        if (coinSpendFeedback == null)
            PlayerDataManager.CoinsChanged += OnShopCoinsChanged;
        if (coinSpendFeedback != null && coinText != null)
            coinSpendFeedback.SetExternalBalanceMirror(coinText);
        RefreshCoinText();
        coinSpendFeedback?.RefreshBalanceText();
        LoadItems();
    }

    private void OnDisable()
    {
        coinSpendFeedback?.SetExternalBalanceMirror(null);
        PlayerDataManager.CoinsChanged -= OnShopCoinsChanged;
    }

    void OnShopCoinsChanged(int _) => RefreshCoinText();

    private void Update()
    {
        // ESC 關閉商店 UI
        if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            gameObject.SetActive(false);
        }
    }

    private void FindUIReferences()
    {
        // 尋找商品列表父物件
        if (itemContentParent == null)
            itemContentParent = transform.Find("Content");

        // 尋找金幣文字
        if (coinText == null)
            coinText = transform.Find("CoinDisplay/CoinText")?.GetComponent<TextMeshProUGUI>();

        // 尋找商品面板
        if (itemPanel == null)
            itemPanel = transform.Find("ItemPanel")?.gameObject;

        // 如果還找不到，嘗試全局搜索
        if (itemContentParent == null)
            itemContentParent = GameObject.Find("Content")?.transform;
        if (coinText == null)
            coinText = GameObject.Find("CoinText")?.GetComponent<TextMeshProUGUI>();
        if (itemPanel == null)
            itemPanel = GameObject.Find("ItemPanel");
        if (coinSpendFeedback == null)
            coinSpendFeedback = GetComponentInChildren<CoinSpendFeedbackView>(true);
    }

    public void LoadItems()
    {
        if (inventoryData == null || itemContentParent == null || shopSlotPrefab == null)
        {
            Debug.LogError("ShopUIController 缺少設定元件");
            return;
        }

        foreach (Transform child in itemContentParent)
            Destroy(child.gameObject);

        if (inventoryData.items != null)
        {
            foreach (var item in inventoryData.items)
            {
                var slot = Instantiate(shopSlotPrefab, itemContentParent);
                var ui = slot.GetComponent<ShopSlotUI>();
                if (ui != null)
                    ui.Setup(item, this);
                else
                    Debug.LogError("ShopSlotPrefab 沒有掛載 ShopSlotUI 元件！");
            }
        }
    }

    public void AttemptBuy(ShopItemData data)
    {
        int currentCoins = PlayerDataManager.Coins;

        if (currentCoins >= data.price)
        {
            // 規則：爆彈單次持有上限 8
            if (data.itemId == "Bomb")
            {
                var bomb = InventoryManager.Instance.GetItemById("Bomb");
                if (bomb != null && bomb.quantity >= 8)
                {
                    Debug.Log("爆彈已達上限 8，無法購買");
                    return;
                }
            }

            if (coinSpendFeedback != null)
                coinSpendFeedback.SetAnimLock(true);
            int coinsBefore = PlayerDataManager.Coins;
            // 統一透過 CoinManager 扣款，確保HUD金幣即時更新
            if (CoinManager.Instance != null)
                CoinManager.Instance.SpendCoins(data.price);
            else
                PlayerDataManager.Coins -= data.price; // 後備：以防場景未掛載 CoinManager
            InventoryManager.Instance.AddItem(data, 1);
            // 規則：護甲購買即使用（不進背包或進背包同時觸發）
            if (data.itemId == "ArmorPack")
            {
                var armor = GameObject.FindObjectOfType<Game.Inventory.Items.ArmorBuffItem>();
                if (armor != null) armor.TryUse();
            }
            if (coinSpendFeedback != null)
                coinSpendFeedback.RunSpendPresentation(coinsBefore, data.price, this);
            else
                RefreshCoinText();

            // 購買後刷新快捷欄（如存在）
            if (Game.Inventory.UI.InventoryQuickbarBinder.Instance != null)
                Game.Inventory.UI.InventoryQuickbarBinder.Instance.Refresh();
        }
        else
        {
            Debug.Log("餘額不足");
        }
    }

    public void RefreshCoinText()
    {
        if (coinText != null)
            coinText.text = "" + PlayerDataManager.Coins;
    }
}