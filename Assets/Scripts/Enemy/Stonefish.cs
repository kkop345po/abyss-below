using UnityEngine;

// 🐡 砂鍋魚（中型/主動）：坦型，咬合+衝撞，擋路
public class Stonefish : BaseEnemy2D
{
    [Header("撞擊")]
    public float blockSpeed = 1.2f;
    public float rushSpeed = 6f;
    public float rushRange = 5f;

    private bool rushing = false;
    private float rushTimer = 0f;

    protected override void Awake()
    {
        base.Awake();
        isAggressive = true; // 主動怪
        facingAngleOffset = 180f; // 反向修正
    }

    [Header("膨脹防禦")]
    public float inflateDuration = 3f;
    public float inflateScaleMultiplier = 1.5f;
    private bool isInflated = false;
    private float inflateTimer = 0f;
    private Vector3 originalScale;

    protected override void Start()
    {
        base.Start();
        originalScale = transform.localScale;
    }

    public override void TakeDamage(int amount)
    {
        if (isInflated)
        {
            // 膨脹時減傷 50%
            amount = Mathf.CeilToInt(amount * 0.5f);
            // 播放金屬音效或反彈特效?
        }
        
        base.TakeDamage(amount);
        
        // 受傷有機率觸發膨脹
        if (!isInflated && Random.value < 0.3f)
        {
            StartInflate();
        }
    }

    private void StartInflate()
    {
        isInflated = true;
        inflateTimer = inflateDuration;
        transform.localScale = originalScale * inflateScaleMultiplier;
        knockbackResistance = 1f; // 霸體
        
        // 稍微回血?
        // currentHP += 1;
    }

    private void EndInflate()
    {
        isInflated = false;
        transform.localScale = originalScale;
        knockbackResistance = 0f; // 恢復
    }

    protected override void TickBehavior()
    {
        // 膨脹狀態處理
        if (isInflated)
        {
            inflateTimer -= Time.fixedDeltaTime;
            rb.velocity = Vector2.zero; // 停止移動
            if (inflateTimer <= 0f)
            {
                EndInflate();
            }
            return; // 膨脹時不做其他事
        }

        float dist = DistanceToPlayer();

        // 玩家太近強制膨脹
        if (dist < 1.5f && !isInflated && CanAttack())
        {
            StartInflate();
            lastAttackTime = Time.time; // 視為一種攻擊/防禦動作
            return;
        }

        if (rushing)
        {
            rushTimer -= Time.fixedDeltaTime;
            MoveTowards(player.position, rushSpeed);
            if (rushTimer <= 0f) rushing = false;
            return;
        }

        if (dist <= rushRange && CanAttack())
        {
            // 衝刺前兆：稍微後退
            // 這裡可以加一個簡單的預備動作，暫時直接衝
            rushing = true;
            rushTimer = 0.5f;
            lastAttackTime = Time.time;
            return;
        }

        // 平時像路障慢慢靠近
        if (dist <= chaseRange)
        {
            MoveTowards(player.position, blockSpeed);
        }
        else
        {
            Patrol();
        }
    }
}


