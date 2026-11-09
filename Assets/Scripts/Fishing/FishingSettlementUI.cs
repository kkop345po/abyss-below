using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 釣魚結算畫面（對齊賭博 JudgePanel：標題、說明、圖示、確認）。
/// </summary>
public class FishingSettlementUI : MonoBehaviour
{
    [Header("結算面板")]
    [SerializeField] private GameObject settlementPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemAmountText;
    [SerializeField] private Button confirmButton;

    [Header("預設文案")]
    [SerializeField] private string failTitle = "沒釣到";
    [SerializeField] private string failDetail = "魚脫鉤了，再試一次吧。";
    [SerializeField] private string fishTitle = "釣到了！";
    [SerializeField] private string trashTitle = "釣到垃圾";
    [SerializeField] private string trashSellLine = "已兌換 {0} 金幣";

    [Header("發獎")]
    [SerializeField] private MonsterDropTable monsterDropTable;
    [SerializeField] private int trashSellPrice = 1;
    [SerializeField] private bool grantTrashCoinsImmediately = true;

    bool _confirmRequested;

    void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        Hide();
    }

    void OnConfirmClicked()
    {
        _confirmRequested = true;
        AudioManager.GetInstance()?.PlayButtonSfx();
    }

    public void Hide()
    {
        if (settlementPanel != null)
            settlementPanel.SetActive(false);
    }

    public MonsterDropTable MonsterDropTable => monsterDropTable;

    public IEnumerator ShowFailureSettlement()
    {
        var result = new FishingSessionResult
        {
            QteSuccess = false,
            GotFish = false,
            Title = failTitle,
            Detail = failDetail
        };

        yield return ShowSettlementPresentation(result, grantRewards: false);
    }

    public IEnumerator ShowFishSettlement(MonsterDropTable.DropEntry drop)
    {
        var result = BuildFishResult(drop);
        yield return ShowSettlementPresentation(result, grantRewards: true);
    }

    public IEnumerator ShowTrashSettlement(TrashData.TrashEntry trash)
    {
        var result = BuildTrashResult(trash);
        yield return ShowSettlementPresentation(result, grantRewards: true);
    }

    FishingSessionResult BuildFishResult(MonsterDropTable.DropEntry drop)
    {
        var r = new FishingSessionResult
        {
            QteSuccess = true,
            GotFish = true,
            Title = fishTitle,
            ItemAmount = 1
        };

        if (drop == null)
        {
            r.Detail = "但沒有抽到掉落（請檢查 MonsterDropTable）。";
            return r;
        }

        r.ItemId = MonsterDropTable.ResolveItemId(drop);
        r.ItemAmount = Mathf.Max(1, drop.amount);
        r.ItemDisplayName = r.ItemId;

        if (InventoryManager.Instance != null)
        {
            var data = InventoryManager.Instance.ResolveItemData(r.ItemId);
            if (data != null)
            {
                r.ItemDisplayName = data.itemName;
                r.ItemIcon = data.icon;
            }
        }

        r.Detail = $"獲得 {r.ItemDisplayName} x{r.ItemAmount}";
        return r;
    }

    FishingSessionResult BuildTrashResult(TrashData.TrashEntry trash)
    {
        var r = new FishingSessionResult
        {
            QteSuccess = true,
            GotFish = false,
            Title = trashTitle,
            ItemAmount = 1,
            CoinsGranted = grantTrashCoinsImmediately ? trashSellPrice : 0
        };

        if (trash == null)
        {
            r.Detail = "（垃圾資料為空）";
            return r;
        }

        r.ItemDisplayName = trash.displayName;
        r.ItemIcon = trash.icon;
        r.ItemId = string.IsNullOrWhiteSpace(trash.inventoryItemId)
            ? null
            : trash.inventoryItemId.Trim();
        r.Detail = trash.displayName;
        if (r.CoinsGranted > 0)
            r.Detail += "\n" + string.Format(trashSellLine, r.CoinsGranted);

        return r;
    }

    IEnumerator ShowSettlementPresentation(FishingSessionResult result, bool grantRewards)
    {
        if (grantRewards)
            ApplyRewards(result);

        ApplyToWidgets(result);

        if (settlementPanel != null)
            settlementPanel.SetActive(true);

        _confirmRequested = false;
        while (!_confirmRequested
               && !Input.GetKeyDown(KeyCode.E)
               && !Input.GetKeyDown(KeyCode.Escape))
        {
            yield return null;
        }

        Hide();
    }

    void ApplyRewards(FishingSessionResult result)
    {
        if (!result.QteSuccess)
            return;

        if (result.GotFish)
        {
            if (InventoryManager.Instance != null && !string.IsNullOrEmpty(result.ItemId))
                InventoryManager.Instance.AddItem(result.ItemId, result.ItemAmount);

            if (PickupFeedbackManager.Instance != null && !string.IsNullOrEmpty(result.ItemDisplayName))
                PickupFeedbackManager.Instance.ShowPickup(result.ItemDisplayName, result.ItemAmount);
        }
        else
        {
            if (grantTrashCoinsImmediately && result.CoinsGranted > 0 && CoinManager.Instance != null)
                CoinManager.Instance.AddCoins(result.CoinsGranted);

            if (InventoryManager.Instance != null && !string.IsNullOrEmpty(result.ItemId))
                InventoryManager.Instance.AddItem(result.ItemId, 1);
        }
    }

    void ApplyToWidgets(FishingSessionResult result)
    {
        if (titleText != null)
            titleText.text = result.Title ?? "";

        if (detailText != null)
            detailText.text = result.Detail ?? "";

        bool showItem = result.QteSuccess && !string.IsNullOrEmpty(result.ItemDisplayName);
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(showItem);
            itemIcon.enabled = result.ItemIcon != null;
            itemIcon.sprite = result.ItemIcon;
        }

        if (itemNameText != null)
        {
            itemNameText.gameObject.SetActive(showItem);
            itemNameText.text = showItem ? result.ItemDisplayName : "";
        }

        if (itemAmountText != null)
        {
            bool showAmt = showItem && result.GotFish && result.ItemAmount > 1;
            itemAmountText.gameObject.SetActive(showAmt);
            itemAmountText.text = showAmt ? $"x{result.ItemAmount}" : "";
        }
    }
}
