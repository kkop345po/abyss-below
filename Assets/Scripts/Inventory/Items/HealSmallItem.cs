using UnityEngine;

namespace Game.Inventory.Items
{
    /// <summary>
    /// 小補血：回最大生命的百分比。預設 10%，冷卻建議 8s（在 BaseUsableItem 設定）。
    /// </summary>
    public class HealSmallItem : HealUsableItemBase
    {
        [Range(0f, 1f)]
        [SerializeField] private float healPercent = 0.10f;

        protected override bool ApplyHeal(PlayerHealth hp)
        {
            hp.HealPercent(healPercent);
            AudioManager.GetInstance()?.PlayUseMedkitSfx();
            return true;
        }

        public float GetHealPercent() => healPercent;
    }
}
