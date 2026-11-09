using UnityEngine;

public abstract class BaseEnemy2D : MonoBehaviour
{
    [Header("朝向")]
    [SerializeField] protected float facingAngleOffset = 0f; // 某些美術預設向左，需加180
    [SerializeField] protected float faceFlipEnterThreshold = 0.15f; // 需要超過此水平量才允許翻面
    private int facingSign = 1; // 1:面向右, -1:面向左
    [Header("生命/戰鬥")]
    public int maxHP = 10;
    public int damageToPlayer = 5;
    public float attackCooldown = 1.5f;

    [Header("識別 (任務用)")]
    public string enemyID; // Quest Target Key

    [Header("行為模式")]
    public bool isAggressive = true; // true=主動怪，false=被動怪
    public float aggroDuration = 5f; // 被動怪被攻擊後的追擊時間

    [Header("移動/偵測")]
    public float moveSpeed = 2f;
    public float chaseRange = 6f;
    public float stopDistance = 0.5f;
    public LayerMask obstacleLayer;
    
    [Header("碰撞反應")]
    public float obstacleCheckDistance = 0.5f; // 檢測障礙物的距離
    public float obstacleAvoidanceAngle = 45f; // 避障時嘗試的角度
    public float stuckCheckInterval = 0.5f; // 檢查是否卡住的間隔
    public float stuckThreshold = 0.1f; // 判斷卡住的移動距離閾值

    [Header("巡邏設定")]
    public bool enablePatrol = true; // 是否啟用巡邏
    public float patrolDistance = 3f; // 左右巡邏距離
    public float patrolSpeed = 1.2f; // 巡邏速度倍率
    public float idleTimeMin = 0.5f;
    public float idleTimeMax = 1.5f;
    [Header("巡邏範圍（可選）")]
    public Vector2 patrolCenter; // 巡邏中心點，留空則使用初始位置
    public float patrolRadius = 5f; // 圓形巡邏半徑，0=直線巡邏

    [Header("掉落")]
    public int coinDropAmount = 0;
    public GameObject coinPrefab;
    public MaterialType dropMaterialType = MaterialType.None;
    public int materialDropCount = 0;
    public GameObject materialPrefab;

    [Header("音效")]
    [Tooltip("怪物受傷時播放音效（預設：怪物受傷）。Boss 可關掉改用自己的音效。")]
    [SerializeField] protected bool playHurtSfxOnDamage = true;
    [SerializeField] protected string hurtSfxName = "殺魚";
    [Tooltip("同一隻怪在極短時間內連續被打到時，最少間隔多久才播一次（避免爆音）")]
    [SerializeField] protected float hurtSfxMinIntervalSeconds = 0.12f;
    float _lastHurtSfxTime = -999f;
    [Tooltip("怪物死亡時播放音效（預設：怪物死）。Boss 可覆寫成自己的死亡音效。")]
    [SerializeField] protected bool playDeathSfxOnDie = true;
    [SerializeField] protected string deathSfxName = "殺魚";

    protected Transform player;
    protected Rigidbody2D rb;
    protected Animator animator;
    protected int currentHP;

    public int CurrentHP => currentHP;
    protected SpriteRenderer[] flashRenderers;
    protected Color flashColor = Color.red;
    protected float flashDuration = 0.1f;
    protected float flashInterval = 0.05f;
    protected bool isFlashing;
    protected Vector2 startPos;
    protected Vector2 lastPosition;
    protected float lastStuckCheckTime;
    protected float sineTimeOffset;
    protected bool isAggroed;
    protected float aggroTimer;
    protected float lastAttackTime;
    protected bool isKnockedBack;
    protected float knockbackTimer;
    protected Vector2 knockbackVelocity;
    [Header("擊退設定")] [SerializeField] protected float knockbackResistance = 0f;
    [Header("自然游動")] [SerializeField] protected bool enableOrganicMovement = false;
    [SerializeField] protected float sineFrequency = 1f;
    [SerializeField] protected float sineMagnitude = 0.5f;
    protected bool isStuck;
    protected float stuckTimer;
    protected Vector2 lastMoveDirection;
    protected bool isMoving;
    protected Vector2 currentPatrolTarget;
    protected bool movingRight = true;
    protected bool isIdling;
    protected float idleTimer;
    /// <summary>避免同一幀多次受傷重複執行 Die／掉落。</summary>
    protected bool isDead;

