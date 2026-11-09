using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Inventory.Items
{
    /// <summary>
    /// 通用可使用道具基底：處理數量/冷卻/事件，具備 UnityEvent 方便在 Inspector 綁定實際效果。
    /// 具體效果請在 OnPerformEffect 中（或綁 UnityEvent）實作。
    /// </summary>
    public abstract class BaseUsableItem : MonoBehaviour, Game.Inventory.IUsableItem
    {
        [Header("Visual")]
        [SerializeField] private Sprite icon;
        [Tooltip("若 ItemDatabase 有對應 itemId，啟用後會自動用資料庫 icon 覆蓋此欄，避免快捷欄/背包顯示不一致。")]
        [SerializeField] private bool preferDatabaseIcon = true;

        [Header("Inventory Link")] 
        [Tooltip("背包中的 itemId（例如 small_potion / Big_potion ...）")] 
        [SerializeField] protected string itemId; 
        [Tooltip("使用成功後是否自動從背包扣 1 件（推進器請關閉，改為在 Charges 用盡時扣）")] 
        [SerializeField] protected bool removeFromInventoryOnUse = true;

        [Header("Count & Cooldown")]
        [Tooltip("-1 表示無限，不顯示數量")]
        [SerializeField] private int startCount = -1;
        [Min(0f)]
        [SerializeField] private float cooldownSeconds = 0f;

        [Header("Events (Gameplay Hook)")]
        [SerializeField] protected UnityEvent onPerformed; // 使用成功時呼叫（給設計師在 Inspector 綁定）

        public Sprite Icon => icon;
        public int CurrentCount => currentCount;
        public float CooldownSeconds => cooldownSeconds;
        public bool IsOnCooldown => cooldownRemaining > 0f;
        public float CooldownRemaining => cooldownRemaining;

        public event Action OnCooldownEnded;
        public event Action<int> OnCountChanged;

        protected int currentCount;
        protected float cooldownRemaining;

        protected void NotifyCountChanged(int count) => OnCountChanged?.Invoke(count);

        protected void NotifyPerformed() => onPerformed?.Invoke();

        protected virtual void Awake()
        {
            currentCount = startCount;
        }

        public virtual bool TryUse()
        {
            if (currentCount == 0) return false;

            // 自訂前置檢查（如狀態禁止使用）
            if (!CanPerformEffect()) return false;

            // 扣數量（若非無限）
            if (currentCount > 0)
            {
                currentCount--;
                OnCountChanged?.Invoke(currentCount);
            }

            // 實際效果
            PerformEffect();
            onPerformed?.Invoke();

            // 背包扣除 1（如需）
            if (removeFromInventoryOnUse && !string.IsNullOrEmpty(itemId) && InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RemoveItem(itemId, 1);
            }
            return true;
        }

        protected virtual bool CanPerformEffect() => true;

        protected abstract void PerformEffect();

        protected virtual void Start()
        {
            if (preferDatabaseIcon)
                StartCoroutine(TryApplyDatabaseIcon());

            ClearCooldownState();
        }

        /// <summary>道具冷卻已停用；清除執行期與存檔殘留。</summary>
        protected void ClearCooldownState()
        {
            cooldownRemaining = 0f;

            if (!string.IsNullOrEmpty(itemId) && InventoryManager.Instance != null)
                InventoryManager.Instance.ClearCooldown(itemId);

            OnCooldownEnded?.Invoke();
        }

        private IEnumerator TryApplyDatabaseIcon()
        {
            // 等 InventoryManager 就緒（最多等 10 幀）
            for (int i = 0; i < 10 && InventoryManager.Instance == null; i++)
                yield return null;

            if (InventoryManager.Instance == null || string.IsNullOrEmpty(itemId))
                yield break;

            var data = InventoryManager.Instance.ResolveItemData(itemId);
            if (data != null && data.icon != null)
            {
                icon = data.icon;
            }
        }

        /// <summary>道具冷卻已停用；子類若仍呼叫此方法不會進入冷卻。</summary>
        protected void StartCooldown(float seconds, bool saveToManager = true)
        {
            ClearCooldownState();
        }

        // 提供外部加數量（購買/掉落）
        public void AddCount(int add)
        {
            if (startCount < 0 && currentCount < 0) return; // 無限不加
            currentCount = Mathf.Max(0, currentCount + add);
            OnCountChanged?.Invoke(currentCount);
        }

        // 取得 itemId（用於快捷欄綁定）
        public string GetItemId()
        {
            return itemId;
        }

        /// <summary>供跨場景常駐物件在執行期建立時設定 itemId／冷卻。</summary>
        public void ConfigureRuntime(string runtimeItemId, float runtimeCooldownSeconds = 0f, bool runtimeRemoveFromInventory = true)
        {
            if (!string.IsNullOrEmpty(runtimeItemId))
                itemId = runtimeItemId;
            cooldownSeconds = Mathf.Max(0f, runtimeCooldownSeconds);
            removeFromInventoryOnUse = runtimeRemoveFromInventory;
            currentCount = startCount;
        }

        // 設定當前數量（用於同步背包數量）
        public void SetCurrentCount(int count)
        {
            if (startCount < 0 && currentCount < 0) return; // 無限不設
            currentCount = Mathf.Max(0, count);
            OnCountChanged?.Invoke(currentCount);
        }
    }
}


