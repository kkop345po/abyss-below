using UnityEngine;

// 🐙 中卷（中型/主動）：黏人持續追擊
public class Squid : BaseEnemy2D
{
    [Header("追擊行為")]
    public float stickRange = 7f;
    public float stickSpeed = 4.5f;



    [Header("墨汁逃脫")]
    public float fleeDuration = 2f;
    public float fleeSpeed = 7f;
    private bool hasSquirted = false;
    private bool isFleeing = false;
    private float fleeTimer = 0f;

    [Header("脈衝移動")]
    public float pulseMoveTime = 0.8f;
    public float pulseStopTime = 0.4f;
    private float pulseTimer = 0f;
    private bool isPulsingMove = true;

    protected override void Awake()
    {
        base.Awake();
        isAggressive = true; // 主動怪
        enableOrganicMovement = true; // 啟用自然游動
    }

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
        // 血量低於 50% 觸發噴墨
        if (!hasSquirted && currentHP < maxHP * 0.5f)
        {
            SquirtInk();
        }
    }

    private void SquirtInk()
    {
        hasSquirted = true;
        isFleeing = true;
        fleeTimer = fleeDuration;
        
        // 視覺效果：變黑或生成粒子 (這裡先用 Log)
        Debug.Log("Squid squirts ink!");
        // 如果有粒子特效可以在這裡 Instantiate
        
        // 簡單的視覺回饋：暫時變黑
        foreach(var sr in flashRenderers) sr.color = Color.black;
    }

    protected override void TickBehavior()
    {
        // 逃跑邏輯
        if (isFleeing)
        {
            fleeTimer -= Time.fixedDeltaTime;
            if (player != null)
            {
                // 反向移動
                Vector2 fleeDir = (transform.position - player.position).normalized;
                MoveTowards(transform.position + (Vector3)fleeDir, fleeSpeed);
            }
            
            if (fleeTimer <= 0f)
            {
                isFleeing = false;
                // 恢復顏色 (這裡假設 BaseEnemy 會處理 flash，所以可能會有衝突，但暫時這樣)
                foreach(var sr in flashRenderers) sr.color = Color.white; 
            }
            return;
        }

        float dist = DistanceToPlayer();
        if (dist <= stickRange && dist > stopDistance)
        {
            // 脈衝移動邏輯
            pulseTimer -= Time.fixedDeltaTime;
            if (pulseTimer <= 0f)
            {
                isPulsingMove = !isPulsingMove;
                pulseTimer = isPulsingMove ? pulseMoveTime : pulseStopTime;
                
                // 衝刺時給一點爆發力
                if (isPulsingMove && rb != null)
                {
                    Vector2 dir = (player.position - transform.position).normalized;
                    rb.AddForce(dir * 2f, ForceMode2D.Impulse);
                }
            }

            if (isPulsingMove)
            {
                MoveTowards(player.position, stickSpeed);
            }
            else
            {
                // 停止時慢慢減速
                if (rb != null) rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.fixedDeltaTime * 5f);
            }
        }
        else
        {
            Patrol();
        }
    }
}