    protected Vector2 BodyPosition => rb != null ? rb.position : (Vector2)transform.position;

    static EnemyHpBalanceAsset LoadBalance()
    {
        return Resources.Load<EnemyHpBalanceAsset>("EnemyHpBalance");
    }

    static EnemyDamageBalanceAsset LoadDamageBalance()
    {
        return Resources.Load<EnemyDamageBalanceAsset>("EnemyDamageBalance");
    }

    int ComputeBalancedMaxHp(EnemyHpBalanceAsset bal)
    {
        if (bal == null) return maxHP;

        // 以 enemyID 為主，未填時退回物件名稱
        string id = !string.IsNullOrWhiteSpace(enemyID)
            ? enemyID
            : gameObject.name.Replace("(Clone)", "").Trim();

        var tier = bal.ResolveTier(id);
        float ttk = bal.GetTtkSeconds(tier);

        // 玩家輸出估算：依 WeaponUpgradeBalance 推導（不讀 Prefab，避免場景依賴）
        int lvl = Mathf.Clamp(PlayerDataManager.WeaponUpgradeLevel, 0, WeaponUpgradeBalance.MaxLevel);
        float dmgMult = WeaponUpgradeBalance.GetDamageMultiplier(lvl);
        int projCount = WeaponUpgradeBalance.GetProjectileCount(lvl);
        float phase = WeaponUpgradeBalance.GetPhaseTimeScale(lvl);

        // PlayerController: max(attackCooldown, attackDuration+reloadDuration) * phaseScale
        // 這裡拿不到實例值，保底用 fallback；若你之後想更準，我可以改成從場景 PlayerController 抓參數。
        float cycle = Mathf.Max(0.2f, bal.fallbackAttackCycleSeconds) * Mathf.Max(0.1f, phase);
        float dps = (bal.baseBulletDamage * dmgMult * projCount) / cycle;

        float hp = dps * ttk * Mathf.Max(0.05f, bal.globalHpMultiplier);
        // 基準：最初至少 10hp（你需求）
        return Mathf.Max(10, Mathf.RoundToInt(hp));
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // 一般怪物：統一受傷/死亡音效為「殺魚」
        // 例外：小BOSS（BossController）會自行指定；大BOSS 不走 BaseEnemy2D。
        if (GetComponent<BossController>() == null)
        {
            playHurtSfxOnDamage = true;
            hurtSfxName = "殺魚";
            playDeathSfxOnDie = true;
            deathSfxName = "殺魚";
        }

        // 在 currentHP 初始化前先套用平衡表（若 Resources/EnemyHpBalance.asset 存在）
        var bal = LoadBalance();
        if (bal != null)
            maxHP = ComputeBalancedMaxHp(bal);

        // 固定玩家受傷數值：若 Resources/EnemyDamageBalance.asset 存在，則以表格覆蓋 damageToPlayer
        var dmgBal = LoadDamageBalance();
        if (dmgBal != null)
        {
            string id = !string.IsNullOrWhiteSpace(enemyID)
                ? enemyID
                : gameObject.name.Replace("(Clone)", "").Trim();
            damageToPlayer = dmgBal.ResolveDamage(id, damageToPlayer);
        }

        currentHP = Mathf.Max(1, maxHP);
        if (flashRenderers == null || flashRenderers.Length == 0)
            flashRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    protected virtual void Start()
    {
        CheckAnimParameters(); // [New] 初始化動畫參數檢測
        
        var p = GameObject.FindGameObjectWithTag("Player");
        player = p ? p.transform : null;
        
        // 設定巡邏中心點
        if (patrolCenter == Vector2.zero)
        {
            patrolCenter = transform.position;
        }
        startPos = patrolCenter;
        SetNextPatrolTarget();
        
        // 初始化碰撞檢測
        lastPosition = rb != null ? rb.position : (Vector2)transform.position;
        lastStuckCheckTime = Time.time;
        
        sineTimeOffset = Random.Range(0f, 100f); // 隨機偏移避免同步

        // 成就／任務：未填 enemyID 時用物件名稱（與 Kill_<名稱>、Quest TargetKey 對齊）
        if (string.IsNullOrWhiteSpace(enemyID))
        {
            enemyID = gameObject.name.Replace("(Clone)", "").Trim();
        }
        
#if UNITY_EDITOR
        Debug.Log($"[EnemyDebug] 怪物 {gameObject.name} 生成 (ID: '{enemyID}')");
#endif
    }

    protected virtual void FixedUpdate()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            player = p ? p.transform : null;
            if (player == null) return;
        }
        TickBehavior();
    }

    // 子類別覆寫：每禎行為邏輯（含移動/攻擊觸發）
    protected abstract void TickBehavior();

    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;
        currentHP -= Mathf.Max(1, amount);

        if (playHurtSfxOnDamage && !string.IsNullOrWhiteSpace(hurtSfxName))
        {
            if (Time.time - _lastHurtSfxTime >= Mathf.Max(0f, hurtSfxMinIntervalSeconds))
            {
                AudioManager.GetInstance()?.PlaySFX(hurtSfxName.Trim());
                _lastHurtSfxTime = Time.time;
            }
        }

        StartDamageFlash();
        
        // 被動怪被攻擊時進入憤怒狀態
        if (!isAggressive)
        {
            isAggroed = true;
            aggroTimer = aggroDuration;
        }
        
        if (currentHP <= 0) Die();
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

