using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BigBoss_AncientGolem : MonoBehaviour
{
    [Header("識別 (任務/存檔用)")]
    [Tooltip("任務目標 Key（QuestObjective.TargetKey）。建議與 bossID 相同。")]
    public string enemyID = "Enlik";
    [Tooltip("唯一 ID 用於存檔（PlayerPrefs: <bossID>_Dead）。建議與 enemyID 相同。")]
    public string bossID = "Enlik";

    [Header("Health Settings")]
    public int maxHealth = 1000;
    [SerializeField] private int currentHealth;

    [Header("Rock Slide Attack")]
    [Tooltip("Small Rock Prefab (70% chance)")]
    public GameObject smallRockPrefab;
    [Tooltip("Big Rock Prefab (30% chance)")]
    public GameObject bigRockPrefab;
    
    [Header("Spawn Area Settings")]
    [Tooltip("Size of the spawn area (Width, Height)")]
    public Vector2 spawnAreaSize = new Vector2(16f, 1f);
    [Tooltip("Offset from the Boss center")]
    public Vector2 spawnAreaOffset = new Vector2(0f, 5f);
    [Tooltip("Gravity Scale for Rocks (Must be > 0 to fall in water)")]
    public float rockGravityScale = 1f;

    [Header("Water Bomb Attack")]
    public GameObject waterBombPrefab;
    [Tooltip("手動設定發射點。若有設定，將在這些點上生成水彈(不限數量)，並且依據發射點的紅色箭頭(Right)方向發射；若保持空白，會退回原先在相機左側隨機生成的機制")]
    public Transform[] waterBombSpawnPoints;
    [Tooltip("若上方沒設定發射點，預設隨機產生的數量")]
    public int waterBombCount = 12;

    [Header("Summon Minion Settings")]
    public GameObject[] minionPrefabs;
    public float summonInterval = 20f;
    private float summonTimer;

    [Header("Summon Minion Spawn Area (可視化可調)")]
    [Tooltip("若有指定生成點，會優先使用這些點（循環使用）；否則使用下方的矩形範圍隨機生成。")]
    public Transform[] minionSpawnPoints;
    [Tooltip("小怪生成範圍 (寬, 高)")]
    public Vector2 minionSpawnAreaSize = new Vector2(10f, 6f);
    [Tooltip("小怪生成範圍中心相對於 Boss 的偏移")]
    public Vector2 minionSpawnAreaOffset = new Vector2(0f, 0f);
    [Tooltip("在隨機範圍內生成時，離 Boss 的最小距離（避免直接疊在身上）")]
    public float minionMinDistanceFromBoss = 1.5f;
    [Tooltip("Scene 視窗顯示小怪生成範圍")]
    public bool showMinionSpawnGizmos = true;

    [Header("Weakness Settings")]
    public float weakDuration = 10f;
    [Tooltip("虛弱起身（WeakEnd）後額外安全時間（秒）")]
    public float postWeakNoAttackDuration = 2f;
    [Tooltip("等待 WeakEnd 動畫播放完成的最長秒數；超時會強制切回待機避免卡住")]
    public float weakEndMaxWait = 1.5f;
    [Tooltip("Animator 中的待機狀態名；WeakEnd 超時時會強制切回此狀態")]
    public string idleStateName = "idle";
    [Tooltip("Animator 中的虛弱起身狀態名")]
    public string weakEndStateName = "WeakEnd";
    private bool hasTriggeredWeak1 = false;
    private bool hasTriggeredWeak2 = false;
    private bool hasTriggeredWeak3 = false;
    private bool isWeak = false;
    private bool isWeakRecovering = false;

    [Header("Visuals")]
    public Animator animator;
    public SpriteRenderer[] renderers; // For optional flashing effects or control
    [Tooltip("Color to flash when taking damage")]
    public Color flashColor = Color.white; 
    [Tooltip("Duration of the damage flash")]
    public float flashDuration = 0.1f;
    private Coroutine flashCoroutine;

    [Header("音效")]
    [Tooltip("Boss 受傷時播放音效（預設：怪物受傷）。")]
    public bool playHurtSfxOnDamage = true;
    public string hurtSfxName = "章魚受傷";
    [Tooltip("同一隻 Boss 極短時間內連續被打到時，最少間隔多久才播一次（避免爆音）")]
    public float hurtSfxMinIntervalSeconds = 0.12f;
    private float _lastHurtSfxTime = -999f;

    [Tooltip("Boss 死亡時播放音效（預設：怪物死）。")]
    public bool playDeathSfxOnDie = true;
    public string deathSfxName = "章魚死";

    [Header("UI Settings")]
    public string bossName = "Ancient Golem";
    [Tooltip("Drag the BossHealthPanel here manually if auto-find fails")]
    public BossHealthBar healthBar;

    [Header("Death Events")]
    [Tooltip("Boss 死亡後延遲幾秒觸發事件/影片")]
    public float deathEventDelay = 3f;
    [Tooltip("Boss 死亡時是否套用顏色變化（石化/灰化）")]
    public bool applyDeathTint = true;
    [Tooltip("Boss 死亡顏色")]
    public Color deathTintColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [Tooltip("Boss 死亡時播放影片並切換場景的 Loader")]
    public VideoIntroLoader deathVideoLoader;
    [Tooltip("Boss 死亡時的額外事件")]
    public UnityEngine.Events.UnityEvent onBossDeath;

    [Header("Combat Settings")]
    public float attackInterval = 4f;
    private float attackTimer;

    [Header("鏡頭震動")]
    [Header("鏡頭 — 落石（中長、較重）")]
    [SerializeField] private float rockSlideShakeDuration = 0.5f;
    [SerializeField] private float rockSlideShakeMagnitude = 0.4f;
    [Header("鏡頭 — 水彈（齊射當下短促震動）")]
    [SerializeField] private float waterBombShakeDuration = 0.35f;
    [SerializeField] private float waterBombShakeMagnitude = 0.22f;
    [Tooltip("水彈實際射出前搖：等 Animator 狀態 normalizedTime 達此值，或逾時用下方秒數")]
    [SerializeField, Range(0f, 1f)] private float waterBombFireNormalizedTime = 0.42f;
    [Tooltip("Animator 狀態節點名（例：water bomb start），不是 Trigger 參數 WaterBombStart")]
    [SerializeField] private string waterBombStartStateName = "water bomb start";
    [SerializeField] private float waterBombSpawnFallbackSeconds = 2f;
    [SerializeField] private float waterBombAnimEndFallbackSeconds = 2f;

    // Internal State
    private bool isActive = false; // Defalt to false (Sleeping)
    private bool isDead = false;
    private Rigidbody2D rb;
    private Collider2D col;

    [Header("Idle Hover")]
    [Tooltip("垂直漂浮幅度（世界座標）；過大會看起來像是在飄走")]
    [SerializeField] private float hoverAmplitudeWorld = 0.12f;
    [SerializeField] private float hoverFrequency = 2f;
    private float _hoverAnchorWorldY;

    private Coroutine _waterBombRoutine;
    private bool _waterBombSpawnedThisAttack;
    private Coroutine _weakRoutine;
    private readonly List<GameObject> _summonedMinions = new List<GameObject>();

    /// <summary>Animator 狀態名（與 Trigger 參數 WaterBombStart 不同）。須與圖上狀態節點名稱一致。</summary>
    static readonly string[] WaterBombStartStateAliases = { "water bomb start", "WaterBombStart" };

    private void Start()
    {
        NormalizeWaterBombStateName();
        // 若已擊敗則不再生成/顯示
        if (!string.IsNullOrEmpty(bossID) && PlayerPrefs.GetInt(bossID + "_Dead", 0) == 1)
        {
            gameObject.SetActive(false);
            return;
        }

        _hoverAnchorWorldY = transform.position.y;
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        attackTimer = attackInterval;
        summonTimer = summonInterval;

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>();

        // Ensure Boss itself is weightless (Water Mode A)
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.drag = 10f; 
        }

        if (healthBar != null)
        {
            // Do NOT activate immediately. Wait for BossZoneTrigger to call ActivateBoss().
            // This prevents the boss from attacking if the object is enabled by mistake.
            healthBar.gameObject.SetActive(false);
        }
    }
    
    // Call this from BossZoneTrigger when player enters
    public void ActivateBoss()
    {
        if (isActive || isDead) return;
        
        isActive = true;
        Debug.Log("[AncientGolem] Boss Activated! State is now Active.");

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(true);
            healthBar.SetBossName(bossName);
            healthBar.Initialize(maxHealth);
            healthBar.UpdateHealth(currentHealth);
        }
    }
    
    private void OnDisable()
    {
        // Ensure UI is hidden if scene changes or object is disabled
        if (healthBar != null) healthBar.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isActive || isDead) return;

        // Hover Effect (Optional simple idle movement)
        HoverBehavior();

        if (isWeak) return; // 虛弱期間暫停常駐召喚與攻擊
        if (isWeakRecovering) return; // 虛弱起身後短暫不攻擊

        // Automatic Summon Loop
        summonTimer -= Time.deltaTime;
        if (summonTimer <= 0f)
        {
            summonTimer = summonInterval + Random.Range(-2f, 2f); 
            SpawnMinions(3); // Spawn 3 minions periodically
        }

        // 水彈協程進行中時不選新招，避免 StopCoroutine 導致「有動畫、沒水彈」
        if (_waterBombRoutine == null)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = attackInterval;
                PerformAttack();
            }
        }
    }

    private void PerformAttack()
    {
        if (_waterBombRoutine != null) return;

        if (animator != null)
        {
            if (Random.value < 0.5f)
            {
                Debug.Log("[AncientGolem] Triggering Rock Slide Attack...");
                animator.SetTrigger("Attack");
            }
            else
            {
                Debug.Log("[AncientGolem] Triggering Water Bomb Attack...");
                _waterBombRoutine = StartCoroutine(AttackWaterBombCoroutine());
            }
        }
        else
        {
            Debug.LogWarning("[AncientGolem] Animator is missing! Cannot attack.");
        }
    }

    private IEnumerator AttackWaterBombCoroutine()
    {
        if (animator == null)
        {
            _waterBombRoutine = null;
            yield break;
        }

        _waterBombSpawnedThisAttack = false;
        animator.ResetTrigger("WaterBombStart");
        animator.SetTrigger("WaterBombStart");

        yield return null;
        yield return null;

        EnsureWaterBombStartStatePlaying();

        yield return WaitForWaterBombFirePoint(waterBombFireNormalizedTime, waterBombSpawnFallbackSeconds);

        if (!_waterBombSpawnedThisAttack)
        {
            int spawned = SpawnWaterBombVolley();
            if (spawned > 0)
            {
                _waterBombSpawnedThisAttack = true;
                ShakeCamera(waterBombShakeDuration, waterBombShakeMagnitude);
            }
            else
                Debug.LogWarning("[AncientGolem] 水彈齊射：0 顆（檢查 Water Bomb Prefab / Spawn Points / Camera.main）。");
        }

        yield return WaitForWaterBombStateEnd(waterBombAnimEndFallbackSeconds);
        _waterBombRoutine = null;
    }

    void NormalizeWaterBombStateName()
    {
        if (string.IsNullOrWhiteSpace(waterBombStartStateName) || waterBombStartStateName == "WaterBombStart")
            waterBombStartStateName = "water bomb start";
    }

    static bool IsWaterBombStartState(AnimatorStateInfo info)
    {
        for (int i = 0; i < WaterBombStartStateAliases.Length; i++)
        {
            if (info.IsName(WaterBombStartStateAliases[i]))
                return true;
        }
        return false;
    }

    void EnsureWaterBombStartStatePlaying()
    {
        if (IsWaterBombStartState(animator.GetCurrentAnimatorStateInfo(0)))
            return;

        for (int i = 0; i < WaterBombStartStateAliases.Length; i++)
        {
            string stateName = WaterBombStartStateAliases[i];
            animator.Play(stateName, 0, 0f);
            animator.Update(0f);
            if (IsWaterBombStartState(animator.GetCurrentAnimatorStateInfo(0)))
            {
                waterBombStartStateName = stateName;
                return;
            }
        }

        Debug.LogWarning("[AncientGolem] 無法 Play 水彈狀態，請確認 Animator 狀態名為「water bomb start」。");
    }

    bool TryGetWaterBombStartState(out AnimatorStateInfo info)
    {
        info = animator.GetCurrentAnimatorStateInfo(0);
        return IsWaterBombStartState(info);
    }

    /// <summary>動畫事件請綁這個（或舊名 SpawnAllWaterBombs 會轉到這裡）。</summary>
    public void OnWaterBombFireAnimEvent() => FireWaterBombVolleyIfReady();

    /// <summary>相容舊動畫事件綁定。</summary>
    public void SpawnAllWaterBombs() => OnWaterBombFireAnimEvent();

    void FireWaterBombVolleyIfReady()
    {
        if (_waterBombRoutine == null || isDead || _waterBombSpawnedThisAttack) return;
        int n = SpawnWaterBombVolley();
        if (n > 0)
        {
            _waterBombSpawnedThisAttack = true;
            ShakeCamera(waterBombShakeDuration, waterBombShakeMagnitude);
        }
    }

    int SpawnWaterBombVolley()
    {
        if (waterBombPrefab == null)
        {
            Debug.LogWarning("[AncientGolem] waterBombPrefab 未指定。");
            return 0;
        }

        int spawned = 0;

        if (waterBombSpawnPoints != null && waterBombSpawnPoints.Length > 0)
        {
            foreach (Transform spawnPoint in waterBombSpawnPoints)
            {
                if (spawnPoint == null) continue;

                AudioManager.GetInstance()?.PlayWaterMissileShootSfx();
                GameObject bomb = Instantiate(waterBombPrefab, spawnPoint.position, spawnPoint.rotation);
                var script = bomb.GetComponent<BossProjectile>();
                if (script != null)
                    script.LaunchDirection(spawnPoint.right);
                spawned++;
            }
        }

        if (spawned == 0)
        {
            if (Camera.main == null)
            {
                Debug.LogWarning("[AncientGolem] 無有效發射點且 Camera.main 為 null，無法生成水彈。");
                return 0;
            }

            Vector2 camPos = Camera.main.transform.position;
            float spawnX = camPos.x - 12f;
            float minY = camPos.y - 5f;
            float maxY = camPos.y + 5f;

            for (int i = 0; i < waterBombCount; i++)
            {
                float spawnY = Random.Range(minY, maxY);
                Vector3 spawnPos = new Vector3(spawnX, spawnY, 0);

                AudioManager.GetInstance()?.PlayWaterMissileShootSfx();
                GameObject bomb = Instantiate(waterBombPrefab, spawnPos, Quaternion.identity);
                var script = bomb.GetComponent<BossProjectile>();
                if (script != null)
                    script.LaunchDirection(Vector3.right);
                spawned++;
            }
        }

        if (spawned > 0)
            Debug.Log($"[AncientGolem] 水彈齊射 {spawned} 顆。");

        return spawned;
    }

    private void SpawnMinions(int count)
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            GameObject prefabToSpawn = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
            Vector3 spawnPos = GetMinionSpawnPosition(i);

            GameObject minion = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            _summonedMinions.Add(minion);
            
            // Disable loot drops
            var enemyScript = minion.GetComponent<BaseEnemy2D>();
            if (enemyScript != null)
            {
                enemyScript.coinDropAmount = 0;
                enemyScript.materialDropCount = 0;
            }
        }
    }

    private Vector3 GetMinionSpawnPosition(int index)
    {
        // 1) 指定生成點（可視化最好控）
        if (minionSpawnPoints != null && minionSpawnPoints.Length > 0)
        {
            var t = minionSpawnPoints[index % minionSpawnPoints.Length];
            if (t != null) return t.position;
        }

        // 2) 矩形範圍內隨機（有 Gizmos 可看範圍）
        Vector2 center = (Vector2)transform.position + minionSpawnAreaOffset;
        int guard = 0;
        while (guard++ < 20)
        {
            float x = Random.Range(-minionSpawnAreaSize.x / 2f, minionSpawnAreaSize.x / 2f);
            float y = Random.Range(-minionSpawnAreaSize.y / 2f, minionSpawnAreaSize.y / 2f);
            Vector2 p = center + new Vector2(x, y);
            if (minionMinDistanceFromBoss <= 0f) return new Vector3(p.x, p.y, 0f);
            if (Vector2.Distance(p, transform.position) >= minionMinDistanceFromBoss)
                return new Vector3(p.x, p.y, 0f);
        }

        // 後備：避免無限迴圈
        return transform.position;
    }

    private void HoverBehavior()
    {
        Vector3 p = transform.position;
        p.y = _hoverAnchorWorldY + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitudeWorld;
        transform.position = p;
    }

    // --- Public API for Interaction ---

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        // 虛弱狀態時承受 1.5 倍傷害
        if (isWeak) damage = Mathf.RoundToInt(damage * 1.5f);

        currentHealth -= damage;

        if (playHurtSfxOnDamage && !string.IsNullOrWhiteSpace(hurtSfxName))
        {
            if (Time.time - _lastHurtSfxTime >= Mathf.Max(0f, hurtSfxMinIntervalSeconds))
            {
                AudioManager.GetInstance()?.PlaySFX(hurtSfxName.Trim());
                _lastHurtSfxTime = Time.time;
            }
        }
        
        if (healthBar != null && healthBar.gameObject.activeInHierarchy)
        {
            healthBar.UpdateHealth(currentHealth);
        }

        CheckPhaseTransition();

        // Trigger Flash Effect
        if (gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(DamageFlash());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void CheckPhaseTransition()
    {
        float hpPercent = (float)currentHealth / maxHealth;

        if (hpPercent <= 0.8f && !hasTriggeredWeak1)
        {
            hasTriggeredWeak1 = true;
            if (_weakRoutine == null)
                _weakRoutine = StartCoroutine(EnterWeaknessRoutine(4)); 
        }
        else if (hpPercent <= 0.6f && !hasTriggeredWeak2)
        {
            hasTriggeredWeak2 = true;
            if (_weakRoutine == null)
                _weakRoutine = StartCoroutine(EnterWeaknessRoutine(6)); 
        }
        else if (hpPercent <= 0.4f && !hasTriggeredWeak3)
        {
            hasTriggeredWeak3 = true;
            if (_weakRoutine == null)
                _weakRoutine = StartCoroutine(EnterWeaknessRoutine(8)); 
        }
    }

    private IEnumerator EnterWeaknessRoutine(int spawnCount)
    {
        if (_waterBombRoutine != null)
        {
            StopCoroutine(_waterBombRoutine);
            _waterBombRoutine = null;
        }
        StopBossSustainShake();
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        // 清掉可能殘留的攻擊 Trigger，避免 WeakStart/WeakEnd 與攻擊動畫打架
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("WaterBombStart");
            animator.ResetTrigger("WaterBombEnd");
        }
        
        // 將受擊閃爍強制恢復，避免卡在閃爍顏色
        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r != null) r.color = Color.white;
            }
        }

        isWeak = true;
        if (rb != null) rb.velocity = Vector2.zero;

        // 生成護衛小怪
        SpawnMinions(spawnCount);

        // 播放倒地/虛弱動畫（避免 Animator 誤拉「死亡」過渡）
        if (animator)
        {
            animator.ResetTrigger("Die");
            animator.SetTrigger("WeakStart");
        }

        yield return new WaitForSeconds(weakDuration);

        isWeak = false;
        
        // 播放起身動畫
        if (animator)
        {
            animator.ResetTrigger("Die");
            animator.SetTrigger("WeakEnd");
        }

        // 等 WeakEnd 結束，超時則強制回 idle，避免卡在起身動畫或與攻擊重疊
        if (animator != null)
        {
            float t = 0f;
            bool enteredWeakEnd = false;
            while (t < weakEndMaxWait)
            {
                var info = animator.GetCurrentAnimatorStateInfo(0);
                bool inWeakEnd = info.IsName(weakEndStateName);
                if (inWeakEnd) enteredWeakEnd = true;
                if (enteredWeakEnd && info.normalizedTime >= 1f && !animator.IsInTransition(0))
                    break;
                t += Time.deltaTime;
                yield return null;
            }

            if (!string.IsNullOrEmpty(idleStateName))
                animator.Play(idleStateName, 0, 0f);
        }

        isWeakRecovering = true;
        attackTimer = postWeakNoAttackDuration; // 起身後至少 2 秒不攻擊
        yield return new WaitForSeconds(postWeakNoAttackDuration);
        isWeakRecovering = false;
        _weakRoutine = null;
    }

    private void LateUpdate()
    {
        if (animator == null || isDead) return;
        if (isWeak && currentHealth > 0)
            animator.ResetTrigger("Die");
    }

    private IEnumerator DamageFlash()
    {
        if (renderers == null || renderers.Length == 0) yield break;

        // 1. Change to Flash Color
        foreach (var r in renderers)
        {
            if (r != null) 
            {
                // If using a custom shader with "FlashAmount", use that. 
                // Otherwise, simple color tinting (works best if sprite is white, or using a material that supports overlay)
                // For standard sprite renderer, setting color tints the sprite.
                // To make it truly "white", we often need a material swap or shader property.
                // Here we simply set it to the requested flash color (usually red or white tint).
                r.color = flashColor;
            }
        }

        yield return new WaitForSeconds(flashDuration);

        // 2. Revert to White (Normal)
        foreach (var r in renderers)
        {
            if (r != null) r.color = Color.white;
        }
    }

    static void ShakeCamera(float duration, float magnitude)
    {
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(duration, magnitude);
    }

    static void StartBossSustainShake(float magnitude)
    {
        if (CameraShake.Instance != null)
            CameraShake.Instance.StartSustainShake(magnitude);
    }

    static void StopBossSustainShake()
    {
        if (CameraShake.Instance != null)
            CameraShake.Instance.StopSustainShake();
    }

    IEnumerator WaitForWaterBombFirePoint(float normalizedThreshold, float fallbackSeconds)
    {
        fallbackSeconds = Mathf.Max(0.1f, fallbackSeconds);
        if (animator == null)
        {
            yield return new WaitForSeconds(fallbackSeconds);
            yield break;
        }

        float deadline = Time.time + fallbackSeconds;
        while (Time.time < deadline)
        {
            if (TryGetWaterBombStartState(out var info) && info.normalizedTime >= normalizedThreshold)
                yield break;
            yield return null;
        }

        if (!_waterBombSpawnedThisAttack)
            Debug.LogWarning($"[AncientGolem] 水彈：逾時未進入「{waterBombStartStateName}」發射幀，改強制齊射（Inspector 狀態名請用 water bomb start，不是 Trigger 名 WaterBombStart）。");
    }

    IEnumerator WaitForWaterBombStateEnd(float fallbackSeconds)
    {
        fallbackSeconds = Mathf.Max(0.1f, fallbackSeconds);
        if (animator == null)
        {
            yield return new WaitForSeconds(fallbackSeconds);
            yield break;
        }

        yield return null;
        yield return null;

        float enterTimeout = 1f;
        AnimatorStateInfo tracked = default;
        while (enterTimeout > 0f && !TryGetWaterBombStartState(out tracked))
        {
            enterTimeout -= Time.deltaTime;
            yield return null;
        }

        if (!TryGetWaterBombStartState(out tracked))
        {
            yield return new WaitForSeconds(fallbackSeconds);
            yield break;
        }

        int stateHash = tracked.fullPathHash;
        float maxWait = tracked.length > 0.1f ? tracked.length + 0.15f : fallbackSeconds;
        float timer = 0f;

        while (timer < maxWait)
        {
            if (!TryGetWaterBombStartState(out var info))
                break;

            if (info.fullPathHash == stateHash && info.normalizedTime >= 1f)
                break;

            timer += Time.deltaTime;
            yield return null;
        }
    }

    // --- Attack Logic (Called via Animation Event) ---

    // USAGE: Add an Animation Event in the "Attack_Summon" clip calling this function.
    public void SpawnRocks()
    {
        if (isDead) return;

        ShakeCamera(rockSlideShakeDuration, rockSlideShakeMagnitude);

        // 召喚落石音效（大BOSS）
        AudioManager.GetInstance()?.PlaySFX("章魚召喚落石");

        // Determine which rock to spawn
        GameObject prefabToSpawn = (Random.value > 0.3f) ? smallRockPrefab : bigRockPrefab;
        
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[AncientGolem] Rock Prefabs are missing!");
            return;
        }

        // Calculate Random Position within the box
        Vector2 center = (Vector2)transform.position + spawnAreaOffset;
        float randomX = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float randomY = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        Vector3 spawnPos = new Vector3(center.x + randomX, center.y + randomY, 0);

        // Spawn
        GameObject rock = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        // PHYSICS ENFORCEMENT
        // Ensure the rock falls even in underwater (gravity-less) scenes
        Rigidbody2D rockRb = rock.GetComponent<Rigidbody2D>();
        if (rockRb != null)
        {
            rockRb.gravityScale = rockGravityScale; // Force gravity
            rockRb.drag = 1f; // Optional: Add some drag so it doesn't fall too fast like a bullet
        }

        Debug.Log($"[AncientGolem] Spawned Rock at {spawnPos}");
    }

    // --- Gizmos for Visualizing Spawn Area ---
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Red semi-transparent
        Vector3 center = transform.position + (Vector3)spawnAreaOffset;
        Gizmos.DrawWireCube(center, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 1f));
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawCube(center, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 1f));

        if (showMinionSpawnGizmos)
        {
            // 小怪生成點
            if (minionSpawnPoints != null && minionSpawnPoints.Length > 0)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.9f);
                foreach (var t in minionSpawnPoints)
                {
                    if (t == null) continue;
                    Gizmos.DrawSphere(t.position, 0.25f);
                }
            }

            // 小怪生成範圍
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.6f);
            Vector3 minionCenter = transform.position + (Vector3)minionSpawnAreaOffset;
            Gizmos.DrawWireCube(minionCenter, new Vector3(minionSpawnAreaSize.x, minionSpawnAreaSize.y, 1f));
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.12f);
            Gizmos.DrawCube(minionCenter, new Vector3(minionSpawnAreaSize.x, minionSpawnAreaSize.y, 1f));
        }
    }


    // --- Death Logic ---

    [ContextMenu("Test Die")]
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (playDeathSfxOnDie && !string.IsNullOrWhiteSpace(deathSfxName))
            AudioManager.GetInstance()?.PlaySFX(deathSfxName.Trim());

        StopAllCoroutines();
        StopBossSustainShake();
        _weakRoutine = null;
        _waterBombRoutine = null;
        flashCoroutine = null;
        isWeak = false;
        isWeakRecovering = false;

        Debug.Log("[AncientGolem] Died. Becoming a statue.");

        // 存檔：標記 Boss 已死亡
        if (!string.IsNullOrEmpty(bossID))
        {
            PlayerPrefs.SetInt(bossID + "_Dead", 1);
            PlayerPrefs.Save();
        }

        // 任務：擊殺類目標回報
        if (!string.IsNullOrEmpty(enemyID))
            QuestManager.Instance?.UpdateObjectiveProgress(enemyID, 1);

        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.AddProgress("Kill_SmallBoss", 1);
            AchievementManager.Instance.AddProgress("TotalKills", 1);
            if (!string.IsNullOrEmpty(enemyID))
                AchievementManager.Instance.AddProgress($"Kill_{enemyID}", 1);
        }

        // 1. Play Death Animation
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("WaterBombStart");
            animator.ResetTrigger("WaterBombEnd");
            animator.ResetTrigger("WeakStart");
            animator.ResetTrigger("WeakEnd");
            animator.SetTrigger("Die");
            // Ensure the "Die" animation state in the Controller does NOT loop.
        }

        ApplyDeathTintIfNeeded();

        // 2. Disable Collider (Player can walk through)
        if (col != null)
        {
            col.enabled = false;
        }
        
        // 3. Disable Physics (Stop moving/being pushed)
        if (rb != null)
        {
            rb.simulated = false; // RigidBody stops interacting with physics world entirely
            rb.velocity = Vector2.zero;
        }

        // Boss 死亡時：血條先歸 0，再漸變消失
        if (healthBar != null && healthBar.gameObject.activeInHierarchy)
            healthBar.ForceZeroAndFadeOut();

        // Boss 死亡時：召喚出的所有小怪一併消失
        for (int i = _summonedMinions.Count - 1; i >= 0; i--)
        {
            var m = _summonedMinions[i];
            if (m != null) Destroy(m);
        }
        _summonedMinions.Clear();

        // 4. Handle Post-Death Events (Video, Scene Load, etc.)
        // Do not disable this script immediately so the coroutine can run
        StartCoroutine(DeathEventRoutine());
    }

    private void ApplyDeathTintIfNeeded()
    {
        if (!applyDeathTint || renderers == null) return;
        foreach (var r in renderers)
        {
            if (r != null) r.color = deathTintColor;
        }
    }

    private IEnumerator DeathEventRoutine()
    {
        // Wait for the boss's death animation / dramatic pause
        yield return new WaitForSeconds(deathEventDelay);

        // Trigger any Inspector-assigned UnityEvents (e.g. giving achievements, unlocking doors)
        onBossDeath?.Invoke();

        // 5. Play Video and transition scene
        if (deathVideoLoader != null)
        {
            Debug.Log("[AncientGolem] Triggering Death Video Loader -> Menu");
            deathVideoLoader.PlayAndLoad("Menu");
        }
    }
}
