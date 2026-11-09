using UnityEngine;

// 🦀 后里蟹（小型/主動）：主動追擊、速度快
public class HouliCrab : BaseEnemy2D
{
    [Header("追擊")]
    public float sprintSpeed = 5.5f;
    public float sprintTriggerRange = 6f;

    [Header("跳躍攻擊")]
    public float jumpAttackRange = 2.5f;
    public float jumpForce = 5f;

    protected override void Awake()
    {
        base.Awake();
        isAggressive = true; // 主動怪
        
        // 啟用自然游動並設定為高頻大幅度 (螃蟹步)
        enableOrganicMovement = true;
        sineFrequency = 8f;
        sineMagnitude = 1.2f;
    }

    protected override void TickBehavior()
    {
        float dist = DistanceToPlayer();
        
        // 跳躍攻擊邏輯
        if (dist <= jumpAttackRange && CanAttack())
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.AddForce(dir * jumpForce + Vector2.up * 2f, ForceMode2D.Impulse);
            lastAttackTime = Time.time;
            if (animator) animator.SetTrigger("Attack");
            return;
        }

        if (dist <= sprintTriggerRange && dist > stopDistance)
        {
            MoveTowards(player.position, sprintSpeed);
        }
        else
        {
            Patrol();
        }
    }
}


