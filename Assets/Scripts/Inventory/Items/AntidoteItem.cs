using UnityEngine;

namespace Game.Inventory.Items
{
    /// <summary>
    /// 解毒劑：解除負面狀態（實際解除邏輯請在事件中接你的狀態系統）。
    /// </summary>
    public class AntidoteItem : BaseUsableItem
    {
        [Tooltip("使用後給短暫免疫(秒)，0 代表不給免疫窗")]
        [SerializeField] private float immunitySeconds = 2f;

        protected override void PerformEffect()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var dot = player.GetComponent<PlayerDamageOverTime>();
                if (dot != null) dot.Clear();
                if (immunitySeconds > 0f)
                {
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null) hp.InvincibleFor(immunitySeconds);
                }
            }
            Debug.Log($"[AntidoteItem] Clear debuffs, grant {immunitySeconds}s immunity");
        }

        public float GetImmunitySeconds() => immunitySeconds;
    }
}


