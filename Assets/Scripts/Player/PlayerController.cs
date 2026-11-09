using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("World Bounds (Optional)")]
    [Tooltip("是否限制玩家位置在 WorldBounds 內。若你覺得玩家進場會被拉扯/抖動，建議關閉，改由相機或碰撞牆限制。")]
    [SerializeField] private bool clampPlayerToWorldBounds = false;
    [Header("Components")]
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private float rotationZVelocity;
    private PlayerParalyze paralyze;
    private PlayerSlow playerSlow;
    private PlayerRopeGrabber2D _ropeGrabber;
    
    [Header("Facing")]
    [Tooltip("承載精靈/模型的節點，用於左右轉身（建議使用子物件；若留空則使用 SpriteRenderer 節點）")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("是否啟用平滑翻面（需要 visualRoot 為子物件）")]
    [SerializeField] private bool smoothFlip = true;
    [Tooltip("平滑翻面時間（秒）")]
    [SerializeField] private float faceFlipSmoothTime = 0.08f;
    [Tooltip("滑鼠在角色左／右側超過此距離才翻面，避免游標貼臉時抖動")]
    [SerializeField] private float faceFlipEnterThreshold = 0.15f;

    private float facingYVelocity;
    private float smoothedHorizontal;
    private float baseVisualScaleX = 1f;
    private float scaleXVelocity;
    private int facingSign = 1; // 1: 面向右, -1: 面向左

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f; // [Modified] 增加陸地移動速度 (5 -> 8)
    [Tooltip("拿武器時的移動速度倍率（< 1 為變慢）")]
    [SerializeField] private float armedMoveSpeedMultiplier = 0.85f;
    [SerializeField] private float swimSpeed = 9f;      // [Modified] 增加水中移動速度 (6 -> 9)
    [SerializeField] private float sprintMultiplierLand = 1.4f;   // 陸地衝刺倍率
    [SerializeField] private float sprintMultiplierWater = 1.4f;  // 水中衝刺倍率
    private Vector2 movementInput;
    private bool isMoving = false;
    private bool isMovingUp = false;
    private bool isMovingDown = false;
    // 左右方向改用 MovingSide 與 flipX，不再單獨追蹤
    private bool isInWater = false;

    /// <summary>供攝影機等系統判斷是否在陸地（非水中）。</summary>
    public bool IsInWater => isInWater;

    /// <summary>衝刺 UI：目前可用格數。</summary>
    public int DashCharges => dashCharges;

    /// <summary>衝刺 UI：最大格數（與 Inspector dashMaxCharges 一致）。</summary>
    public int DashMaxCharges => dashMaxCharges;

    /// <summary>衝刺 UI：是否僅水中顯示／可用。</summary>
    public bool DashOnlyInWater => dashOnlyInWater;

    /// <summary>衝刺 UI：正在排隊回復的格數。</summary>
    public int DashRegenPending => dashRegenPending;

    /// <summary>衝刺 UI：目前這一格回復進度 0～1（無回復時為 0）。</summary>
    public float DashRegenProgress01
    {
        get
        {
            if (dashRegenPending <= 0 || dashRegenStepEndsAt < 0f)
                return 0f;
            float total = Mathf.Max(0.01f, dashRegenSecondsPerCharge);
            return Mathf.Clamp01(1f - (dashRegenStepEndsAt - Time.time) / total);
        }
    }

    // 方向仲裁：避免同時按上下左右時在多個動畫間來回抖動
    private enum DominantAxis { None, Horizontal, Vertical }
    [Tooltip("方向黏著係數：越大越不容易在水平/垂直之間來回切換")]
    [SerializeField] private float directionStickiness = 0.1f;
    private DominantAxis dominantAxis = DominantAxis.None;

    [Header("Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackSpeed = 10f;
    [SerializeField] private float attackCooldown = 0.5f; // 射擊冷卻 (影響總射速)
    [SerializeField] private float attackDuration = 0.2f; // 攻擊動畫長度 (影響多久切換到換彈)
    [SerializeField] private float reloadDuration = 1.0f; // 換彈時間
    [SerializeField] private GameObject spearProjectilePrefab;
    [SerializeField] private float recoilForce = 5f; // 攻擊後座力
    private float lastAttackTime = 0f;
    private bool canAttack = true;
    private bool isReloading = false;
    [Tooltip("武器 Lv.5：攻擊／換彈段落時間縮放（由 ApplyWeaponUpgrades 寫入）")]
    private float _weaponPhaseTimeScale = 1f;
    
    // 走路音效計時器
    private float walkSoundTimer = 0f;
    /// <summary> 上一幀是否在移動（用於停步時立刻 Stop 移動音效）。 </summary>
    private bool _wasMovingForSfx;

    [Header("BGM & Ambient Auto Switch")]
    [Tooltip("上岸／在陸地時要播的 BGM 名稱。留空則不切換（交給場景的 SceneBackgroundMusic／SceneAudioSettings）。")]
    public string landBGMName = "";
    public string landAmbientName = "海浪聲";
    [Tooltip("進入掛有 LandHarborAmbientZone 的港口區時改播的循環環境音（須與 landAmbientName 不同鍵）。")]
    public string harborLandAmbientName = "港口邊";
    [Tooltip("留空＝不切換 BGM，由場景 SceneBackgroundMusic（淺海／中海／深海）負責。填「水下環境音」須在 AudioManager 登錄。")]
    public string waterBGMName = "";
    public string waterAmbientName = "水底氣泡";

    private int _harborAmbientZoneCount;

    [Header("除錯（陸地／港口環境音）")]
    [Tooltip("勾選後會在 Console 印出港口區進出與實際要播的循環環境音名稱。")]
    [SerializeField] private bool debugLandHarborAmbient;

    [Header("Movement SFX")]
    public string landWalkSoundName = "走路新";
    public string waterSwimSoundName = "海底游泳";
    public float walkSoundInterval = 0.4f;
    public float swimSoundInterval = 1.0f;
    private bool isInputLocked = false;
    public bool IsInputLocked => isInputLocked; // 開放給 PlayerInteract 讀取
    private float initialMoveSpeed;
    private float initialSwimSpeed;

    [Header("Weapon System")]
    [SerializeField] private GameObject handModel;
    [SerializeField] private GameObject spearModel;
    private bool hasSpearEquipped = false;

    private readonly string ANIM_IS_MOVING = "IsMoving";
    private readonly string ANIM_LOCAL_X = "LocalInputX";
    private readonly string ANIM_LOCAL_Y = "LocalInputY";
    private readonly string ANIM_ATTACK = "Attack";
    private readonly string ANIM_RELOAD = "Reload";
    private readonly string ANIM_DIE_ARMED = "DieArmed";
    private readonly string ANIM_DIE_UNARMED = "DieUnarmed";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        if (visualRoot == null && spriteRenderer != null)
            visualRoot = spriteRenderer.transform;
        if (visualRoot != null)
            baseVisualScaleX = Mathf.Max(0.0001f, Mathf.Abs(visualRoot.localScale.x));

        // 狀態元件
        paralyze = GetComponent<PlayerParalyze>();
        playerSlow = GetComponent<PlayerSlow>();
        _ropeGrabber = GetComponent<PlayerRopeGrabber2D>();
    }

    private void OnEnable()
    {
        BuffManager.OnMultiplierChanged += OnSquidBuffMultiplierChanged;
    }

    private void OnDisable()
    {
        BuffManager.OnMultiplierChanged -= OnSquidBuffMultiplierChanged;
    }

    private void OnSquidBuffMultiplierChanged(float _)
    {
        ApplySpeedUpgrade();
    }

    private static float GetSquidAbilityMultiplier()
    {
        return BuffManager.Instance != null ? BuffManager.Instance.CurrentAbilityMultiplier : 1f;
    }

    private void Start()
    {
        if (attackPoint == null)
            attackPoint = transform;

        CoinManager.Instance?.UpdateCoinUI();

        initialMoveSpeed = moveSpeed;
        initialSwimSpeed = swimSpeed;
        ApplySpeedUpgrade();

        var health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.RecalculateFromUpgrades();
        }

        // 預設永遠持有武器
        hasSpearEquipped = true;
        UpdateWeaponVisibility();
        ApplyWeaponUpgrades();

        // 初始強制觸發一次環境音樂切換
        StartCoroutine(InitBGMRoutine());

        // 防呆：開場清掉所有觸發器，避免 Animator 預設或外部殘留導致誤觸
        if (animator != null)
        {
            animator.ResetTrigger(ANIM_ATTACK);
            animator.ResetTrigger(ANIM_RELOAD);
            animator.ResetTrigger(ANIM_DIE_ARMED);
            animator.ResetTrigger(ANIM_DIE_UNARMED);
        }

        // Dash 技能初始化：滿格開場
        dashMaxCharges = Mathf.Max(1, dashMaxCharges);
        dashCharges = dashMaxCharges;
        dashRegenPending = 0;
    }

    private System.Collections.IEnumerator InitBGMRoutine()
    {
        // 稍微等待 AudioManager 準備好
        yield return new WaitForSeconds(0.1f);
        bool initState = isInWater;
        isInWater = !initState; // 強制改成相反，迫使下面的 SetInWater 確實執行
        SetInWater(initState);
    }

    // 升級後重新計算移動速度
    public void RecalculateFromUpgrades()
    {
        ApplySpeedUpgrade();

        var health = GetComponent<PlayerHealth>();
        if (health != null)
            health.RecalculateFromUpgrades();

        var oxygen = GetComponent<OxygenManager>();
        if (oxygen != null)
            oxygen.RecalculateFromUpgrades();
    }

    private void ApplySpeedUpgrade()
    {
        float[] bonus = { 0.01f, 0.03f, 0.05f, 0.07f, 0.09f };
        float speedBoost = 1f + bonus[Mathf.Clamp(PlayerDataManager.SpeedLevel, 0, bonus.Length - 1)];
        float abilityMult = GetSquidAbilityMultiplier();
        moveSpeed = initialMoveSpeed * speedBoost * abilityMult;
        swimSpeed = initialSwimSpeed * speedBoost * abilityMult;
    }

    private void Update()
    {
        if (Time.timeScale == 0)
        {
            if (_wasMovingForSfx)
            {
                AudioManager.GetInstance()?.StopMovementSFX();
                _wasMovingForSfx = false;
            }
            walkSoundTimer = 0f;
            return;
        }

        if (isInputLocked)
        {
            if (_wasMovingForSfx)
            {
                AudioManager.GetInstance()?.StopMovementSFX();
                _wasMovingForSfx = false;
            }
            walkSoundTimer = 0f;
            return;
        }
        if (paralyze != null && paralyze.isParalyzed)
        {
            // 麻痺時停止所有輸入與攻擊
            rb.velocity = Vector2.zero;
            if (_wasMovingForSfx)
            {
                AudioManager.GetInstance()?.StopMovementSFX();
                _wasMovingForSfx = false;
            }
            walkSoundTimer = 0f;
            return;
        }

        GetInput();
        HandleDashInput();
        HandleAttack();
        // HandleReloadCosmeticOnRelease(); // [Refactor] 移除，避免與 AttackSequence 的換彈衝突
        HandleWeaponSwitch();

        // 先依滑鼠決定左右面向，再更新 8 方位移動動畫
        UpdateFacing();
        UpdateAnimations();
        
        // 陸地走路與水中游泳音效（停步時 Stop，避免 OneShot 殘響）
        bool allowMoveSfx = isMoving && !isInputLocked;
        if (allowMoveSfx)
        {
            walkSoundTimer -= Time.deltaTime;
            if (walkSoundTimer <= 0f)
            {
                var am = AudioManager.GetInstance();
                if (!isInWater)
                {
                    if (am != null && !string.IsNullOrEmpty(landWalkSoundName))
                        am.PlayMovementSFX(landWalkSoundName);
                    walkSoundTimer = walkSoundInterval;
                }
                else
                {
                    if (am != null && !string.IsNullOrEmpty(waterSwimSoundName))
                        am.PlayMovementSFX(waterSwimSoundName);
                    walkSoundTimer = swimSoundInterval;
                }
            }
        }
        else
        {
            if (_wasMovingForSfx)
                AudioManager.GetInstance()?.StopMovementSFX();
            walkSoundTimer = 0f;
        }
        _wasMovingForSfx = allowMoveSfx;
    }

    private void LateUpdate()
    {
        if (clampPlayerToWorldBounds && WorldBounds.Instance != null)
        {
            transform.position = WorldBounds.Instance.ClampPosition(transform.position);
        }
        // 若場景未放置 WorldBounds，則不夾位，避免 NullReferenceException
    }

    private void FixedUpdate()
    {
        if (paralyze != null && paralyze.isParalyzed)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // Dash 期間移動由 DashRoutine 接管，避免一般移動（AddForce/設定 velocity）干擾衝刺推進
        if (isDashing)
            return;

        // 其他情境的輸入鎖定（對話/過場等）：停住
        if (isInputLocked)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        PlayerStamina stamina = GetComponent<PlayerStamina>();
        bool shouldSprint = stamina != null && stamina.CanSprint() && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && movementInput.sqrMagnitude > 0.01f;

        float slowMult = playerSlow != null ? playerSlow.CurrentSpeedMultiplier : 1f;

        if (isInWater)
        {
            float usedSwimSpeed = swimSpeed * slowMult * (shouldSprint ? sprintMultiplierWater : 1f);
            
            // [Refactor] 使用 AddForce 模擬水下推進，乘上倍率以對抗高阻力 (Drag=3)
            Vector2 force = movementInput * usedSwimSpeed * 5f;
            rb.AddForce(force, ForceMode2D.Force);

            // [Refactor] 移除隨機漂浮 (Drift) 以避免與外部浮力衝突
        }
        else
        {
            float usedMoveSpeed = hasSpearEquipped ? moveSpeed * armedMoveSpeedMultiplier : moveSpeed;
            usedMoveSpeed *= slowMult;
            if (shouldSprint) usedMoveSpeed *= sprintMultiplierLand;
            rb.velocity = new Vector2(movementInput.x * usedMoveSpeed, rb.velocity.y);

        }
    }

    private void HandleDashInput()
    {
        if (!Input.GetKeyDown(dashKey)) return;
        if (!CanUseDashNow()) return;

        Vector2 dir = ResolveDashDirectionFromInput();
        if (dir.sqrMagnitude < 0.0001f) return;

        ConsumeDashCharge();
        // 需求：衝刺中不可以受到傷害，因此玩家技能 Dash 強制無敵
        StartDash(dir, dashDuration, GetDashDistanceByBodyLengths(), true);
    }

    private float GetDashDistanceByBodyLengths()
    {
        float lengths = Mathf.Max(0.1f, dashBodyLengths);

        if (_dashSizeCollider == null)
        {
            // 優先用自身 Collider2D；沒有就找子物件
            _dashSizeCollider = GetComponent<Collider2D>();
            if (_dashSizeCollider == null)
                _dashSizeCollider = GetComponentInChildren<Collider2D>(true);
        }

        float bodyWidth = 1f;
        if (_dashSizeCollider != null)
        {
            // bounds 是世界尺寸，能正確反映縮放
            bodyWidth = Mathf.Max(0.05f, _dashSizeCollider.bounds.size.x);
        }
        else if (spriteRenderer != null)
        {
            bodyWidth = Mathf.Max(0.05f, spriteRenderer.bounds.size.x);
        }

        return bodyWidth * lengths;
    }

    private bool CanUseDashNow()
    {
        if (Time.timeScale == 0) return false;
        if (isInputLocked) return false;
        if (paralyze != null && paralyze.isParalyzed) return false;
        if (dashOnlyInWater && !isInWater) return false;
        if (dashCharges <= 0) return false;

        // 待機不可用
        if (movementInput.sqrMagnitude < 0.01f) return false;

        // 上游/下游（純垂直）不可用；允許右上/右下（斜向）
        bool pureVertical = Mathf.Abs(movementInput.x) < 0.1f && Mathf.Abs(movementInput.y) > 0.1f;
        if (pureVertical) return false;

        return true;
    }

    /// <summary>
    /// Dash 方向規則：以 WASD 組合決定（WD/SD/WA/SA 都支援）。
    /// - 待機與純上下游（只按 W 或只按 S）時，回傳 Vector2.zero（禁止 Dash）
    /// - 水平/斜向：依輸入方向輸出 右/左/右上/右下/左上/左下
    /// </summary>
    private Vector2 ResolveDashDirectionFromInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        const float thr = 0.1f;

        bool hasH = Mathf.Abs(h) >= thr;
        bool hasV = Mathf.Abs(v) >= thr;

        // 待機或純上下游：禁止 Dash
        if (!hasH && !hasV) return Vector2.zero;
        if (!hasH && hasV) return Vector2.zero;

        float x = h > 0f ? 1f : -1f;
        float y = hasV ? (v > 0f ? 1f : -1f) : 0f;
        return new Vector2(x, y).normalized;
    }

    private void ConsumeDashCharge()
    {
        dashCharges = Mathf.Max(0, dashCharges - 1);
        dashRegenPending++;
        ScheduleDashRegenStepEnd();

        if (dashRegenCoroutine == null && dashRegenSecondsPerCharge > 0.01f)
            dashRegenCoroutine = StartCoroutine(DashRegenLoop());
    }

    private void ScheduleDashRegenStepEnd()
    {
        dashRegenStepEndsAt = Time.time + Mathf.Max(0.01f, dashRegenSecondsPerCharge);
    }

    private System.Collections.IEnumerator DashRegenLoop()
    {
        while (dashRegenPending > 0)
        {
            ScheduleDashRegenStepEnd();
            yield return new WaitForSeconds(dashRegenSecondsPerCharge);
            if (dashCharges < dashMaxCharges)
                dashCharges++;
            dashRegenPending = Mathf.Max(0, dashRegenPending - 1);
            if (dashRegenPending > 0)
                ScheduleDashRegenStepEnd();
            else
                dashRegenStepEndsAt = -1f;
        }
        dashRegenCoroutine = null;
    }

    [Header("Dash Settings")]
    [SerializeField] private LayerMask dashBlockMask; // 指定牆壁圖層（如 Ground）
    [SerializeField] private float dashSkin = 0.05f;  // 與牆保持的最小距離

    [Header("Dash Collision")]
    [Tooltip("Dash 期間要穿過的碰撞層（例如 Enemy、NPC、Item 等）。地圖/牆壁請留在 dashBlockMask，不要放這裡。")]
    [SerializeField] private LayerMask dashPassThroughMask;

    [Header("Dash Ability (Charges)")]
    [Tooltip("玩家技能 Dash 的按鍵。")]
    [SerializeField] private KeyCode dashKey = KeyCode.Space;
    [Tooltip("Dash 持續秒數。")]
    [SerializeField] private float dashDuration = 0.15f;
    [Tooltip("Dash 位移距離（以角色寬度為 1 身位）。例如 2.5 代表向前衝 2.5 個身位。")]
    [SerializeField] private float dashBodyLengths = 2.5f;
    [Tooltip("DASH 結束/到達位置後，額外延長的無敵秒數。")]
    [SerializeField] private float dashPostInvincibleSeconds = 1f;
    [Tooltip("最大可用次數（格數）。")]
    [SerializeField] private int dashMaxCharges = 3;
    [Tooltip("每次使用後，幾秒回復 1 格。")]
    [SerializeField] private float dashRegenSecondsPerCharge = 20f;
    [Tooltip("是否限制只有水中可用（符合上/下游語意）。")]
    [SerializeField] private bool dashOnlyInWater = true;

    private int dashCharges;
    private int dashRegenPending;
    private float dashRegenStepEndsAt = -1f;
    private Coroutine dashRegenCoroutine;
    private int _dashIgnorePlayerLayer = -1;
    private bool _dashIgnoringCollisions;
    private Collider2D _dashSizeCollider;
    
    private Coroutine currentDashCoroutine;
    private bool isDashing;

    // 強制中斷衝刺 (例如受傷、撞牆時呼叫)
    public void StopDash()
    {
        if (currentDashCoroutine != null)
        {
            StopCoroutine(currentDashCoroutine);
            currentDashCoroutine = null;
        }
        
        isInputLocked = false;
        if (rb != null) rb.velocity = Vector2.zero;
        
        // 恢復無敵狀態 (若 Dash 有開無敵)
        PlayerHealth hp = GetComponent<PlayerHealth>();
        if (hp != null) hp.CancelInvincible(); // 假設 PlayerHealth 有此方法，若無則需確認
    }

    private void StartDash(Vector2 dir, float duration, float distance, bool invincible)
    {
        if (currentDashCoroutine != null) return;
        isDashing = true;
        currentDashCoroutine = StartCoroutine(DashRoutine(dir, duration, distance, invincible));
    }

    // 公開 API：推進器等外部呼叫（向面朝方向推進）。
    public System.Collections.IEnumerator Dash(float duration, float distance, bool invincible)
    {
        Vector2 dir = facingSign >= 0 ? Vector2.right : Vector2.left;
        yield return DashRoutine(dir, duration, distance, invincible);
    }

    // 推進衝刺（duration 秒內推進 distance，期間可選無敵）。含防穿牆。
    private System.Collections.IEnumerator DashRoutine(Vector2 dir, float duration, float distance, bool invincible)
    {
        if (duration <= 0f || distance <= 0f) yield break;
        if (rb == null) yield break;
        if (dir.sqrMagnitude < 0.0001f) yield break;
        dir.Normalize();

        // 播放衝刺音效（音量倍率 1 = 使用 Sound 表原始音量）
        if (AudioManager.GetInstance() != null) AudioManager.GetInstance().PlaySFX("像素衝刺音效", 1f);

        isInputLocked = true;

        // 清掉既有動量，避免水中 AddForce 或殘留速度把衝刺推偏
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        BeginDashCollisionIgnore();

        // 動畫：提供 Dash 觸發與方向參數（右向為主，左向交由翻面控制）
        if (animator != null)
        {
            animator.SetFloat(ANIM_LOCAL_X, dir.x * facingSign); // localX：以面向為前（向前為正）
            animator.SetFloat(ANIM_LOCAL_Y, dir.y);
            animator.SetTrigger("Dash");
        }

        PlayerHealth hp = GetComponent<PlayerHealth>();
        if (invincible && hp != null)
            hp.InvincibleFor(duration + Mathf.Max(0f, dashPostInvincibleSeconds));

        // 用碰撞體形狀做前方檢測，保證每次衝刺都「沿鎖定方向」前進且被地圖真實擋住
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = dashBlockMask;
        filter.useTriggers = false;
        RaycastHit2D[] hits = new RaycastHit2D[8];

        float remaining = duration;
        float speed = distance / duration; // 單位/秒

        try
        {
            while (remaining > 0f)
            {
                yield return new WaitForFixedUpdate();

                float dt = Mathf.Min(Time.fixedDeltaTime, remaining);
                remaining -= dt;

                float stepDist = speed * dt;
                if (stepDist <= 0.0001f) continue;

                // Cast：用玩家的 Rigidbody2D 形狀朝前測試這一步能走多遠
                int hitCount = rb.Cast(dir, filter, hits, stepDist + dashSkin);
                float allowed = stepDist;

                if (hitCount > 0)
                {
                    float minDist = float.MaxValue;
                    for (int i = 0; i < hitCount; i++)
                    {
                        float d = hits[i].distance;
                        if (d > 0f && d < minDist) minDist = d;
                    }

                    if (minDist != float.MaxValue)
                    {
                        allowed = Mathf.Max(0f, minDist - dashSkin);
                        // 若幾乎不能前進，直接結束衝刺
                        if (allowed <= 0.0001f)
                            break;
                    }
                }

                rb.MovePosition(rb.position + dir * allowed);
            }
        }
        finally
        {
            isInputLocked = false;
            currentDashCoroutine = null;
            EndDashCollisionIgnore();
            isDashing = false;
        }
    }

    private void BeginDashCollisionIgnore()
    {
        if (_dashIgnoringCollisions) return;
        _dashIgnorePlayerLayer = gameObject.layer;
        int mask = dashPassThroughMask.value;
        if (mask == 0)
        {
            _dashIgnoringCollisions = true;
            return;
        }

        for (int layer = 0; layer < 32; layer++)
        {
            if ((mask & (1 << layer)) != 0)
            {
                Physics2D.IgnoreLayerCollision(_dashIgnorePlayerLayer, layer, true);
            }
        }
        _dashIgnoringCollisions = true;
    }

    private void EndDashCollisionIgnore()
    {
        if (!_dashIgnoringCollisions) return;
        int mask = dashPassThroughMask.value;
        if (mask != 0 && _dashIgnorePlayerLayer >= 0)
        {
            for (int layer = 0; layer < 32; layer++)
            {
                if ((mask & (1 << layer)) != 0)
                {
                    Physics2D.IgnoreLayerCollision(_dashIgnorePlayerLayer, layer, false);
                }
            }
        }
        _dashIgnoringCollisions = false;
        _dashIgnorePlayerLayer = -1;
    }

    private void GetInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        movementInput = new Vector2(horizontal, vertical).normalized;
        isMoving = movementInput.magnitude > 0.1f;

        // 方向仲裁：同時按時只選一個主方向（含遲滯帶）
        float ax = Mathf.Abs(horizontal);
        float ay = Mathf.Abs(vertical);
        const float dirThreshold = 0.1f; // 與上方一致
        if (ax < dirThreshold && ay < dirThreshold)
        {
            dominantAxis = DominantAxis.None;
            isMovingUp = false;
            isMovingDown = false;
        }
        else
        {
            if (dominantAxis == DominantAxis.Horizontal)
            {
                // 保持水平，除非垂直明顯更強
                dominantAxis = (ay > ax + directionStickiness) ? DominantAxis.Vertical : DominantAxis.Horizontal;
            }
            else if (dominantAxis == DominantAxis.Vertical)
            {
                // 保持垂直，除非水平明顯更強
                dominantAxis = (ax > ay + directionStickiness) ? DominantAxis.Horizontal : DominantAxis.Vertical;
            }
            else
            {
                dominantAxis = (ax >= ay) ? DominantAxis.Horizontal : DominantAxis.Vertical;
            }

            if (dominantAxis == DominantAxis.Vertical)
            {
                isMovingUp = vertical > dirThreshold;
                isMovingDown = vertical < -dirThreshold;
            }
            else
            {
                isMovingUp = false;
                isMovingDown = false;
            }
        }

        // [Refactor] 移除舊的水平濾波代碼 (horizontalSmoothing 已刪除)
        // float lerpK = 1f - Mathf.Exp(-horizontalSmoothing * Time.deltaTime);
        // smoothedHorizontal = Mathf.Lerp(smoothedHorizontal, horizontal, lerpK);
    }

    // [Refactor] 移除 RotateToMouse，因為我們現在使用 8 方位動畫
    // private void RotateToMouse() { ... }

    private void HandleAttack()
    {
        if (!hasSpearEquipped) return;
        if (_ropeGrabber != null && _ropeGrabber.IsGrabbing)
            return;

        // [Refactor] 純垂直移動時不能射擊 (允許對角線射擊)
        // 若 X 軸幾乎無輸入，但 Y 軸有輸入，則視為純垂直
        bool isPureVertical = Mathf.Abs(movementInput.x) < 0.1f && Mathf.Abs(movementInput.y) > 0.1f;
        if (isPureVertical) return;

        // [Refactor] 改為單發點擊 (GetMouseButtonDown) 且需非換彈狀態
        float effCd = attackCooldown * _weaponPhaseTimeScale;
        if (Input.GetMouseButtonDown(0) && canAttack && !isReloading && Time.time >= lastAttackTime + effCd)
        {
            StartCoroutine(AttackSequence());
        }
    }

    /// <summary>升級武器或讀檔後呼叫，套用射速／換彈時間縮放。</summary>
    public void ApplyWeaponUpgrades()
    {
        _weaponPhaseTimeScale = WeaponUpgradeBalance.GetPhaseTimeScale(PlayerDataManager.WeaponUpgradeLevel);
    }

    static Vector2 RotateVectorDegrees(Vector2 v, float degreesClockwise)
    {
        float rad = degreesClockwise * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    void SpawnSpearVolley(Vector3 spawnPosition, Vector2 baseDirection)
    {
        if (spearProjectilePrefab == null) return;

        int lvl = PlayerDataManager.WeaponUpgradeLevel;
        int count = WeaponUpgradeBalance.GetProjectileCount(lvl);
        float halfSpread = WeaponUpgradeBalance.GetSpreadHalfAngleDegrees();
        float dmgMult = WeaponUpgradeBalance.GetDamageMultiplier(lvl) * GetSquidAbilityMultiplier();
        int pierce = WeaponUpgradeBalance.GetPierceBudget(lvl);

        for (int i = 0; i < count; i++)
        {
            float angleOff = 0f;
            if (count == 3)
                angleOff = (i - 1) * halfSpread;
            Vector2 dir = RotateVectorDegrees(baseDirection, angleOff).normalized;
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, 0, angleDeg);

            GameObject projectile = Instantiate(spearProjectilePrefab, spawnPosition, rotation);
            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            if (projectileRb != null)
                projectileRb.velocity = dir * attackSpeed;

            Bullet bullet = projectile.GetComponent<Bullet>();
            if (bullet != null)
            {
                int prefabDmg = bullet.damage;
                bullet.ApplyWeaponDamageFromUpgrade(prefabDmg, dmgMult);
                bullet.pierceEnemiesRemaining = pierce;
                bullet.pierceDamageFactorAfterHit = WeaponUpgradeBalance.PierceDamageFactorAfterHit;
            }
        }
    }

    private System.Collections.IEnumerator AttackSequence()
    {
        lastAttackTime = Time.time;
        canAttack = false;
        isReloading = true; // 開始射擊流程即視為忙碌/換彈中
        float ph = _weaponPhaseTimeScale;

        // 1. 執行射擊
        if (spearProjectilePrefab != null)
        {
            Vector3 spawnPosition = attackPoint.position;

            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (Vector2)mousePos - (Vector2)transform.position;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.right;
            else
                direction.Normalize();

            SpawnSpearVolley(spawnPosition, direction);
        }

        // 攻擊後座力
        if (rb != null)
        {
            Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mousePos - (Vector2)transform.position).normalized;
            rb.AddForce(-direction * recoilForce, ForceMode2D.Impulse);
        }

        // 射擊瞬間：攝影機短促震動（與後座力分開，避免只靠推角色造成跟拍抖動）
        if (CameraShake.Instance != null)
            CameraShake.Instance.ShakeOnShoot();

        // 播放射擊音效 ( AudioManager )
        if (AudioManager.GetInstance() != null) AudioManager.GetInstance().PlaySFX("射擊");

        if (animator != null)
            animator.SetTrigger(ANIM_ATTACK);

        if (animator != null)
            animator.SetTrigger(ANIM_ATTACK);

        // 2. 等待攻擊動畫播放完畢 (或到達切換點)
        // 使用獨立的 attackDuration 來控制何時切換到換彈，避免卡頓
        yield return new WaitForSeconds(attackDuration * ph);

        // 3. 觸發換彈動畫
        if (animator != null)
            animator.SetTrigger(ANIM_RELOAD);

        // 4. 等待換彈時間
        yield return new WaitForSeconds(reloadDuration * ph);

        // 5. 確保總冷卻時間 (若 attackDuration + reloadDuration < attackCooldown，則補足剩餘時間)
        float totalTimePassed = (attackDuration + reloadDuration) * ph;
        float effCd = attackCooldown * ph;
        if (totalTimePassed < effCd)
        {
            yield return new WaitForSeconds(effCd - totalTimePassed);
        }

        // 5. 恢復可射擊狀態
        canAttack = true;
        isReloading = false;
    }

    // 移除舊的 Attack 與 ResetAttack 方法
    // private void Attack() { ... }
    // private void ResetAttack() { ... }

    private void HandleWeaponSwitch()
    {
        // [Refactor] 移除武器切換，始終持有武器
    }

    private void UpdateWeaponVisibility()
    {
        if (handModel != null)
            handModel.SetActive(!hasSpearEquipped);
        if (spearModel != null)
            spearModel.SetActive(hasSpearEquipped);
        if (animator != null)
        {
            // 移除 HasWeapon 參數設定，因為不再需要切換
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        bool isMoving = movementInput.sqrMagnitude > 0.01f;
        animator.SetBool(ANIM_IS_MOVING, isMoving);

        // 移動動畫：以滑鼠決定的左右面向為基準，WASD 為相對方向
        float localX = movementInput.x * facingSign;
        float localY = movementInput.y;

        // 向後退時，上下分量改依瞄準方向（滑鼠）
        if (localX < -0.1f)
            localY = GetAimDirection().y;

        animator.SetFloat(ANIM_LOCAL_X, localX);
        animator.SetFloat(ANIM_LOCAL_Y, localY);

        if (spearModel != null)
        {
            Vector2 aimDir = GetAimDirection();
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            spearModel.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    Vector2 GetAimDirection()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera == null)
            return facingSign >= 0 ? Vector2.right : Vector2.left;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (Vector2)(mouseWorld - transform.position);
        if (dir.sqrMagnitude < 0.0001f)
            return facingSign >= 0 ? Vector2.right : Vector2.left;
        return dir.normalized;
    }

    float GetMouseDeltaX()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera == null)
            return movementInput.x;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        return mouseWorld.x - transform.position.x;
    }

    public void SetInWater(bool inWater)
    {
        if (isInWater == inWater) return;
        isInWater = inWater;
        
        if (rb != null) 
        {
            rb.drag = isInWater ? 3f : 0f; // 水下高阻力，離開恢復
        }

        // 自動切換音樂與環境音
        if (AudioManager.GetInstance() != null && Application.isPlaying)
        {
            if (isInWater)
            {
                if (!string.IsNullOrEmpty(waterBGMName)) AudioManager.GetInstance().PlayBGM(waterBGMName);
                if (!string.IsNullOrEmpty(landAmbientName)) AudioManager.GetInstance().StopLoopingSFX(landAmbientName);
                if (!string.IsNullOrEmpty(harborLandAmbientName)) AudioManager.GetInstance().StopLoopingSFX(harborLandAmbientName);
                if (!string.IsNullOrEmpty(waterAmbientName)) AudioManager.GetInstance().PlayLoopingSFX(waterAmbientName);
            }
            else
            {
                if (!string.IsNullOrEmpty(landBGMName)) AudioManager.GetInstance().PlayBGM(landBGMName);
                if (!string.IsNullOrEmpty(waterAmbientName)) AudioManager.GetInstance().StopLoopingSFX(waterAmbientName);
                ApplyLandAmbientLooping();
            }
        }
    }

    /// <summary>
    /// 選單關閉後依目前是否在水中重套循環環境音（不切換 BGM；由 <see cref="SetInWater"/>／場景邏輯決定 BGM）。
    /// </summary>
    public void RefreshEnvironmentLoopingAfterUiClosed()
    {
        if (AudioManager.GetInstance() == null || !Application.isPlaying) return;

        if (isInWater)
        {
            if (!string.IsNullOrEmpty(landAmbientName)) AudioManager.GetInstance().StopLoopingSFX(landAmbientName);
            if (!string.IsNullOrEmpty(harborLandAmbientName)) AudioManager.GetInstance().StopLoopingSFX(harborLandAmbientName);
            if (!string.IsNullOrEmpty(waterAmbientName)) AudioManager.GetInstance().PlayLoopingSFX(waterAmbientName);
        }
        else
        {
            if (!string.IsNullOrEmpty(waterAmbientName)) AudioManager.GetInstance().StopLoopingSFX(waterAmbientName);
            ApplyLandAmbientLooping();
        }
    }

    void DbgHarbor(string msg)
    {
        if (debugLandHarborAmbient)
            Debug.Log($"[PlayerController 港口環境音] {msg}", this);
    }

    /// <summary>供港口區 Trigger（<see cref="LandHarborAmbientZone"/>）呼叫。</summary>
    public void NotifyEnterHarborAmbientZone()
    {
        _harborAmbientZoneCount++;
        DbgHarbor($"NotifyEnter：區域計數={_harborAmbientZoneCount}，IsInWater={isInWater}");
        if (!isInWater) ApplyLandAmbientLooping();
        else
            DbgHarbor("NotifyEnter：在水中，不呼叫 ApplyLandAmbientLooping。");
    }

    /// <summary>供港口區 Trigger（<see cref="LandHarborAmbientZone"/>）呼叫。</summary>
    public void NotifyExitHarborAmbientZone()
    {
        _harborAmbientZoneCount = Mathf.Max(0, _harborAmbientZoneCount - 1);
        DbgHarbor($"NotifyExit：區域計數={_harborAmbientZoneCount}，IsInWater={isInWater}");
        if (!isInWater) ApplyLandAmbientLooping();
    }

    private void ApplyLandAmbientLooping()
    {
        var am = AudioManager.GetInstance();
        if (am == null || !Application.isPlaying) return;

        if (!string.IsNullOrEmpty(landAmbientName)) am.StopLoopingSFX(landAmbientName);
        if (!string.IsNullOrEmpty(harborLandAmbientName)) am.StopLoopingSFX(harborLandAmbientName);

        if (_harborAmbientZoneCount > 0)
        {
            if (!string.IsNullOrEmpty(harborLandAmbientName))
            {
                DbgHarbor($"Apply：播港口循環「{harborLandAmbientName}」");
                am.PlayLoopingSFX(harborLandAmbientName);
            }
            else
                DbgHarbor("Apply：區域計數>0 但 harborLandAmbientName 為空，無法播放。");
        }
        else if (!string.IsNullOrEmpty(landAmbientName))
        {
            DbgHarbor($"Apply：播一般陸地循環「{landAmbientName}」");
            am.PlayLoopingSFX(landAmbientName);
        }
        else
            DbgHarbor("Apply：區域計數=0 且 landAmbientName 為空，未播任何循環環境音。");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            SetInWater(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            SetInWater(false);
        }
    }

    // [Refactor] 移除此方法，避免與 AttackSequence 的換彈衝突
    // private void HandleReloadCosmeticOnRelease() { ... }

    // 供生命系統呼叫
    public void OnPlayerDied()
    {
        isInputLocked = true;
        canAttack = false;
        if (rb != null) rb.velocity = Vector2.zero;
    }

    public void OnPlayerRespawned()
    {
        isInputLocked = false;
        canAttack = true;
    }

    // 通用：在對話/過場等情況鎖定/解鎖玩家輸入
    public void LockInput()
    {
        isInputLocked = true;
        if (rb != null) rb.velocity = Vector2.zero;
    }

    public void UnlockInput()
    {
        isInputLocked = false;
    }

    public bool HasSpearEquipped => hasSpearEquipped;

    private void UpdateFacing()
    {
        // 左右面向由滑鼠在角色的左／右側決定（與長矛瞄準一致）
        float dx = GetMouseDeltaX();
        float thresh = Mathf.Max(0.01f, faceFlipEnterThreshold);
        if (dx > thresh)
            facingSign = 1;
        else if (dx < -thresh)
            facingSign = -1;

        Transform root = visualRoot != null ? visualRoot : transform;

        // 目標縮放值 (1 或 -1 乘上基礎縮放)
        float targetX = (facingSign > 0 ? 1f : -1f) * baseVisualScaleX;

        // 如果開啟平滑翻轉，且 root 不是自己 (避免物理發生穿牆問題)，使用 SmoothDamp
        // 若使用者堅持要對 root 平滑，也可以移除 `root != transform` 的限制
        if (smoothFlip && root != transform)
        {
            float newScaleX = Mathf.SmoothDamp(root.localScale.x, targetX, ref scaleXVelocity, faceFlipSmoothTime);
            Vector3 s = root.localScale;
            s.x = newScaleX;
            root.localScale = s;
        }
        else
        {
            // 立即翻轉 (也會翻轉該節點下的所有子物件，如武器、飾品)
            Vector3 s = root.localScale;
            s.x = targetX;
            root.localScale = s;
        }

        // 強制關閉 SpriteRenderer 的 flipX，以免重複翻轉或混淆
        if (spriteRenderer != null) spriteRenderer.flipX = false;
    }
}
