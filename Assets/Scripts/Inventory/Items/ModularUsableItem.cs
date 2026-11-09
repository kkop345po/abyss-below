using System.Collections.Generic;
using UnityEngine;
using Game.Inventory.Items;

namespace Game.Inventory.Items
{
    /// <summary>
    /// 模組化道具：使用 ItemEffectSO 列表來定義行為
    /// </summary>
    public class ModularUsableItem : BaseUsableItem
    {
        [Header("Effects")]
        [SerializeField] private List<ItemEffectSO> effects = new List<ItemEffectSO>();

        protected override void PerformEffect()
        {
            GameObject user = PlayerLocator.GetGameObject();
            if (user == null)
            {
                Debug.LogWarning("[ModularUsableItem] 找不到玩家");
                return;
            }

            foreach (var effect in effects)
            {
                effect.ExecuteEffect(user);
            }
        }

        protected override bool CanPerformEffect()
        {
            GameObject user = PlayerLocator.GetGameObject();
            if (user == null) return false;

            // 只要有一個效果可以執行，就允許使用
            // 或者：所有效果都必須可以執行？通常是"至少一個"
            foreach (var effect in effects)
            {
                if (effect.CanExecute(user))
                {
                    return true;
                }
            }
            
            // 如果所有效果都不能執行（例如滿血時喝水），則返回 false
            return false;
        }
    }
}
