using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 使用花枝丸後，在畫面上顯示小狀態圖示（可選剩餘秒數）。訂閱 <see cref="BuffManager"/> 事件。
/// 請掛在永遠存在的 HUD Canvas 下，並在 Inspector 指定圖示（可三檔各一張，或只填一張共用）。
/// </summary>
public class SquidBuffStatusHud : MonoBehaviour
{
    [Tooltip("要顯示／隱藏的根物件（建議為子物件）。若拖成與本腳本同一物件，將改用 CanvasGroup 或只開關 Image／文字，避免 SetActive 把自己關掉後無法再訂閱 Buff 事件。")]
    [SerializeField] GameObject visibilityRoot;

    [SerializeField] Image statusIcon;

    [Tooltip("留空則不顯示倒數數字")]
    [SerializeField] TextMeshProUGUI remainingSecondsText;

    [Header("依倍率切換圖示（與 SquidBallUsableItem 的 1.2 / 1.5 / 2.0 對齊）")]
    [SerializeField] Sprite iconSmallSquid;
    [SerializeField] Sprite iconLargeSquid;
    [SerializeField] Sprite iconEnhancedSquid;

    static bool ApproximatelyMultiplier(float a, float b) => Mathf.Abs(a - b) < 0.06f;

    void OnEnable()
    {
        BuffManager.OnMultiplierChanged += OnMultiplierChanged;
        BuffManager.OnBuffTimeChanged += OnBuffTimeChanged;
        SyncFromBuffManager();
    }

    void OnDisable()
    {
        BuffManager.OnMultiplierChanged -= OnMultiplierChanged;
        BuffManager.OnBuffTimeChanged -= OnBuffTimeChanged;
    }

    void OnMultiplierChanged(float multiplier) => SyncFromBuffManager();

    void OnBuffTimeChanged(float _) => SyncFromBuffManager();

    void SyncFromBuffManager()
    {
        if (BuffManager.Instance == null)
        {
            ApplyVisual(1f, 0f);
            return;
        }

        ApplyVisual(
            BuffManager.Instance.CurrentAbilityMultiplier,
            BuffManager.Instance.RemainingBuffSeconds);
    }

    void ApplyVisual(float abilityMultiplier, float remainingSeconds)
    {
        bool active = abilityMultiplier > 1.01f && remainingSeconds > 0f;

        SetHudVisible(active);

        if (!active)
            return;

        if (statusIcon != null)
        {
            Sprite s = ResolveIconSprite(abilityMultiplier);
            if (s != null)
                statusIcon.sprite = s;
        }

        if (remainingSecondsText != null)
            remainingSecondsText.text = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds)).ToString();
    }

    /// <summary>
    /// 不可對掛載本腳本的 GameObject SetActive(false)，否則 OnDisable 會取消訂閱，下次使用花枝丸時圖示永遠不更新。
    /// </summary>
    void SetHudVisible(bool active)
    {
        if (visibilityRoot != null && visibilityRoot != gameObject)
        {
            visibilityRoot.SetActive(active);
            return;
        }

        if (visibilityRoot == gameObject && visibilityRoot.TryGetComponent<CanvasGroup>(out var cg))
        {
            cg.alpha = active ? 1f : 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
            return;
        }

        if (statusIcon != null)
            statusIcon.enabled = active;
        if (remainingSecondsText != null)
            remainingSecondsText.enabled = active;
    }

    Sprite ResolveIconSprite(float mult)
    {
        if (ApproximatelyMultiplier(mult, 2f) && iconEnhancedSquid != null)
            return iconEnhancedSquid;
        if (ApproximatelyMultiplier(mult, 1.5f) && iconLargeSquid != null)
            return iconLargeSquid;
        if (ApproximatelyMultiplier(mult, 1.2f) && iconSmallSquid != null)
            return iconSmallSquid;
        if (iconEnhancedSquid != null)
            return iconEnhancedSquid;
        if (iconLargeSquid != null)
            return iconLargeSquid;
        if (iconSmallSquid != null)
            return iconSmallSquid;
        return null;
    }
}
