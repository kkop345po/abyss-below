using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>已改由 <see cref="FishingSettlementUI"/> 負責結算；保留僅供舊場景相容。</summary>
[System.Obsolete("請改用 FishingSettlementUI")]
public class FishingReward : MonoBehaviour
{
    [Header("UI 參考")]
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text resultText;

    [Header("文案")]
    [SerializeField] private string fishResultMessage = "釣到了魚！";
    [SerializeField] private string trashResultMessage = "釣到了垃圾！";
    [SerializeField] private string trashSellPriceSuffix = "（售價：1）";

    [Header("垃圾售出")]
    [SerializeField] private int trashSellPrice = 1;
    [SerializeField] private bool grantTrashCoinsImmediately = true;

    [Header("掉落表")]
    [SerializeField] private MonsterDropTable monsterDropTable;

    [Header("確認（可綁 UI 按鈕）")]
    [SerializeField] private Button rewardConfirmButton;

    bool _rewardDismissRequested;

    void Awake()
    {
        if (rewardConfirmButton != null)
            rewardConfirmButton.onClick.AddListener(RequestDismissReward);

        Hide();
    }

    void RequestDismissReward()
    {
        _rewardDismissRequested = true;
        AudioManager.GetInstance()?.PlayButtonSfx();
    }

    public void Hide()
    {
        if (rewardPanel != null)
            rewardPanel.SetActive(false);
    }

    public IEnumerator ShowFishRewardAndGrant(MonsterDropTable.DropEntry drop)
    {
        if (drop == null)
        {
            Hide();
            yield break;
        }

        string itemId = MonsterDropTable.ResolveItemId(drop);
        int amount = Mathf.Max(1, drop.amount);
        Sprite icon = null;
        string displayName = itemId;

        if (InventoryManager.Instance != null)
        {
            var data = InventoryManager.Instance.ResolveItemData(itemId);
            if (data != null)
            {
                displayName = data.itemName;
                icon = data.icon;
            }

            if (!string.IsNullOrEmpty(itemId))
                InventoryManager.Instance.AddItem(itemId, amount);
        }

        if (itemIcon != null)
        {
            itemIcon.enabled = icon != null;
            itemIcon.sprite = icon;
        }

        if (itemNameText != null)
            itemNameText.text = displayName ?? "";

        if (resultText != null)
            resultText.text = fishResultMessage;

        if (rewardPanel != null)
            rewardPanel.SetActive(true);

        if (PickupFeedbackManager.Instance != null && !string.IsNullOrEmpty(displayName))
            PickupFeedbackManager.Instance.ShowPickup(displayName, amount);

        yield return WaitUntilDismissed();
        Hide();
    }

    public IEnumerator ShowTrashRewardAndGrant(TrashData.TrashEntry trash)
    {
        if (trash == null)
        {
            Hide();
            yield break;
        }

        string displayName = trash.displayName;
        if (itemIcon != null)
        {
            itemIcon.enabled = trash.icon != null;
            itemIcon.sprite = trash.icon;
        }

        if (itemNameText != null)
            itemNameText.text = displayName ?? "";

        if (resultText != null)
            resultText.text = trashResultMessage + trashSellPriceSuffix;

        if (rewardPanel != null)
            rewardPanel.SetActive(true);

        if (grantTrashCoinsImmediately && CoinManager.Instance != null)
            CoinManager.Instance.AddCoins(trashSellPrice);

        string invId = string.IsNullOrWhiteSpace(trash.inventoryItemId)
            ? displayName
            : trash.inventoryItemId.Trim();

        if (InventoryManager.Instance != null && !string.IsNullOrEmpty(invId))
            InventoryManager.Instance.AddItem(invId, 1);

        yield return WaitUntilDismissed();
        Hide();
    }

    IEnumerator WaitUntilDismissed()
    {
        _rewardDismissRequested = false;
        while (!_rewardDismissRequested
               && !Input.GetKeyDown(KeyCode.E)
               && !Input.GetKeyDown(KeyCode.Escape))
        {
            yield return null;
        }
    }

    public MonsterDropTable MonsterDropTable => monsterDropTable;
}
