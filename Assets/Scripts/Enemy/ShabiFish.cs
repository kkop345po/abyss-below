using UnityEngine;

// 💈 鯊痹魚（大型/主動）：高血高攻，突進並施加麻痺
public class ShabiFish : BaseEnemy2D
{
    [Header("突進/麻痺")]
    public float dashSpeed = 9f;
    public float dashRange = 7f;
    public float dashDuration = 0.35f;
    public float paralyzeDuration = 1.2f;
    [Tooltip("玩家身上 PlayerParalyze.paralyzeImmunityCooldown 的建議值；Start 時會寫入玩家元件。")]
    public float paralyzeImmunityCooldown = 3.5f;

    private bool dashing = false;
    private float dashTimer = 0f;

    protected override void Awake()
    {
        base.Awake();
        isAggressive = true; // 主動怪
        facingAngleOffset = 180f; // 反向修正
    }

    private float originalDashCooldown;
    private float originalMoveSpeed;
    private PlayerParalyze playerParalyze;

    protected override void Start()
    {
        base.Start();
        originalDashCooldown = dashDuration + 1f; // 假設冷卻大概是這樣，或需新增欄位
        originalMoveSpeed = moveSpeed;
        if (player != null)
        {
            playerParalyze = player.GetComponent<PlayerParalyze>() ?? player.GetComponentInParent<PlayerParalyze>();
            if (playerParalyze != null && paralyzeImmunityCooldown > 0f)
                playerParalyze.paralyzeImmunityCooldown = paralyzeImmunityCooldown;
        }

        // 鯊魚本體為非 Trigger 碰撞器，實際傷害多由 ContactDamage2D 處理；一併掛上麻痺
        var contact = GetComponent<ContactDamage2D>();
        if (contact != null)
            contact.SetParalyzeOnHit(paralyzeDuration);
    }

    void TryApplyParalyze(Collider2D other)
    {
        if (paralyzeDuration <= 0f || other == null) return;
        var debuff = other.GetComponent<PlayerParalyze>() ?? other.GetComponentInParent<PlayerParalyze>();
        if (debuff != null)
            debuff.ApplyParalyze(paralyzeDuration);
    }

    protected override void TickBehavior()
    {
        float dist = DistanceToPlayer();
        
        // 獵殺模式：檢測玩家是否麻痺
        if (playerParalyze != null && playerParalyze.isParalyzed)
        {
            moveSpeed = originalMoveSpeed * 1.5f;
            // 這裡簡單處理，若要更嚴謹應該控制 attackCooldown
        }
        else
        {
            moveSpeed = originalMoveSpeed;
        }

        if (dashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            MoveTowards(player.position, dashSpeed);
            if (dashTimer <= 0f) dashing = false;
            return;
        }

        if (dist <= dashRange && CanAttack())
        {
            dashing = true;
            dashTimer = dashDuration;
            lastAttackTime = Time.time;
        }
        else if (dist <= chaseRange)
        {
            MoveTowards(player.position, moveSpeed);
        }
        else
        {
            Patrol();
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (!dashing || !other.CompareTag("Player")) return;
        if (DoContactAttack(other))
            TryApplyParalyze(other);
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        bool wasDashing = dashing;
        var other = collision.collider;
        bool isPlayer = (other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>()) != null;

        base.OnCollisionEnter2D(collision);

        if (wasDashing && isPlayer)
            TryApplyParalyze(other);
    }

    protected override void OnCollisionStay2D(Collision2D collision)
    {
        bool wasDashing = dashing;
        var other = collision.collider;
        bool isPlayer = (other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>()) != null;

        base.OnCollisionStay2D(collision);

        if (wasDashing && isPlayer)
            TryApplyParalyze(other);
    }
}


