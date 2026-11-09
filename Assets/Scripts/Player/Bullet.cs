using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Tooltip("子彈存在的時間(秒)")]
    public float lifetime = 3f;
    [Tooltip("子彈傷害值（預製體預設；可由武器升級覆寫）")]
    public int damage = 1;
    [Tooltip("碰撞特效預製體")]
    public GameObject hitEffect;
    [Tooltip("子彈移動速度")]
    public float speed = 20f;

    [Header("武器升級：穿透")]
    [Tooltip("尚可額外貫穿的敵人次數（Lv3：1）")]
    public int pierceEnemiesRemaining;
    [Tooltip("貫穿後下一命中傷害 = 當前傷害 × 此值")]
    [Range(0.05f, 1f)]
    public float pierceDamageFactorAfterHit = WeaponUpgradeBalance.PierceDamageFactorAfterHit;

    private void Awake()
    {
        gameObject.tag = "PlayerAttack";

        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.5f, 0.2f);
        }

        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    /// <summary>依預製體基礎傷害 × 升級倍率寫入 <see cref="damage"/>。</summary>
    public void ApplyWeaponDamageFromUpgrade(int prefabBaseDamage, float levelDamageMultiplier)
    {
        damage = Mathf.Max(1, Mathf.RoundToInt(prefabBaseDamage * levelDamageMultiplier));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;

        int otherLayer = other.gameObject.layer;
        int groundLayer = LayerMask.NameToLayer("Ground");

        if (otherLayer == groundLayer)
        {
            SpawnHitFx();
            Destroy(gameObject);
            return;
        }

        if (otherLayer == LayerMask.NameToLayer("Enemy") ||
            otherLayer == LayerMask.NameToLayer("EnemyPart"))
        {
            ProcessEnemyOrPartHit(other);
        }
    }

    void ProcessEnemyOrPartHit(Collider2D other)
    {
        Hurtbox hurtbox = other.GetComponent<Hurtbox>();
        if (hurtbox != null)
        {
            hurtbox.TakeDamage();
        }
        else
        {
            NormalEnemy normal = other.GetComponent<NormalEnemy>();
            if (normal == null)
                normal = other.GetComponentInParent<NormalEnemy>();
            if (normal != null)
            {
                normal.TakeDamage(damage);
            }
            else
            {
                var baseEnemy = other.GetComponentInParent<BaseEnemy2D>();
                if (baseEnemy != null)
                {
                    baseEnemy.TakeDamage(damage);
                    Vector2 knockbackDir = Vector2.right;
                    if (GetComponent<Rigidbody2D>() != null)
                        knockbackDir = GetComponent<Rigidbody2D>().velocity.normalized;
                    if (knockbackDir == Vector2.zero) knockbackDir = transform.right;
                    baseEnemy.ApplyKnockback(knockbackDir * 5f);
                }
                else
                {
                    var enemyParts = other.GetComponentInParent<Enemy>();
                    if (enemyParts != null)
                    {
                        enemyParts.OnBodyPartDamaged();
                    }
                    else
                    {
                        var golem = other.GetComponent<BigBoss_AncientGolem>();
                        if (golem == null) golem = other.GetComponentInParent<BigBoss_AncientGolem>();
                        if (golem != null)
                            golem.TakeDamage(damage);
                    }
                }
            }
        }

        SpawnHitFx();

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int partLayer = LayerMask.NameToLayer("EnemyPart");
        int ol = other.gameObject.layer;
        if (pierceEnemiesRemaining > 0 && (ol == enemyLayer || ol == partLayer))
        {
            pierceEnemiesRemaining--;
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * pierceDamageFactorAfterHit));
            return;
        }

        Destroy(gameObject);
    }

    void SpawnHitFx()
    {
        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, Quaternion.identity);
    }
}
