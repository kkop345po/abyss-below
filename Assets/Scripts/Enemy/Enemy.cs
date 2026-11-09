using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("朝向")]
    [SerializeField] private float facingAngleOffset = 0f;
    [SerializeField] private float faceFlipEnterThreshold = 0.15f;
    private int facingSign = 1;
    [Header("Enemy Settings")]
    [SerializeField] private int totalBodyParts = 4;
    [SerializeField] private int damageToPlayer = 10;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int coinDropAmount = 10;
    [SerializeField] private GameObject coinPrefab;

    [Header("Effects")]
    [SerializeField] private AudioClip deathSoundClip;
    [SerializeField] private GameObject deathEffect;

    [Header("音效")]
    [Tooltip("部位受傷時播放音效（預設：怪物受傷）。")]
    [SerializeField] private bool playHurtSfxOnDamage = true;
    [SerializeField] private string hurtSfxName = "殺魚";
    [Tooltip("同一隻怪在極短時間內連續被打到時，最少間隔多久才播一次（避免爆音）")]
    [SerializeField] private float hurtSfxMinIntervalSeconds = 0.12f;
    private float _lastHurtSfxTime = -999f;
    [Tooltip("怪物死亡時播放音效（預設：怪物死）。")]
    [SerializeField] private bool playDeathSfxOnDie = true;
    [SerializeField] private string deathSfxName = "殺魚";

    [Header("Chase Settings")]
    [SerializeField] private float chaseRange = 6f;
    [SerializeField] private float chaseSpeed = 2.5f;
    [SerializeField] private float stopDistance = 0.5f;

    [Header("Swim Settings")]
    [SerializeField] private float swimSpeed = 1.5f;
    [SerializeField] private float swimDistance = 3f;
    [SerializeField] private float verticalAmplitude = 0.5f;
    [SerializeField] private float swimFrequency = 1f;
    [SerializeField] private float idleTimeMin = 1f;
    [SerializeField] private float idleTimeMax = 3f;
    [SerializeField] private LayerMask wallLayer;

    [Header("素材掉落設定")]
    public MaterialType dropMaterialType;
    public int baseDropAmount = 1;
    public GameObject materialPrefab;

    [Header("掉落控制")]
    [SerializeField] private bool dropDirectLoot = false;

    private int damagedBodyParts = 0;
    private bool isDead;
    private Transform player;
    private Rigidbody2D rb;
    private AudioSource audioSource;
    private Animator animator;
    [Header("受傷閃爍")]
    [SerializeField] private SpriteRenderer[] flashRenderers;
    [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float flashInterval = 0.08f;
    [SerializeField] private float flashDuration = 0.4f;
    private bool isFlashing = false;
    private Vector2 startPos;
    private bool movingRight = true;
    private float swimTimer;
    private float idleTimer = 0f;
    private bool isIdling = false;
    private float lastAttackTime = -999f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        animator = GetComponent<Animator>();
        startPos = transform.position;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (flashRenderers == null || flashRenderers.Length == 0)
            flashRenderers = GetComponentsInChildren<SpriteRenderer>();

        var bodyParts = GetComponentsInChildren<Hurtbox>();
        if (bodyParts.Length != totalBodyParts)
        {
            Debug.LogWarning($"預期有 {totalBodyParts} 個部位，但找到 {bodyParts.Length} 個");
        }

        // 一般怪物：統一受傷/死亡音效為「殺魚」
        playHurtSfxOnDamage = true;
        hurtSfxName = "殺魚";
        playDeathSfxOnDie = true;
        deathSfxName = "殺魚";

        // 固定玩家受傷數值：若 Resources/EnemyDamageBalance.asset 存在，則以表格覆蓋 damageToPlayer
        var dmgBal = Resources.Load<EnemyDamageBalanceAsset>("EnemyDamageBalance");
        if (dmgBal != null)
        {
            string id = gameObject.name.Replace("(Clone)", "").Trim();
            damageToPlayer = dmgBal.ResolveDamage(id, damageToPlayer);
        }
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;
        }

        if (rb == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= chaseRange && distanceToPlayer > stopDistance)
        {
            if (animator) animator.SetBool("Chase", true);
            ChasePlayer();
        }
        else
        {
            if (animator) animator.SetBool("Chase", false);
            SwimIdle();
        }
    }

    private void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.MovePosition(rb.position + direction * chaseSpeed * Time.fixedDeltaTime);
        FaceByVector(direction);
    }

    private void SwimIdle()
    {
        if (isIdling)
        {
            idleTimer -= Time.fixedDeltaTime;
            if (idleTimer <= 0) isIdling = false;
            return;
        }

        swimTimer += Time.fixedDeltaTime;

        float horizontal = movingRight ? 1 : -1;
        float vertical = Mathf.Sin(swimTimer * swimFrequency) * verticalAmplitude;
        Vector2 swimDir = new Vector2(horizontal, vertical).normalized;

        Vector2 nextPos = rb.position + swimDir * swimSpeed * Time.fixedDeltaTime;
        rb.MovePosition(nextPos);
        FaceByVector(swimDir);

        // 撞牆轉向
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position, Vector2.right * (movingRight ? 1 : -1), 0.5f, wallLayer);
        if (wallHit.collider != null || Mathf.Abs(rb.position.x - startPos.x) >= swimDistance)
        {
            movingRight = !movingRight;
            isIdling = true;
            idleTimer = Random.Range(idleTimeMin, idleTimeMax);
        }
    }

    // 依移動向量更新面向方向（僅左右翻面，不旋轉，避免上下翻轉）
    private void FaceByVector(Vector2 move)
    {
        if (move.sqrMagnitude < 0.0001f) return;
        float x = move.x;
        if (Mathf.Abs(x) < faceFlipEnterThreshold) return;

        int sign = x > 0f ? 1 : -1;
        if (Mathf.Abs(Mathf.DeltaAngle(facingAngleOffset, 180f)) < 1f)
        {
            sign *= -1;
        }
        if (sign == facingSign) return;
        facingSign = sign;
        Vector3 s = transform.localScale;
        float baseAbs = Mathf.Abs(s.x) > 0.0001f ? Mathf.Abs(s.x) : 1f;
        s.x = baseAbs * facingSign;
        transform.localScale = s;
        // 不變更 rotation
    }

    public void OnBodyPartDamaged()
    {
        if (isDead) return;
        damagedBodyParts++;

        if (playHurtSfxOnDamage && !string.IsNullOrWhiteSpace(hurtSfxName))
        {
            if (Time.time - _lastHurtSfxTime >= Mathf.Max(0f, hurtSfxMinIntervalSeconds))
            {
                AudioManager.GetInstance()?.PlaySFX(hurtSfxName.Trim());
                _lastHurtSfxTime = Time.time;
            }
        }

        StartDamageFlash();
        if (damagedBodyParts >= totalBodyParts) Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (playDeathSfxOnDie && !string.IsNullOrWhiteSpace(deathSfxName))
            AudioManager.GetInstance()?.PlaySFX(deathSfxName.Trim());

        if (deathSoundClip != null)
            AudioSource.PlayClipAtPoint(deathSoundClip, transform.position);
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        if (animator) animator.SetTrigger("Die");

        // 觸發相機震動
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeOnEnemyDeath();
        }

        if (dropDirectLoot)
        {
            DropCoins();
            DropMaterial();
        }
        
        // 廣播死亡事件
        var deathEvents = GetComponent<EnemyDeathEvents>();
        if (deathEvents != null)
        {
            deathEvents.InvokeDied();
        }

        // 若為 BOSS 並掛有掉落器，生成寶箱
        var bossDropper = GetComponent<BossChestDropper>();
        if (bossDropper != null)
        {
            bossDropper.DropChest();
        }
        StartCoroutine(DestroyAfterDeathAnimation());
    }

    private System.Collections.IEnumerator DestroyAfterDeathAnimation()
    {
        float waitTime = 0.25f;
        if (animator != null)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.length > 0f)
            {
                waitTime = Mathf.Max(waitTime, state.length - state.normalizedTime * state.length);
            }
        }
        yield return new WaitForSeconds(waitTime);
        Destroy(gameObject);
    }

    private void StartDamageFlash()
    {
        if (isFlashing) return;
        if (flashRenderers == null || flashRenderers.Length == 0) return;
        StartCoroutine(DamageFlashCoroutine());
    }

    private System.Collections.IEnumerator DamageFlashCoroutine()
    {
        isFlashing = true;
        float elapsed = 0f;
        var originals = new Color[flashRenderers.Length];
        for (int i = 0; i < flashRenderers.Length; i++) if (flashRenderers[i]) originals[i] = flashRenderers[i].color;
        while (elapsed < flashDuration)
        {
            for (int i = 0; i < flashRenderers.Length; i++) if (flashRenderers[i]) flashRenderers[i].color = flashColor;
            yield return new WaitForSeconds(flashInterval);
            for (int i = 0; i < flashRenderers.Length; i++) if (flashRenderers[i]) flashRenderers[i].color = originals[i];
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval * 2f;
        }
        for (int i = 0; i < flashRenderers.Length; i++) if (flashRenderers[i]) flashRenderers[i].color = originals[i];
        isFlashing = false;
    }

    private void DropCoins()
    {
        if (coinPrefab != null)
        {
            GameObject coin = Instantiate(coinPrefab, transform.position, Quaternion.identity);
            CoinPickup pickup = coin.GetComponent<CoinPickup>();
            if (pickup != null) pickup.SetAmount(coinDropAmount);
        }
    }

    private void DropMaterial()
    {
        if (materialPrefab != null && dropMaterialType != MaterialType.None)
        {
            int amount = GetDropAmountByDepth();
            for (int i = 0; i < amount; i++)
            {
                GameObject mat = Instantiate(materialPrefab, transform.position, Quaternion.identity);
                MaterialPickup pickup = mat.GetComponent<MaterialPickup>();
                if (pickup != null)
                {
                    pickup.SetMaterial(dropMaterialType, 1); // 每個素材物件1個
                }
            }
        }
    }

    private int GetDropAmountByDepth()
    {
        float depth = Mathf.Abs(transform.position.y); // y 越小越深
        if (depth > 50) return baseDropAmount * 3;
        if (depth > 20) return baseDropAmount * 2;
        return baseDropAmount;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time - lastAttackTime <= attackCooldown) return;
        var playerHealth = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;
        playerHealth.TakeDamage(damageToPlayer);
        lastAttackTime = Time.time;
        if (animator) animator.SetTrigger("Attack");
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time - lastAttackTime <= attackCooldown) return;
        var playerHealth = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;
        playerHealth.TakeDamage(damageToPlayer);
        lastAttackTime = Time.time;
        if (animator) animator.SetTrigger("Attack");
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time - lastAttackTime <= attackCooldown) return;
        var other = collision.collider;
        var playerHealth = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;
        playerHealth.TakeDamage(damageToPlayer);
        lastAttackTime = Time.time;
        if (animator) animator.SetTrigger("Attack");
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time - lastAttackTime <= attackCooldown) return;
        var other = collision.collider;
        var playerHealth = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;
        playerHealth.TakeDamage(damageToPlayer);
        lastAttackTime = Time.time;
        if (animator) animator.SetTrigger("Attack");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position + Vector3.left * swimDistance, transform.position + Vector3.right * swimDistance);
        Gizmos.DrawRay(transform.position, Vector2.right * 0.5f);
        Gizmos.DrawRay(transform.position, Vector2.left * 0.5f);
    }
}
