using UnityEngine;

namespace Game.Inventory.Items
{
    /// <summary>
    /// 護甲：給減傷百分比一段時間。購買後即使用可將此物件放在商店流程中直接呼叫 TryUse。
    /// </summary>
    public class ArmorBuffItem : BaseUsableItem
    {
        [Range(0f, 1f)]
        [SerializeField] private float damageReductionPercent = 0.2f; // 20%
        [SerializeField] private float durationSeconds = 60f;

        protected override void PerformEffect()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var hp = player.GetComponent<PlayerHealth>();
                if (hp != null)
                {
                    hp.ApplyDamageReduction(damageReductionPercent, durationSeconds);
                }
            }
            Debug.Log($"[ArmorBuffItem] DR={damageReductionPercent * 100f}% for {durationSeconds}s");
        }

        public (float dr, float duration) GetBuffSpec() => (damageReductionPercent, durationSeconds);
    }
}


