using UnityEngine;

// 🐢 孩膽（小型/被動）：低血、低傷，簡單左右游動，撞到玩家才扣血
public class Haidan : BaseEnemy2D
{
    [Header("海膽攻擊設定")]
    public float jumpImpulse = 6f; // 向上跳的衝力（Impulse）
    public float nudgeTowardsPlayer = 0.5f; // 向玩家微量水平推進（0=純垂直）

    protected override void Awake()
    {
        base.Awake();
        isAggressive = false; // 被動怪
        enablePatrol = false; // 定點待機

        // 需求：海膽（Haidan）對玩家固定傷害 10 HP
        damageToPlayer = 10;
    }

    private bool isPreparingJump = false;
    private float jumpDelayTimer = 0f;

    protected override void TickBehavior()
    {
        // 定點待機：不主動移動，僅在玩家接近時進入攻擊判定
        if (player == null) return;
        float dist = DistanceToPlayer();
        if (dist <= chaseRange)
        {
            // 保持面向玩家（8方向）
            Vector2 dir = (player.position - transform.position);
            FaceByVector(dir);
            
            // 觸發向上跳攻擊（冷卻控制）
            if (CanAttack() && rb != null)
            {
                if (!isPreparingJump)
                {
                    isPreparingJump = true;
                    jumpDelayTimer = Random.Range(0.2f, 0.6f); // 隨機預備時間
                    // 這裡可以播放一個 "預備" 動畫 trigger，例如 animator.SetTrigger("Charge");
                    transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.8f, 1f); // 簡單的壓扁效果
                }
                else
                {
                    jumpDelayTimer -= Time.fixedDeltaTime;
                    if (jumpDelayTimer <= 0f)
                    {
                        // 還原形狀
                        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y / 0.8f, 1f);
                        
                        // 清空垂直速度，給予向上衝力 + 朝向玩家的微量水平推進
                        Vector2 jumpDir = new Vector2(Mathf.Sign(dir.x) * nudgeTowardsPlayer, 1f).normalized;
                        FaceByVector(jumpDir);
                        rb.velocity = new Vector2(rb.velocity.x, 0f);
                        rb.AddForce(jumpDir * jumpImpulse, ForceMode2D.Impulse);

                        lastAttackTime = Time.time;
                        if (animator) animator.SetTrigger("Attack");
                        
                        isPreparingJump = false;
                    }
                }
            }
            // 在 OnTriggerEnter2D 會處理接觸傷害
        }
        else
        {
            isPreparingJump = false; // 玩家離開範圍取消蓄力
        }
    }
}


