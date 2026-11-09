using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ShopCarouselUIController : MonoBehaviour
{
    [Header("商店資料")]
    public ShopInventory shopInventory;
    public ShopItemType currentType = ShopItemType.Weapon;

    [Header("UI 元件")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    public TextMeshProUGUI itemDescText;
    public TextMeshProUGUI coinText;

    [Tooltip("可選：金幣即時同步＋扣款 -X／遞減（未指定時仍用 coinText 並訂閱 CoinsChanged）")]
    [SerializeField] private CoinSpendFeedbackView coinSpendFeedback;

    public Button buyButton;
    public Button leftButton;
    public Button rightButton;

    private ShopItemData[] currentItemList;
    private int currentIndex = 0;

    private void OnEnable()
    {
        // 確保本面板開啟時會暫停
        if (GetComponent<PauseOnEnable>() == null)
            gameObject.AddComponent<PauseOnEnable>();
        if (coinSpendFeedback == null)
            PlayerDataManager.CoinsChanged += OnCarouselCoinsChanged;
        if (coinSpendFeedback != null && coinText != null)
            coinSpendFeedback.SetExternalBalanceMirror(coinText);
        UpdateCoinText();
        coinSpendFeedback?.RefreshBalanceText();
        LoadItemList();
        ShowItem();

        leftButton.onClick.AddListener(PrevItem);
        rightButton.onClick.AddListener(NextItem);
        buyButton.onClick.AddListener(BuyCurrentItem);
    }

    private void OnDisable()
    {
        coinSpendFeedback?.SetExternalBalanceMirror(null);
        PlayerDataManager.CoinsChanged -= OnCarouselCoinsChanged;
        leftButton.onClick.RemoveAllListeners();
        rightButton.onClick.RemoveAllListeners();
        buyButton.onClick.RemoveAllListeners();
    }

    void OnCarouselCoinsChanged(int _) => UpdateCoinText();

    public void SetTab(ShopItemType type)
    {
        currentType = type;
        currentIndex = 0;
        LoadItemList();
        ShowItem();
    }

    void LoadItemList()
    {
        switch (currentType)
        {
            case ShopItemType.Weapon:
                currentItemList = shopInventory.weapons;
                break;
            case ShopItemType.Item:
                currentItemList = shopInventory.items;
                break;
            default:
                currentItemList = new ShopItemData[0];
                break;
        }
    }

    void ShowItem()
    {
        if (currentItemList.Length == 0) return;

        ShopItemData data = currentItemList[currentIndex];

        itemIcon.DOFade(0, 0f);
        itemIcon.sprite = data.icon;
        itemIcon.DOFade(1, 0.2f);

        itemNameText.text = data.itemName;
        itemPriceText.text = "價格:" + data.price + " 金幣";
        itemDescText.text = data.description;
    }

    public void PrevItem()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = currentItemList.Length - 1;
        ShowItem();
    }

    public void NextItem()
    {
        currentIndex++;
        if (currentIndex >= currentItemList.Length) currentIndex = 0;
        ShowItem();
    }

    public void BuyCurrentItem()
    {
        var item = currentItemList[currentIndex];
        if (PlayerDataManager.Coins >= item.price)
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("[ShopCarouselUIController] InventoryManager 不存在，無法購買");
                return;
            }

            // 爆彈上限 8
            if (item.itemId == "Bomb")
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
                CoinManager.Instance.SpendCoins(item.price);
            else
                PlayerDataManager.Coins -= item.price; // 後備：以防場景未掛載 CoinManager
            InventoryManager.Instance.AddItem(item.itemId, 1);
            if (item.itemId == "ArmorPack")
            {
                var armor = GameObject.FindObjectOfType<Game.Inventory.Items.ArmorBuffItem>();
                if (armor != null) armor.TryUse();
            }
            if (coinSpendFeedback != null)
                coinSpendFeedback.RunSpendPresentation(coinsBefore, item.price, this);
            else
                UpdateCoinText();

            if (Game.Inventory.UI.InventoryQuickbarBinder.Instance != null)
                Game.Inventory.UI.InventoryQuickbarBinder.Instance.Refresh();
        }
        else
        {
            Debug.Log("金幣不足");
        }
    }

    private void UpdateCoinText()
    {
        if (coinSpendFeedback != null)
            coinSpendFeedback.RefreshBalanceText();
        else if (coinText != null)
            coinText.text = " " + PlayerDataManager.Coins;
    }
}
