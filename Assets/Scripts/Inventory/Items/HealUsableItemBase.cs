using UnityEngine;

namespace Game.Inventory.Items
{
    /// <summary>
    /// 補血道具共用：回血成功才扣快捷欄數量與背包（避免輸出檔找不到玩家卻仍消耗道具）。
    /// </summary>
    public abstract class HealUsableItemBase : BaseUsableItem
    {
        public override bool TryUse()
        {
            if (currentCount == 0)
                return false;

            var hp = PlayerLocator.GetHealth();
            if (hp == null)
            {
                Debug.LogWarning($"[{GetType().Name}] 找不到 PlayerHealth，取消使用（不扣道具）");
                return false;
            }

            if (hp.GetCurrentHealth() >= hp.GetMaxHealth())
                return false;

            int before = hp.GetCurrentHealth();
            if (!ApplyHeal(hp))
                return false;

            if (hp.GetCurrentHealth() <= before)
            {
                Debug.LogWarning($"[{GetType().Name}] 回血後血量未增加（{before}→{hp.GetCurrentHealth()}），取消扣道具");
                return false;
            }

            if (currentCount > 0)
            {
                currentCount--;
                NotifyCountChanged(currentCount);
            }

            NotifyPerformed();

            if (removeFromInventoryOnUse && !string.IsNullOrEmpty(itemId) && InventoryManager.Instance != null)
                InventoryManager.Instance.RemoveItem(itemId, 1);

            return true;
        }

        protected override bool CanPerformEffect() => true;

        protected sealed override void PerformEffect()
        {
            // 實際回血在 TryUse 內完成
        }

        protected abstract bool ApplyHeal(PlayerHealth hp);
    }
}
