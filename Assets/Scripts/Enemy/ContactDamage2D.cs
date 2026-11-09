using UnityEngine;

// 將此元件掛在任何會對玩家造成貼身傷害的物件上（怪物本體）
// 支援 Trigger 與非 Trigger 碰撞，並具有冷卻時間與圖層過濾
public class ContactDamage2D : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damageToPlayer = 5;
    [SerializeField] private float attackCooldown = 1.0f;
    [Tooltip("若父物件有 Enemy/BaseEnemy2D，優先沿用其 damageToPlayer，避免同一隻怪不同碰撞器傷害不一致。")]
    [SerializeField] private bool preferParentEnemyDamage = true;

    [Header("Filter")]
    [Tooltip("只有這些圖層的對象才會被視為有效目標（通常勾 Player）")]
    [SerializeField] private LayerMask targetLayers;

    [Header("Animation (optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTriggerName = "Attack";

    [Header("麻痺（可選，例如鯊痹魚）")]
    [SerializeField] private float paralyzeDurationOnHit;

    private float lastAttackTime = -999f;

    /// <summary>由 <see cref="ShabiFish"/> 等敵人在 Start 設定；>0 時造成傷害會施加麻痺。</summary>
    public void SetParalyzeOnHit(float seconds) => paralyzeDurationOnHit = Mathf.Max(0f, seconds);

    private void Reset()
    {
        // 預設僅打 Player 圖層
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            targetLayers = 1 << playerLayer;
        }
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private bool IsOnTargetLayer(GameObject other)
    {
        return (targetLayers.value & (1 << other.layer)) != 0;
    }

    private void TryDamage(GameObject other)
    {
        if (Time.time - lastAttackTime <= attackCooldown) return;
        // LayerMask 未設定時不過濾圖層（避免 Prefab 忘記勾選導致完全打不到人）
        if (targetLayers.value != 0 && !IsOnTargetLayer(other)) return;

        // 放寬：子物件也可
        var health = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        int dmg = damageToPlayer;
        if (preferParentEnemyDamage)
        {
            var b = GetComponentInParent<BaseEnemy2D>();
            if (b != null) dmg = b.damageToPlayer;
            else
            {
                var e = GetComponentInParent<Enemy>();
                if (e != null)
                {
                    // Enemy.damageToPlayer 是 private，用反射讀值（只在 prefabs 沒統一時用）
                    var f = typeof(Enemy).GetField("damageToPlayer",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (f != null)
                        dmg = (int)f.GetValue(e);
                }
            }
        }

        health.TakeDamage(dmg);
        lastAttackTime = Time.time;

        if (paralyzeDurationOnHit > 0f)
        {
            var paralyze = other.GetComponent<PlayerParalyze>() ?? other.GetComponentInParent<PlayerParalyze>();
            paralyze?.ApplyParalyze(paralyzeDurationOnHit);
        }

        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
        {
            animator.SetTrigger(attackTriggerName);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.collider.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.collider.gameObject);
    }
}


