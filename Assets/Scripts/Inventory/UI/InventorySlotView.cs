using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Inventory.UI
{
    /// <summary>
    /// 方格視圖：支援整格灰階冷卻遮罩（由上而下退去）。
    /// 僅關注視圖與互動，具體使用邏輯由 IUsableItem 提供。
    /// </summary>
    public class InventorySlotView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownMask; // Image Type = Filled, Method = Vertical, Origin = Top
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Button clickButton;
        [SerializeField] private CanvasGroup canvasGroup; // 用於整格灰階/不可用
        [SerializeField] private TextMeshProUGUI cooldownText; // 新增：數字倒數
        [SerializeField] private Image flashImage; // 新增：冷卻完成閃光

        [Header("Optional Visuals")]
        [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color enabledColor = Color.white;

        private Game.Inventory.IUsableItem boundItem;
        private float currentCooldownTotal;

        private void Reset()
        {
            // 嘗試自動抓取
            iconImage = GetComponentInChildren<Image>();
            clickButton = GetComponentInChildren<Button>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            // 避免某些場景/預製體忘了在 Inspector 綁引用，導致點擊無效
            if (iconImage == null) iconImage = GetComponentInChildren<Image>(true);
            if (clickButton == null) clickButton = GetComponentInChildren<Button>(true);
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            if (clickButton != null)
            {
                clickButton.onClick.AddListener(OnClickUse);
            }
            SetCooldownFill(0f);
            if (cooldownText != null) cooldownText.text = "";
            if (flashImage != null)
            {
                flashImage.enabled = false;
                flashImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        public void Bind(Game.Inventory.IUsableItem item)
        {
            Unbind();
            boundItem = item;
            if (boundItem == null)
            {
                Debug.LogWarning("[InventorySlotView] 嘗試綁定 null 物品");
                ApplyEmpty();
                return;
            }

            Debug.Log($"[InventorySlotView] 綁定物品，Icon: {(boundItem.Icon != null ? boundItem.Icon.name : "null")}, Count: {boundItem.CurrentCount}");

            // 初始顯示：處理圖示（無圖示時隱藏 Image，有圖示時顯示並恢復正常透明度）
            if (iconImage != null)
            {
                if (boundItem.Icon != null)
                {
                    iconImage.enabled = true;
                    iconImage.sprite = boundItem.Icon;
                    // 確保顏色 alpha 為正常值（恢復透明度）
                    var color = iconImage.color;
                    color.a = 1f;
                    iconImage.color = color;
                    Debug.Log($"[InventorySlotView] 已設置圖示: {boundItem.Icon.name}, Image.enabled: {iconImage.enabled}, Color.a: {iconImage.color.a}");
                }
                else
                {
                    // 沒有圖示時隱藏 Image，避免顯示白色色塊
                    iconImage.enabled = false;
                    Debug.LogWarning("[InventorySlotView] 物品沒有 Icon，已隱藏 Image");
                }
            }
            else
            {
                Debug.LogError("[InventorySlotView] iconImage 為 null，請檢查 Inspector 設置");
            }
            UpdateCount(boundItem.CurrentCount);
            ApplyInteractable(!boundItem.IsOnCooldown);
            SetCooldownFromState();

            // 訂閱事件
            boundItem.OnCooldownEnded += HandleCooldownEnded;
            boundItem.OnCountChanged += UpdateCount;
        }

        public void Unbind()
        {
            if (boundItem == null) return;
            boundItem.OnCooldownEnded -= HandleCooldownEnded;
            boundItem.OnCountChanged -= UpdateCount;
            boundItem = null;
            ApplyEmpty(); // 清空顯示
        }

        public Game.Inventory.IUsableItem GetBoundItem()
        {
            return boundItem;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnClickUse()
        {
            if (!IsBoundItemAlive())
            {
                TryRebindBoundItem();
                if (!IsBoundItemAlive())
                    return;
            }

            if (!boundItem.IsOnCooldown)
            {
                var ok = boundItem.TryUse();
                if (!ok)
                {
                    // 使用失敗的視覺/音效可在此觸發
                }
            }
            else
            {
                // 冷卻中回饋（可加微弱震動/音效）
            }
        }

        bool IsBoundItemAlive()
        {
            if (boundItem == null)
                return false;
            if (boundItem is MonoBehaviour mb)
                return mb != null;
            return true;
        }

        void TryRebindBoundItem()
        {
            var binder = InventoryQuickbarBinder.Instance;
            var slots = binder != null ? binder.GetSlotViews() : null;
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != this)
                    continue;
                binder.TryRebindSlot(i);
                boundItem = GetBoundItem();
                return;
            }
        }

        private void HandleCooldownEnded()
        {
            SetCooldownFill(0f);
            ApplyInteractable(true);
            if (cooldownText != null) cooldownText.text = "";
            
            // 觸發閃光
            if (flashImage != null)
            {
                flashImage.enabled = true;
                flashImage.color = Color.white;
                flashImage.CrossFadeAlpha(0f, 0.3f, false);
            }
        }

        private void SetCooldownFromState()
        {
            if (boundItem == null)
            {
                SetCooldownFill(0f);
                ApplyInteractable(false);
                return;
            }

            currentCooldownTotal = Mathf.Max(0.0001f, boundItem.CooldownSeconds);
            if (boundItem.IsOnCooldown)
            {
                var t = Mathf.Clamp01(boundItem.CooldownRemaining / currentCooldownTotal);
                SetCooldownFill(t);
                ApplyInteractable(false);
            }
            else
            {
                SetCooldownFill(0f);
                ApplyInteractable(true);
            }
        }

        private void UpdateCount(int count)
        {
            if (countText == null) return;
            if (count < 0)
            {
                countText.text = string.Empty;
            }
            else
            {
                countText.text = count.ToString();
            }
        }

        private void ApplyEmpty()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false; // 隱藏 Image，避免顯示白色色塊
            }
            UpdateCount(0);
            ApplyInteractable(false);
            SetCooldownFill(0f);
        }

        private void ApplyInteractable(bool canInteract)
        {
            if (clickButton != null) clickButton.interactable = canInteract;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = canInteract ? 1f : 0.8f;
            }
            if (iconImage != null && iconImage.enabled)
            {
                // 只在 Image 啟用時才設置顏色，並保持 alpha = 1（確保可見）
                var color = canInteract ? enabledColor : disabledColor;
                color.a = 1f; // 確保 alpha 始終為 1，避免透明度問題
                iconImage.color = color;
            }
        }

        private void SetCooldownFill(float normalized)
        {
            if (cooldownMask == null) return;
            cooldownMask.type = Image.Type.Filled;
            cooldownMask.fillMethod = Image.FillMethod.Vertical;
            cooldownMask.fillOrigin = (int)Image.OriginVertical.Top;
            cooldownMask.fillAmount = Mathf.Clamp01(normalized);
            cooldownMask.enabled = normalized > 0.001f;
        }
    }
}