#if UNITY_EDITOR
        Debug.Log($"[EnemyDebug] 怪物 {gameObject.name} 死亡 (ID: '{enemyID}')");
#endif

        // 播放怪物死亡音效
        if (playDeathSfxOnDie && !string.IsNullOrWhiteSpace(deathSfxName))
            AudioManager.GetInstance()?.PlaySFX(deathSfxName.Trim());

        if (animator) 
        {
            // 清除其他觸發，確保死亡動畫優先
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Die");
        }

        // 觸發相機震動
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeOnEnemyDeath();
        }

        // [Quest Hook]
        if (!string.IsNullOrEmpty(enemyID))
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.UpdateObjectiveProgress(enemyID, 1);
            }
            else
            {
                Debug.LogWarning("[BaseEnemy2D] 雖然設定了 EnemyID，但場景中找不到 QuestManager！");
            }
        }

        // [Achievement Hook]
        if (AchievementManager.Instance != null)
        {
            // 1. Specific Enemy Kill
            if (!string.IsNullOrEmpty(enemyID))
            {
                AchievementManager.Instance.AddProgress($"Kill_{enemyID}", 1);
            }
            // 2. Total Kill
            AchievementManager.Instance.AddProgress("TotalKills", 1);
        }

        DropCoins();
        DropMaterials();
        // 等待死亡動畫播放完畢再銷毀
        StartCoroutine(DestroyAfterDeathAnimation());
    }

    private System.Collections.IEnumerator DestroyAfterDeathAnimation()
    {
        float waitTime = 0.25f;
        
        if (animator != null)
        {
            // 等待一幀讓 Animator 接收 Trigger 並開始轉換
            yield return null;
            
            // 等待直到進入 Die 狀態 (假設狀態名稱包含 "Die" 或 "Death")
            // 為了安全，最多等待 0.5 秒，避免卡死
            float timeout = 0.5f;
            while (timeout > 0f)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName("Die") || state.IsName("Death") || state.IsTag("Death"))
                {
                    // 成功進入死亡狀態，獲取長度
                    waitTime = state.length;
                    break;
                }
                timeout -= Time.deltaTime;
                yield return null;
            }
        }
        
        yield return new WaitForSeconds(waitTime);
        Destroy(gameObject);
    }

    protected virtual void StartDamageFlash()
    {
        if (isFlashing) return;
        if (flashRenderers == null || flashRenderers.Length == 0) return;
        StartCoroutine(DamageFlashCoroutine());
    }

    private System.Collections.IEnumerator DamageFlashCoroutine()
    {
        isFlashing = true;
        float elapsed = 0f;
        // 記錄原色
        var originals = new Color[flashRenderers.Length];
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null) originals[i] = flashRenderers[i].color;
        }
        while (elapsed < flashDuration)
        {
            // 紅
            for (int i = 0; i < flashRenderers.Length; i++) if (flashRenderers[i]) flashRenderers[i].color = flashColor;
            yield return new WaitForSeconds(flashInterval);
            // 原
            for (int i = 0; i < flashRenderers.Length; i++) if (flashRenderers[i]) flashRenderers[i].color = originals[i];
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval * 2f;
        }
        // 還原
        for (int i = 0; i < flashRenderers.Length; i++) if (flashRenderers[i]) flashRenderers[i].color = originals[i];
        isFlashing = false;
    }

    protected void MoveTowards(Vector2 target, float speed)
    {
        if (rb == null) return;

        // 處理擊退狀態
        if (isKnockedBack)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            rb.velocity = knockbackVelocity; // 使用 velocity 模擬擊退滑行
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
                rb.velocity = Vector2.zero;
            }
            return; // 擊退中無法自主移動
        }
        
        // 檢查是否卡住
        CheckIfStuck();
        
        Vector2 dir = (target - rb.position).normalized;

        // 自然游動 (Organic Movement)
        if (enableOrganicMovement && speed > 0.1f)
        {
            // 計算垂直向量
            Vector2 perpendicular = new Vector2(-dir.y, dir.x);
            float sineWave = Mathf.Sin((Time.time + sineTimeOffset) * sineFrequency) * sineMagnitude;
            
            // 疊加波動 (注意：這會稍微改變實際移動方向，創造游動感)
            dir += perpendicular * sineWave;
            dir.Normalize();
        }
        
        // 檢測前方是否有障礙物
        if (IsObstacleAhead(dir))
        {
            // 嘗試避障
            Vector2 avoidDir = TryAvoidObstacle(dir);
            if (avoidDir != Vector2.zero)
            {
                dir = avoidDir;
            }
            else
            {
                // 無法避障時，反向移動或停止
                dir = -dir;
                isStuck = true;
                stuckTimer = 1f; // 短暫停頓
            }
        }
        
        // 如果卡住，短暫停頓
        if (isStuck && stuckTimer > 0f)
        {
            stuckTimer -= Time.fixedDeltaTime;
            if (stuckTimer <= 0f)
            {
                isStuck = false;
                // 嘗試朝相反方向移動
                dir = -lastMoveDirection;
                if (dir == Vector2.zero) dir = Vector2.left; // 預設方向
            }
            else
            {
                // 停頓時不移動
                dir = Vector2.zero;
            }
        }
        
        if (dir != Vector2.zero)
        {
            lastMoveDirection = dir;
            rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);
            FaceByVector(dir);
        }
        
        // 更新移動動畫
        bool shouldMove = speed > 0.1f && dir != Vector2.zero && !isStuck;
        if (isMoving != shouldMove)
        {
            isMoving = shouldMove;
            if (animator) 
            {
                // [Fix] 安全設置動畫參數，防止 Log 刷屏
                if (hasMoveParam)
                {
                    animator.SetBool(moveAnimParamName, isMoving);
                }
            }
        }
    }

    [Header("動畫參數名稱")]
    [SerializeField] protected string moveAnimParamName = "Move";
    protected bool hasMoveParam;

    // 在 Start 中檢測參數是否存在
    protected void CheckAnimParameters()
    {
        if (animator == null) return;
        hasMoveParam = AnimatorHasParameter(animator, moveAnimParamName, AnimatorControllerParameterType.Bool);
    }
    
    protected bool AnimatorHasParameter(Animator anim, string paramName, AnimatorControllerParameterType type)
    {
        if (anim == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (var p in anim.parameters)
        {
            if (p.type == type && p.name == paramName) return true;
        }
        return false;
    }

    // 公開方法：施加擊退
    public void ApplyKnockback(Vector2 force)
    {
        if (knockbackResistance >= 1f) return;
        
        isKnockedBack = true;
        knockbackTimer = 0.2f; // 擊退持續時間
        
        // 根據抗性減免
        Vector2 finalForce = force * (1f - knockbackResistance);
        
        // 這裡簡單用 velocity 模擬，若要更物理可改用 AddForce
        // 假設 force 是衝量
        if (rb != null)
        {
            // 將力轉換為速度 (F = ma -> a = F/m -> v = a*t) 這裡簡化直接視為速度
            knockbackVelocity = finalForce; 
        }
    }
    
    // 檢查是否卡住（位置幾乎沒有改變）
    protected void CheckIfStuck()
    {
        if (Time.time - lastStuckCheckTime < stuckCheckInterval) return;
        lastStuckCheckTime = Time.time;
        
        Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;
        float movedDistance = Vector2.Distance(currentPos, lastPosition);
        
        // 如果移動距離很小，可能卡住了
        if (movedDistance < stuckThreshold && isMoving)
        {
            if (!isStuck)
            {
                isStuck = true;
                stuckTimer = 0.5f; // 短暫停頓後嘗試改變方向
            }
        }
        
        lastPosition = currentPos;
    }
    
    // 檢測前方是否有障礙物
    protected bool IsObstacleAhead(Vector2 direction)
    {
        if (obstacleLayer.value == 0) return false; // 未設置障礙物層
        
        Vector2 checkOrigin = rb != null ? rb.position : (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(checkOrigin, direction, obstacleCheckDistance, obstacleLayer);
        
        // 如果有碰撞器，也檢查碰撞器的尺寸
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            float checkDist = obstacleCheckDistance;
            if (col is CircleCollider2D)
            {
                checkDist += ((CircleCollider2D)col).radius;
            }
            else if (col is BoxCollider2D)
            {
                checkDist += ((BoxCollider2D)col).size.magnitude * 0.5f;
            }
            hit = Physics2D.Raycast(checkOrigin, direction, checkDist, obstacleLayer);
        }
        
        return hit.collider != null;
    }
    
    // 嘗試避開障礙物
    protected Vector2 TryAvoidObstacle(Vector2 originalDir)
    {
        // 嘗試幾個角度
        float[] angles = { obstacleAvoidanceAngle, -obstacleAvoidanceAngle, 
                           obstacleAvoidanceAngle * 2f, -obstacleAvoidanceAngle * 2f, 
                           90f, -90f };
        
        foreach (float angle in angles)
        {
            Vector2 testDir = Quaternion.Euler(0, 0, angle) * originalDir;
            if (!IsObstacleAhead(testDir))
            {
                return testDir.normalized;
            }
        }
        
        // 所有方向都有障礙物，返回反向
        return -originalDir;
    }

    // 依移動向量更新面向方向（僅左右翻面，不旋轉，避免上下翻轉）
    protected void FaceByVector(Vector2 move)
    {
        if (move.sqrMagnitude < 0.0001f) return;
        // 僅根據水平分量決定左右，避免任何上下翻轉
        float x = move.x;
        if (Mathf.Abs(x) < faceFlipEnterThreshold) return; // 避免微小擾動導致抖動翻面

        // facingAngleOffset 用來處理預設素材方向為左或右（180 視為反向）
        int sign = x > 0f ? 1 : -1;
        if (Mathf.Abs(Mathf.DeltaAngle(facingAngleOffset, 180f)) < 1f)
        {
            sign *= -1; // 需要反向
        }

        if (sign == facingSign) return; // 未改變象限則不重設，減少閃爍
        facingSign = sign;

        Vector3 s = transform.localScale;
        float baseAbs = Mathf.Abs(s.x) > 0.0001f ? Mathf.Abs(s.x) : 1f;
        s.x = baseAbs * facingSign;
        transform.localScale = s;
        // 不變更 rotation
    }

    // 設定下一個巡邏目標點
    protected void SetNextPatrolTarget()
    {
        if (patrolRadius > 0f)
        {
            // 圓形巡邏：在圓周上隨機選擇點
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            currentPatrolTarget = patrolCenter + new Vector2(
                Mathf.Cos(angle) * patrolRadius,
                Mathf.Sin(angle) * patrolRadius
            );
        }
        else
        {
            // 直線巡邏：左右移動
            float targetX = startPos.x + (movingRight ? patrolDistance : -patrolDistance);
            currentPatrolTarget = new Vector2(targetX, BodyPosition.y);
        }
    }

    // 基礎巡邏行為
    protected void Patrol()
    {
        if (!enablePatrol) return;

        if (isIdling)
        {
            idleTimer -= Time.fixedDeltaTime;
            if (idleTimer <= 0) 
            {
                isIdling = false;
                SetNextPatrolTarget();
            }
            return;
        }

        // 檢查前方是否有障礙物（巡邏時）
        Vector2 dirToTarget = (currentPatrolTarget - BodyPosition).normalized;
        if (IsObstacleAhead(dirToTarget))
        {
            // 遇到障礙物時改變方向
            if (patrolRadius > 0f)
            {
                // 圓形巡邏：設定新目標
                SetNextPatrolTarget();
            }
            else
            {
                // 直線巡邏：轉向並停頓
                movingRight = !movingRight;
                isIdling = true;
                idleTimer = Random.Range(idleTimeMin, idleTimeMax);
                SetNextPatrolTarget(); // 更新目標點
            }
            return;
        }

        // 移動到巡邏目標點
        MoveTowards(currentPatrolTarget, moveSpeed * patrolSpeed);

        // 檢查是否到達目標點
        float distanceToTarget = Vector2.Distance(BodyPosition, currentPatrolTarget);
        if (distanceToTarget < 0.5f)
        {
            if (patrolRadius > 0f)
            {
                // 圓形巡邏：直接設定新目標
                SetNextPatrolTarget();
            }
            else
            {
                // 直線巡邏：轉向並停頓
                movingRight = !movingRight;
                isIdling = true;
                idleTimer = Random.Range(idleTimeMin, idleTimeMax);
            }
        }
    }

    // 檢查是否應該追擊玩家
    protected bool ShouldChasePlayer()
    {
        if (player == null) return false;
        
        float dist = DistanceToPlayer();
        
        // 主動怪：在追擊範圍內就追擊
        if (isAggressive)
        {
            return dist <= chaseRange && dist > stopDistance;
        }
        // 被動怪：只有在憤怒狀態下才追擊
        else
        {
            if (isAggroed)
            {
                aggroTimer -= Time.fixedDeltaTime;
                if (aggroTimer <= 0f)
                {
                    isAggroed = false; // 憤怒時間結束
                }
                return dist <= chaseRange && dist > stopDistance;
            }
            return false;
        }
    }

    protected float DistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector2.Distance(transform.position, player.position);
    }

    protected bool CanAttack()
    {
        return Time.time - lastAttackTime > attackCooldown;
    }

    protected bool DoContactAttack(Collider2D other)
    {
        if (!CanAttack()) return false;
        // 放寬條件：只要對方或其父物件有 PlayerHealth 就視為玩家
        var health = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (health == null) return false;
        health.TakeDamage(damageToPlayer);
        lastAttackTime = Time.time;
        return true;
    }

    protected virtual void DropCoins()
    {
        if (coinPrefab == null || coinDropAmount <= 0) return;
        var coin = Instantiate(coinPrefab, transform.position, Quaternion.identity);
        var pickup = coin.GetComponent<CoinPickup>();
        if (pickup != null) pickup.SetAmount(coinDropAmount);
        
        // [Physics] 噴發效果
        var rb = coin.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            randomDir.y = Mathf.Abs(randomDir.y);
            rb.AddForce(randomDir * Random.Range(3f, 5f), ForceMode2D.Impulse);
        }
    }

    protected virtual void DropMaterials()
    {
        if (materialPrefab == null || dropMaterialType == MaterialType.None || materialDropCount <= 0) return;
        for (int i = 0; i < materialDropCount; i++)
        {
            var mat = Instantiate(materialPrefab, transform.position, Quaternion.identity);
            var pickup = mat.GetComponent<MaterialPickup>();
            if (pickup != null) pickup.SetMaterial(dropMaterialType, 1);
            
            // [Physics] 噴發效果
            var rb = mat.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 randomDir = Random.insideUnitCircle.normalized;
                randomDir.y = Mathf.Abs(randomDir.y);
                rb.AddForce(randomDir * Random.Range(3f, 5f), ForceMode2D.Impulse);
            }
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        bool attacked = DoContactAttack(other);
        // 只有實際造成攻擊時才播放動畫
        if (attacked && animator)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("Atk")) animator.SetTrigger("Attack");
        }
    }

    // 與玩家持續重疊時也能造成週期性傷害（受攻擊冷卻控制）
    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        bool attacked = DoContactAttack(other);
        if (attacked && animator)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("Atk")) animator.SetTrigger("Attack");
        }
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        var other = collision.collider;
        
        // 檢查是否是障礙物（不是玩家）
        var playerHealth = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            // 這是障礙物，觸發避障反應
            isStuck = true;
            stuckTimer = 0.3f; // 初次碰撞時較短的停頓
            
            // 根據碰撞法線計算避開方向
            if (collision.contacts.Length > 0)
            {
                Vector2 contactNormal = collision.contacts[0].normal;
                // 計算垂直於碰撞法線的方向（避免沿著牆壁移動）
                Vector2 avoidDir = new Vector2(-contactNormal.y, contactNormal.x);
                
                // 選擇遠離碰撞點的方向
                Vector2 toSelf = ((Vector2)transform.position - collision.contacts[0].point).normalized;
                if (Vector2.Dot(avoidDir, toSelf) < 0)
                {
                    avoidDir = -avoidDir;
                }
                
                if (avoidDir != Vector2.zero)
                {
                    lastMoveDirection = avoidDir.normalized;
                }
            }
            
            return; // 不處理玩家傷害
        }
        
        // 處理玩家傷害（若使用非 Trigger 的碰撞也能造成接觸傷害）
        bool attacked = DoContactAttack(other);
        if (attacked && animator)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("Atk")) animator.SetTrigger("Attack");
        }
    }

    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        var other = collision.collider;
        
        // 檢查是否是障礙物（不是玩家）
        var playerHealth = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            // 這是障礙物，觸發避障反應
            isStuck = true;
            stuckTimer = 0.5f; // 短暫停頓
            
            // 嘗試改變方向
            Vector2 avoidDir = Vector2.zero;
            if (collision.contacts.Length > 0)
            {
                // 根據碰撞接觸點計算避開方向
                Vector2 contactNormal = collision.contacts[0].normal;
                avoidDir = Vector2.Perpendicular(contactNormal);
                
                // 選擇遠離碰撞點的方向
                Vector2 toSelf = ((Vector2)transform.position - collision.contacts[0].point).normalized;
                if (Vector2.Dot(avoidDir, toSelf) < 0)
                {
                    avoidDir = -avoidDir;
                }
            }
            
            if (avoidDir != Vector2.zero)
            {
                lastMoveDirection = avoidDir;
            }
            
            return; // 不處理玩家傷害
        }
        
        // 處理玩家傷害
        bool attacked = DoContactAttack(other);
        if (attacked && animator)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("Atk")) animator.SetTrigger("Attack");
        }
    }

    // 在 Scene 視窗中顯示巡邏範圍（僅編輯器）
    void OnDrawGizmosSelected()
    {
        if (!enablePatrol) return;

        Vector2 center = patrolCenter != Vector2.zero ? patrolCenter : transform.position;
        
        if (patrolRadius > 0f)
        {
            // 顯示圓形巡邏範圍
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, patrolRadius);
        }
        else
        {
            // 顯示直線巡邏範圍
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                center + Vector2.left * patrolDistance,
                center + Vector2.right * patrolDistance
            );
        }

        // 顯示追擊範圍
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}



