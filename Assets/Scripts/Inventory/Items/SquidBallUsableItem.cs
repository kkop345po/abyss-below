using UnityEngine;

namespace Game.Inventory.Items
{
    public enum SquidBallTier
    {
        Small,
        Large,
        Enhanced
    }

    /// <summary>
    /// 花枝丸：使用時才套用 Buff；背包內為可堆疊 Item，Buff 由 <see cref="BuffManager"/> 計時且不存檔。
    /// </summary>
    public class SquidBallUsableItem : BaseUsableItem
    {
        [SerializeField] private SquidBallTier tier = SquidBallTier.Small;

        protected override void PerformEffect()
        {
            BuffManager.EnsureExists();
            if (BuffManager.Instance == null)
            {
                Debug.LogWarning("[SquidBallUsableItem] BuffManager 不存在");
                return;
            }

            switch (tier)
            {
                case SquidBallTier.Small:
                    BuffManager.Instance.ApplySquidBuff(30f, 1.2f);
                    break;
                case SquidBallTier.Large:
                    BuffManager.Instance.ApplySquidBuff(60f, 1.5f);
                    break;
                case SquidBallTier.Enhanced:
                    BuffManager.Instance.ApplySquidBuff(90f, 2f);
                    break;
            }
        }
    }
}
