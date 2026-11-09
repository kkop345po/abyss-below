using UnityEngine;

namespace Game.Inventory.Items
{
    /// <summary>
    /// 爆彈：生成投擲物 Prefab，由 Prefab 自己處理彈道與爆炸。
    /// 此類負責扣數量與冷卻，並在 PerformEffect 時生成 Prefab。
    /// </summary>
    public class BombItem : BaseUsableItem
    {
        [Header("Bomb Config")]
        [SerializeField] private GameObject bombPrefab;
        [SerializeField] private Transform spawnPoint; // 若空則用此物件位置
        [SerializeField] private float throwSpeed = 10f;
        [SerializeField] private float explosionRadius = 2.5f;
        [SerializeField] private float explosionDamage = 80f;
        [SerializeField] private bool knockback = true;
        [SerializeField] private AnimationCurve knockbackCurve;
        [SerializeField] private LayerMask enemyLayer;

        protected override void PerformEffect()
        {
            if (bombPrefab == null)
            {
                Debug.LogWarning("[BombItem] bombPrefab not assigned");
                return;
            }
            var pos = spawnPoint != null ? spawnPoint.position : transform.position;
            var go = Instantiate(bombPrefab, pos, Quaternion.identity);
            
            // 嘗試設置初速度與參數
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 優先用玩家面向方向
                var player = GameObject.FindGameObjectWithTag("Player");
                Vector2 dir = Vector2.right; // 預設向右
                
                if (player != null)
                {
                    // 嘗試從 PlayerController 獲取更準確的面向
                    var ctrl = player.GetComponent<PlayerController>();
                    if (ctrl != null)
                    {
                        // 假設 PlayerController 有公開的面向屬性或方法，這裡先用 scale 判斷
                        // 若 PlayerController 有 facingSign 更好，但目前只能訪問公開成員
                        // 這裡改用 transform.localScale.x 判斷
                        float sign = Mathf.Sign(player.transform.localScale.x);
                        dir = new Vector2(sign, 0f);
                    }
                    else
                    {
                        dir = player.transform.right;
                    }
                }
                else
                {
                    // 若找不到玩家，用自己的 right
                    dir = transform.right;
                }
                
                rb.velocity = dir.normalized * throwSpeed;
            }

            // 若 Prefab 有 BombPayload2D，注入參數
            var payload = go.GetComponent<Game.Combat.BombPayload2D>();
            if (payload != null)
            {
                // 注意：BombPayload2D 需要更新 Initialize 方法以接收新參數
                // 這裡假設 BombPayload2D 已經更新了 Initialize 方法，或者我們需要去更新它
                // 根據之前的修改，BombPayload2D 的 Initialize 簽名是 (float radius, float damage, bool kb)
                // 我們需要先去 BombPayload2D 加開參數，或者直接在這裡賦值 public 欄位 (如果它是 public 的話)
                // 檢查 BombPayload2D 發現欄位是 private [SerializeField]，所以必須透過 Initialize 或屬性
                // 讓我們稍後更新 BombPayload2D 的 Initialize，現在先傳舊的，並用反射或額外方法設新的?
                // 不，正確做法是修改 BombPayload2D 的 Initialize。
                // 這裡先寫上理想的呼叫，等下立刻去改 BombPayload2D
                payload.Initialize(explosionRadius, explosionDamage, knockback);
                payload.SetExtraParams(knockbackCurve, enemyLayer); // 需新增此方法
            }
            Debug.Log($"[BombItem] Spawn bomb, R={explosionRadius}, Dmg={explosionDamage}");
        }
    }
}


