using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 武器強化面板：顯示目前 Lv、下一階說明與消耗，可搭配 <see cref="MaterialDatabase"/> 顯示素材圖、自訂「下一階」示意圖。
/// </summary>
public class WeaponUpgradeUIController : MonoBehaviour
{
    [Header("顯示（文字）")]
    [SerializeField] private TextMeshProUGUI currentLevelText;
    [SerializeField] private TextMeshProUGUI nextTitleText;
    [SerializeField] private TextMeshProUGUI nextStatText;
    [SerializeField] private TextMeshProUGUI nextFeelText;
    [SerializeField] private TextMeshProUGUI costText;
    [Tooltip("可選：與下方三張回饋圖並用，顯示完整說明文字")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("訊息回饋（三種圖片，可選）")]
    [Tooltip("升級成功時顯示")]
    [SerializeField] private Image messageSuccessImage;
    [Tooltip("金幣或素材不足時顯示")]
    [SerializeField] private Image messageFailImage;
    [Tooltip("其餘情況：已滿級、找不到管理器等")]
    [SerializeField] private Image messageNeutralImage;

    [SerializeField] private Button upgradeButton;

    [Header("圖片：下一階示意（可選）")]
    [Tooltip("顯示「即將解鎖那一階」的宣傳圖；陣列須 5 張，索引 0＝升到 Lv.1、…、4＝升到 Lv.5")]
    [SerializeField] private Image nextTierFeatureImage;
    [SerializeField] private Sprite[] nextTierFeatureSprites;

    [Tooltip("已滿級（Lv.5）時顯示的圖（可選）；未指定則關閉 nextTierFeatureImage")]
    [SerializeField] private Sprite maxTierFeatureSprite;

    [Header("圖片：消耗素材（可選）")]
    [Tooltip("與專案裡 MaterialDatabase 資產相同一份即可，用 mat_ 編號對應圖示")]
    [SerializeField] private MaterialDatabase materialDatabase;

    [SerializeField] private Image materialCostIconA;
    [SerializeField] private Image materialCostIconB;

    [Tooltip("可選：金幣小圖（精靈拖在 Image 上即可，不必接資料庫）")]
    [SerializeField] private Image coinHintIcon;

    UpgradeUIController _upgradeShell;

