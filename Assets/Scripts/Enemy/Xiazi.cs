using UnityEngine;

// 🐍 瞎子（小型/被動）：多個體行動，每個體=10HP，偶發突刺嚇人
public class Xiazi : BaseEnemy2D
{
    [Header("群體/血量（每個體=10HP）")]
    public int hpPerUnit = 10;
    public int minUnits = 3; // 對應 30HP
    public int maxUnits = 5; // 對應 50HP

    [Header("突刺")]
    public float dashSpeed = 10f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 2.5f;
    public float dashTriggerRange = 5f;

    private float dashTimer = 0f;
    private float lastDashTime = -999f;
    private bool isDashing = false;

    protected override void Awake()
    {
        base.Awake();
        isAggressive = false; // 被動怪
        facingAngleOffset = 180f; // 反向修正
        int units = Mathf.Max(1, Random.Range(minUnits, maxUnits + 1));
        currentHP = Mathf.Max(1, units * Mathf.Max(1, hpPerUnit));
        maxHP = currentHP;
    }

    [Header("群體行為")]
    public float separationRadius = 1.5f;
    public float separationForce = 2f;

    protected override void TickBehavior()
    {
        float dist = DistanceToPlayer();

        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            MoveTowards(player.position, dashSpeed);
            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
            return;
        }

        if (dist <= dashTriggerRange && Time.time - lastDashTime > dashCooldown)
        {
            isDashing = true;
            dashTimer = dashDuration;
            lastDashTime = Time.time;
            return;
        }

        if (ShouldChasePlayer())
        {
            // 群體分離邏輯 (Simple Separation)
            Vector2 separation = Vector2.zero;
            Collider2D[] neighbors = Physics2D.OverlapCircleAll(transform.position, separationRadius);
            int count = 0;
            foreach (var col in neighbors)
            {
                if (col.gameObject != gameObject && col.GetComponent<Xiazi>())
                {
                    Vector2 away = (transform.position - col.transform.position);
                    separation += away.normalized / (away.magnitude + 0.1f);
                    count++;
                }
            }

            Vector2 targetPos = (Vector2)player.position;
            if (count > 0)
            {
                separation /= count;
                // 虛擬目標點 = 玩家位置 + 分離偏移
                targetPos += separation * separationForce;
            }
            
            MoveTowards(targetPos, moveSpeed);
        }
        else
        {
            Patrol();
        }
    }
}


