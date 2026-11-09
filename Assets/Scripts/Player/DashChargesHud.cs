using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 顯示玩家衝刺次數（格）。掛在 GameUI 等 HUD Canvas 下，由 <see cref="PlayerController"/> 讀取格數與回復進度。
/// </summary>
public class DashChargesHud : MonoBehaviour
{
    [Tooltip("留空則執行時 FindFirstObjectByType<PlayerController>")]
    [SerializeField] PlayerController player;

    [Tooltip("要顯示／隱藏的根物件（建議為本物件的子節點）。留空則用 CanvasGroup 或整組 Image。")]
    [SerializeField] GameObject visibilityRoot;

    [Tooltip("由左到右對應第 1～N 格。數量可大於最大格數，多出的會隱藏。")]
    [SerializeField] Image[] chargeSlots;

    [Tooltip("每格「已滿」圖；留空則只改顏色。")]
    [SerializeField] Sprite slotFullSprite;

    [Tooltip("每格「已用／回復中」圖；留空則用 slotFullSprite + 半透明。")]
    [SerializeField] Sprite slotEmptySprite;

    [SerializeField] Color fullColor = Color.white;
    [SerializeField] Color emptyColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] Color regenTintColor = new Color(0.55f, 0.85f, 1f, 0.9f);

    [Header("回復進度（可選）")]
    [Tooltip("與 chargeSlots 同長度；Fill Amount 由 0→1 表示該格正在回復。可留空。")]
    [SerializeField] Image[] regenFillOverlays;

    [Tooltip("dashOnlyInWater 為 true 時，在陸地隱藏整組 UI")]
    [SerializeField] bool hideWhenDashUnavailableOnLand = true;

    CanvasGroup _rootCanvasGroup;

    void Awake()
    {
        if (visibilityRoot != null && visibilityRoot != gameObject)
            _rootCanvasGroup = visibilityRoot.GetComponent<CanvasGroup>();
        else
            _rootCanvasGroup = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        if (player == null)
            player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);

        if (player == null)
        {
            SetHudVisible(false);
            return;
        }

        bool show = true;
        if (hideWhenDashUnavailableOnLand && player.DashOnlyInWater && !player.IsInWater)
            show = false;

        SetHudVisible(show);
        if (!show)
            return;

        int max = Mathf.Max(1, player.DashMaxCharges);
        int current = Mathf.Clamp(player.DashCharges, 0, max);
        float regen01 = player.DashRegenProgress01;
        bool regenActive = player.DashRegenPending > 0 && regen01 > 0f;

        if (chargeSlots == null)
            return;

        for (int i = 0; i < chargeSlots.Length; i++)
        {
            Image slot = chargeSlots[i];
            if (slot == null)
                continue;

            bool inMax = i < max;
            slot.gameObject.SetActive(inMax);
            if (!inMax)
                continue;

            bool filled = i < current;
            ApplySlotVisual(slot, filled, regenActive && i == current);

            if (regenFillOverlays != null && i < regenFillOverlays.Length)
            {
                Image fill = regenFillOverlays[i];
                if (fill == null)
                    continue;

                bool isRegenSlot = regenActive && i == current;
                fill.gameObject.SetActive(isRegenSlot);
                if (isRegenSlot)
                {
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Vertical;
                    fill.fillOrigin = (int)Image.OriginVertical.Bottom;
                    fill.fillAmount = regen01;
                }
            }
        }
    }

    void ApplySlotVisual(Image slot, bool filled, bool regenSlot)
    {
        if (filled)
        {
            if (slotFullSprite != null)
                slot.sprite = slotFullSprite;
            slot.color = fullColor;
            return;
        }

        if (slotEmptySprite != null)
            slot.sprite = slotEmptySprite;
        else if (slotFullSprite != null)
            slot.sprite = slotFullSprite;

        slot.color = regenSlot ? regenTintColor : emptyColor;
    }

    void SetHudVisible(bool active)
    {
        if (visibilityRoot != null && visibilityRoot != gameObject)
        {
            visibilityRoot.SetActive(active);
            return;
        }

        if (_rootCanvasGroup != null)
        {
            _rootCanvasGroup.alpha = active ? 1f : 0f;
            _rootCanvasGroup.interactable = false;
            _rootCanvasGroup.blocksRaycasts = false;
            return;
        }

        if (chargeSlots != null)
        {
            foreach (var img in chargeSlots)
            {
                if (img != null)
                    img.enabled = active;
            }
        }
    }
}
