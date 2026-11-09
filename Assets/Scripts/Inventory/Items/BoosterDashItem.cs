using System.Collections;
using UnityEngine;

namespace Game.Inventory.Items
{
    /// <summary>
    /// 推進器：0.25s 內向面朝方向推進 10m，期間無敵。共有 3 次使用次數。
    /// 實際移動/無敵請在 onPerformed 事件中綁定你的角色控制器。
    /// </summary>
    public class BoosterDashItem : BaseUsableItem
    {
        [Header("Dash Config")]
        [SerializeField] private float dashDuration = 0.25f;
        [SerializeField] private float dashDistance = 10f;
        [SerializeField] private bool invincibleDuringDash = true;
        [SerializeField] private int charges = 3;

        private int remainingCharges;

        protected override void Awake()
        {
            base.Awake();
            remainingCharges = charges;
        }

        public override bool TryUse()
        {
            if (remainingCharges <= 0) return false;
            var ok = base.TryUse();
            if (ok)
            {
                remainingCharges--;
                if (remainingCharges <= 0 && !string.IsNullOrEmpty(itemId) && InventoryManager.Instance != null)
                    InventoryManager.Instance.RemoveItem(itemId, 1);
            }
            return ok;
        }

        protected override void PerformEffect()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var ctrl = player.GetComponent<PlayerController>()
                       ?? player.GetComponentInChildren<PlayerController>(true);
            if (ctrl != null)
                ctrl.StartCoroutine(ctrl.Dash(dashDuration, dashDistance, invincibleDuringDash));
        }

        public (float duration, float distance, bool invincible) GetDashSpec() =>
            (dashDuration, dashDistance, invincibleDuringDash);

        public int GetRemainingCharges() => remainingCharges;
    }
}