    void Awake()
    {
        _upgradeShell = GetComponentInParent<UpgradeUIController>();
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnClickUpgrade);
    }

    void OnEnable()
    {
        _upgradeShell?.EnsureSharedCoinBarVisible();
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        int lv = PlayerDataManager.WeaponUpgradeLevel;
        if (currentLevelText != null)
            currentLevelText.text = lv >= WeaponUpgradeBalance.MaxLevel
                ? $"武器強化　Lv.{lv}（已滿）"
                : $"武器強化　目前 Lv.{lv}";

        if (coinHintIcon != null)
            coinHintIcon.gameObject.SetActive(true);

        if (lv >= WeaponUpgradeBalance.MaxLevel)
        {
            if (nextTitleText != null) nextTitleText.text = "—";
            if (nextStatText != null) nextStatText.text = "已解鎖全部階段。";
            if (nextFeelText != null) nextFeelText.text = "";
            if (costText != null) costText.text = "";
            if (upgradeButton != null) upgradeButton.interactable = false;
            ApplyMaterialCostIcons(MaterialType.None, 0, MaterialType.None, 0);
            ApplyNextTierFeatureImage(-1);
            ClearMessageFeedback();
            return;
        }

        if (WeaponUpgradeService.TryBuildNextLevelPreview(out string title, out string stat, out string feel, out string cost))
        {
            if (nextTitleText != null) nextTitleText.text = $"下一階：{title}";
            if (nextStatText != null) nextStatText.text = stat;
            if (nextFeelText != null) nextFeelText.text = feel;
            if (costText != null) costText.text = cost;
        }

        if (WeaponUpgradeService.TryGetPendingUpgradeMaterials(out MaterialType ma, out int ca, out MaterialType mb, out int cb))
            ApplyMaterialCostIcons(ma, ca, mb, cb);
        else
            ApplyMaterialCostIcons(MaterialType.None, 0, MaterialType.None, 0);

        ApplyNextTierFeatureImage(lv);

        if (upgradeButton != null)
            upgradeButton.interactable = true;

        ClearMessageFeedback();
    }

    /// <summary>目前等級 lv（0～4）時，顯示「下一階＝Lv.(lv+1)」對應的圖；傳入 -1 表示已滿級。</summary>
    void ApplyNextTierFeatureImage(int currentLevelForNextPreview)
    {
        if (nextTierFeatureImage == null) return;

        if (currentLevelForNextPreview < 0)
        {
            if (maxTierFeatureSprite != null)
            {
                nextTierFeatureImage.gameObject.SetActive(true);
                nextTierFeatureImage.sprite = maxTierFeatureSprite;
                nextTierFeatureImage.enabled = true;
            }
            else
            {
                nextTierFeatureImage.gameObject.SetActive(false);
            }
            return;
        }

        if (nextTierFeatureSprites == null || nextTierFeatureSprites.Length == 0)
        {
            nextTierFeatureImage.gameObject.SetActive(false);
            return;
        }

        int idx = Mathf.Clamp(currentLevelForNextPreview, 0, nextTierFeatureSprites.Length - 1);
        var sp = nextTierFeatureSprites[idx];
        if (sp != null)
        {
            nextTierFeatureImage.gameObject.SetActive(true);
            nextTierFeatureImage.sprite = sp;
            nextTierFeatureImage.enabled = true;
        }
        else
            nextTierFeatureImage.gameObject.SetActive(false);
    }

    void ApplyMaterialCostIcons(MaterialType a, int countA, MaterialType b, int countB)
    {
        SetMaterialIcon(materialCostIconA, a, countA > 0 && a != MaterialType.None);
        SetMaterialIcon(materialCostIconB, b, countB > 0 && b != MaterialType.None);
    }

    void SetMaterialIcon(Image img, MaterialType type, bool needShow)
    {
        if (img == null) return;
        if (!needShow)
        {
            img.gameObject.SetActive(false);
            return;
        }

        Sprite s = null;
        if (materialDatabase != null)
        {
            string id = "mat_" + (int)type;
            var data = materialDatabase.GetMaterial(id);
            if (data != null)
                s = data.icon;
        }

        img.sprite = s;
        img.gameObject.SetActive(s != null);
        img.enabled = s != null;
    }

    void ClearMessageFeedback()
    {
        SetMessageImagesVisible(null);
        if (messageText != null)
            messageText.text = "";
    }

    void SetMessageImagesVisible(Image only)
    {
        if (messageSuccessImage != null)
            messageSuccessImage.gameObject.SetActive(only == messageSuccessImage);
        if (messageFailImage != null)
            messageFailImage.gameObject.SetActive(only == messageFailImage);
        if (messageNeutralImage != null)
            messageNeutralImage.gameObject.SetActive(only == messageNeutralImage);
    }

    void ApplyMessageFeedback(WeaponUpgradeAttemptResult result, string msg)
    {
        if (messageText != null)
            messageText.text = msg ?? "";

        Image show = null;
        switch (result)
        {
            case WeaponUpgradeAttemptResult.Success:
                show = messageSuccessImage;
                break;
            case WeaponUpgradeAttemptResult.NotEnoughGold:
            case WeaponUpgradeAttemptResult.NotEnoughMaterials:
                show = messageFailImage;
                break;
            default:
                show = messageNeutralImage;
                break;
        }

        if (show != null)
            SetMessageImagesVisible(show);
        else
            SetMessageImagesVisible(null);
    }

    public void OnClickUpgrade()
    {
        int cur = PlayerDataManager.WeaponUpgradeLevel;
        int coinsBefore = PlayerDataManager.Coins;
        int goldCost = 0;
        if (cur < WeaponUpgradeBalance.MaxLevel)
            WeaponUpgradeBalance.TryGetCostForNextLevel(cur, out goldCost, out _, out _, out _, out _);

        var coinFeedback = _upgradeShell != null ? _upgradeShell.GetCoinSpendFeedback() : null;
        if (coinFeedback != null)
            coinFeedback.SetAnimLock(true);

        var result = WeaponUpgradeService.TryUpgradeNext(out string msg);

        if (coinFeedback != null)
        {
            if (result == WeaponUpgradeAttemptResult.Success && goldCost > 0)
            {
                bool reachedMax = PlayerDataManager.WeaponUpgradeLevel >= PlayerDataManager.WeaponUpgradeMaxLevel;
                _upgradeShell.PlayCoinSpendFeedback(coinsBefore, goldCost, reachedMax);
            }
            else
                coinFeedback.CancelPresentation();
        }

        if (result == WeaponUpgradeAttemptResult.Success)
            RefreshDisplay();
        ApplyMessageFeedback(result, msg);
    }
}
