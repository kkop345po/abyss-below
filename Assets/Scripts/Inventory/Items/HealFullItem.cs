using UnityEngine;

namespace Game.Inventory.Items
{
    /// <summary>
    /// 大補血：回滿 100%。冷卻建議 20s（在 BaseUsableItem 設定）。
    /// </summary>
    public class HealFullItem : HealUsableItemBase
    {
        protected override bool ApplyHeal(PlayerHealth hp)
        {
            hp.HealFull();
            AudioManager.GetInstance()?.PlayUseMedkitSfx();
            return true;
        }
    }
}
