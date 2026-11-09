using System.Collections;
using UnityEngine;

public class BossController : BaseEnemy2D
{
    public enum BossState
    {
        Idle,
        Intro,
        Combat,
        RageTransition,
        RageCombat,
        Death
    }

    [Header("Boss Settings")]
    public string bossID = "Boss_Guardian_01"; // 唯一 ID 用於存檔
    public string bossNameDisplay = "Deep Sea Guardian"; // 顯示在 UI 上的名稱
    public BossState currentState = BossState.Idle;
    public float rageThreshold = 0.5f; // [Removed] 移除暴走
    public BossHealthBar healthBar;
    public Material flashMaterial; // Boss 專用的受傷反白材質

    [Header("Attack Settings")]
    public float actionInterval = 2f; // 攻擊間隔
    // public float rageActionInterval = 1.5f; // [Removed] 移除暴走間隔

    [Header("Attack 1: Water Bomb (Danmaku)")]
    public GameObject waterBombPrefab;
    public Transform firePoint; // 保留舊的，以防萬一，但主要使用 laserOrigins
    public float waterBombSpeed = 5f;

    [Header("Attack 2: Laser (Mouth Beam)")]
    // public GameObject laserPrefab; // 已棄用
    public Transform[] laserOrigins;  // 沿用作為水彈發射點
    public Transform laserMouthTransform; // 雷射發射位置 (嘴巴)
    public Collider2D laserHitbox; // 雷射傷害判定框 (掛在嘴巴子物件上)
    public float laserDelay = 0.5f; // 動畫播放後多久開啟傷害
    [Tooltip("找不到雷射動畫狀態時，傷害／鏡頭晃動的後備秒數")]
    public float laserDuration = 10f;
    [SerializeField] private string[] laserBeamStateNames = { "Laser_Beam_A", "Laser_Beam_B" };

    [Header("Dash Settings")]
    public float dashSpeed = 55f; // 衝刺速度（愈大愈快）
    public float dashBrakingDrag = 5f; // 煞車阻力
    public float dashKnockbackForce = 10f; // [New] 衝撞擊退力道
    public GameObject dashWarningPrefab; // [New] 警告動畫 Prefab
    public float dashWarningDuration = 2.0f; // [New] 警告持續時間
    public string preDashAnimTrigger = "PreDash"; // [New] 衝撞前置動畫 Trigger
    public string preDashStateName = "PreDash"; // [New] 衝撞前置動畫狀態名稱 (通常與 Trigger 相同)
    public string dashStateName = "Dash"; // [New] 衝撞過程動畫狀態名稱
    
    [Header("Intro Settings")]
    public float introZoomSize = 5f; // 特寫鏡頭大小
    public float battleCameraSize = 8f; // 戰鬥鏡頭大小 (房間全景)
    [SerializeField] private string introRoarStateName = "Roar";
    [SerializeField] private float introRoarFallbackSeconds = 2f;
    private Transform roomCenterPoint; // 由 Trigger 傳入

    [Header("場景地圖動畫與鏡頭")]
    [SerializeField] private BossRoomMapAnimation roomMapAnimation;
    [Tooltip("雷射開始後幾秒播場景地圖動畫")]
    [SerializeField] private float laserMapAnimDelay = 2f;
    [Header("鏡頭 — 雷射（開火至動畫結束，全程持續）")]
    [SerializeField] private float laserSustainShakeMagnitude = 0.58f;
    [Header("鏡頭 — 衝撞（全程持續）")]
    [SerializeField] private float dashSustainShakeMagnitude = 0.42f;
    [Header("鏡頭 — 水彈（齊射當下短促震動）")]
    [SerializeField] private float waterBombShakeDuration = 0.35f;
    [SerializeField] private float waterBombShakeMagnitude = 0.38f;
    [Tooltip("水彈實際射出前搖：等 Attack 動畫 normalizedTime 達此值，或逾時用下方秒數")]
    [SerializeField, Range(0f, 1f)] private float waterBombFireNormalizedTime = 0.42f;
    [SerializeField] private string waterBombAttackStateName = "Attack";
    [SerializeField] private float waterBombSpawnFallbackSeconds = 2f;
    [SerializeField] private float waterBombAnimEndFallbackSeconds = 2f;

    [Header("Visuals & Audio")]
    public GameObject chargeEffectPrefab; // 蓄力特效
    public GameObject disappearEffectPrefab; // [New] 消失特效
    public AudioClip chargeSound; // 蓄力音效
    public string[] laserAnimTriggers = new string[] { "LaserAttack" }; // 隨機雷射動畫觸發器列表
    
    private bool isAttacking = false; // 是否正在攻擊中 (避免重疊)
    private float actionTimer = 0f;
    private Vector3 initialPosition;

    protected override void Awake()
    {
        base.Awake();
        if (string.IsNullOrWhiteSpace(enemyID))
            enemyID = "BigOctopus";
    }

    protected override void Start()
    {
        // 檢查是否已死亡
        if (PlayerPrefs.GetInt(bossID + "_Dead", 0) == 1)
        {
            gameObject.SetActive(false);
            return;
        }

        // 成就「深淵觸手」：小 BOSS 擊殺鍵 Kill_BigOctopus（若 Prefab 已填 enemyID 則不覆寫）
        if (string.IsNullOrWhiteSpace(enemyID))
            enemyID = "BigOctopus";

        base.Start();
        initialPosition = transform.position;
        
        // 設定受傷閃爍為紅色
        flashColor = Color.red;

        // 小BOSS：受傷/死亡使用「怪物受傷」「怪物死」
        playHurtSfxOnDamage = true;
        hurtSfxName = "怪物受傷";
        playDeathSfxOnDie = true;
        deathSfxName = "怪物死";

        // [Physics] 如果是 Dynamic，設定水下阻力；如果是 Kinematic，阻力無效，需手動模擬
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) rb.drag = 3f;
        
        // 排除雷射部分的 Renderer，避免閃爍時影響雷射特效
        if (laserMouthTransform != null)
        {
            var allRenderers = new System.Collections.Generic.List<SpriteRenderer>(flashRenderers);
            var laserRenderers = laserMouthTransform.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var lr in laserRenderers)
            {
                if (allRenderers.Contains(lr))
                {
                    allRenderers.Remove(lr);
                }
            }
            flashRenderers = allRenderers.ToArray();
        }
        
        if (healthBar == null)
            healthBar = GetComponentInChildren<BossHealthBar>(true);

        if (healthBar == null)
        {
            var bars = FindObjectsByType<BossHealthBar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (bars.Length == 1)
                healthBar = bars[0];
            else if (bars.Length > 1)
                Debug.LogWarning("[BossController] 場景內有多個 BossHealthBar，請在 Inspector 手動指定 healthBar。");
        }

        if (healthBar != null && currentState == BossState.Idle)
        {
            healthBar.Initialize(maxHP, currentHP);
        }
        else
        {
            Debug.LogWarning($"BossController: 未找到 BossHealthBar UI，請確認場景中是否有該物件。");
        }
        
        // 確保雷射 Hitbox 初始關閉
        if (laserHitbox != null) laserHitbox.enabled = false;

        currentState = BossState.Idle;
    }

    public void StartBossFight(Transform roomCenter, Transform bossAnchor = null)
    {
        if (currentState != BossState.Idle) return;
        
        this.roomCenterPoint = roomCenter;
        
        // [Fix] 設定 Boss 的「歸位點」
        // 如果有指定 Boss 專用錨點，就用錨點；否則使用房間中心
        Transform targetAnchor = bossAnchor != null ? bossAnchor : roomCenter;
        
        if (targetAnchor != null)
        {
            initialPosition = targetAnchor.position;
            // 修正 Z 軸，保持 Boss 原本的深度
            initialPosition.z = transform.position.z;
        }

        if (healthBar != null) 
        {
            healthBar.gameObject.SetActive(true);
            healthBar.Initialize(maxHP, currentHP);
        }
        StartCoroutine(IntroRoutine());
    }

    public void ResetBoss()
    {
        StopAllCoroutines();
        StopIntroRoarSfx();
        StopBossSustainShake();
        
        // 恢復血量
        currentHP = maxHP;
        // 恢復血量
        currentHP = maxHP;
        isAttacking = false; // 重置攻擊狀態
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHP);
            healthBar.SetRageMode(false);
            healthBar.gameObject.SetActive(false);
        }

        // 恢復位置與狀態
        transform.position = initialPosition;
        currentState = BossState.Idle;
        
        // 關閉 Hitbox
        if (laserHitbox != null) laserHitbox.enabled = false;

        // 顯示本體（如果隱藏中）
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers) r.enabled = true;
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = true;
    }

    private IEnumerator IntroRoutine()
    {
        currentState = BossState.Intro;

        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollowClamped>() : null;
        if (cam != null)
            cam.LockCamera(transform.position, introZoomSize);

        if (animator) animator.SetTrigger("Roar");

        var am = AudioManager.GetInstance();
        if (am != null)
            am.PlayActionSFX(AudioManager.SfxName.MonsterRoar);

        try
        {
            yield return WaitForAnimatorStateEnd(introRoarStateName, introRoarFallbackSeconds);
        }
        finally
        {
            StopIntroRoarSfx();
        }

        if (cam != null && roomCenterPoint != null)
            cam.LockCamera(roomCenterPoint.position, battleCameraSize);

        currentState = BossState.Combat;
        actionTimer = actionInterval;
    }

    /// <summary>進場 Roar 動畫結束時可掛 Animation Event 呼叫。</summary>
    public void StopIntroRoarSfx()
    {
        AudioManager.GetInstance()?.StopActionSFX();
    }

    IEnumerator WaitForAnimatorStateEnd(string stateName, float fallbackSeconds)
    {
        yield return WaitForAnyAnimatorStateEnd(new[] { stateName }, fallbackSeconds);
    }

    IEnumerator WaitForAnyAnimatorStateEnd(string[] stateNames, float fallbackSeconds)
    {
        fallbackSeconds = Mathf.Max(0.1f, fallbackSeconds);

        if (animator == null || stateNames == null || stateNames.Length == 0)
        {
            yield return new WaitForSeconds(fallbackSeconds);
            yield break;
        }

        yield return null;
        yield return null;

        int stateHash = 0;
        float stateLength = 0f;
        float enterTimeout = 1f;
        while (enterTimeout > 0f && !TryGetMatchingStateHash(stateNames, out stateHash, out stateLength))
        {
            enterTimeout -= Time.deltaTime;
            yield return null;
        }

        if (!TryGetMatchingStateHash(stateNames, out stateHash, out stateLength))
        {
            yield return new WaitForSeconds(fallbackSeconds);
            yield break;
        }

        float maxWait = stateLength > 0.1f ? stateLength + 0.15f : fallbackSeconds;
        float timer = 0f;

        while (timer < maxWait)
        {
            if (!TryGetMatchingStateHash(stateNames, out _, out _))
                break;

            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.fullPathHash == stateHash && info.normalizedTime >= 1f)
                break;

            timer += Time.deltaTime;
            yield return null;
        }
    }

    bool TryGetMatchingStateHash(string[] stateNames, out int stateHash, out float stateLength)
    {
        stateHash = 0;
        stateLength = 0f;
        if (animator == null || stateNames == null) return false;

        var info = animator.GetCurrentAnimatorStateInfo(0);
        for (int i = 0; i < stateNames.Length; i++)
        {
            string n = stateNames[i];
            if (string.IsNullOrWhiteSpace(n)) continue;
            if (info.shortNameHash == Animator.StringToHash(n) || info.IsName(n))
            {
                stateHash = info.fullPathHash;
                stateLength = info.length;
                return true;
            }
        }

        return false;
    }

    protected override void TickBehavior()
    {
        if (currentState == BossState.Idle || currentState == BossState.Intro || currentState == BossState.Death) return;

        // 如果正在攻擊中，就暫停計時，等待攻擊結束
        // 這樣可以確保暴走時間不會被長動畫（如衝撞）消耗掉
        if (isAttacking) return;

        // 戰鬥邏輯
        actionTimer -= Time.deltaTime;
        if (actionTimer <= 0f)
        {
            PerformAttack();
            actionTimer = actionInterval;
        }
    }

    private void PerformAttack()
    {
        // 一般狀態：隨機水彈或雷射或衝撞 (現在衝撞也是普通招式之一)
        // 權重：水彈(40%)、雷射(40%)、衝撞(20%)
        float rand = Random.value;
        if (rand < 0.4f)
        {
            StartCoroutine(AttackWaterBomb());
        }
        else if (rand < 0.8f)
        {
            StartCoroutine(AttackLaser());
        }
        else
        {
            StartCoroutine(AttackDash());
        }
    }

    private IEnumerator AttackWaterBomb()
    {
        isAttacking = true;

        if (chargeEffectPrefab != null) Instantiate(chargeEffectPrefab, transform.position, Quaternion.identity);
        if (chargeSound != null && GetComponent<AudioSource>()) GetComponent<AudioSource>().PlayOneShot(chargeSound);

        if (animator) animator.SetTrigger("Attack");
        yield return WaitForAnimatorFirePoint(waterBombAttackStateName, waterBombFireNormalizedTime, waterBombSpawnFallbackSeconds);

        SpawnAllWaterBombs();
        ShakeCamera(waterBombShakeDuration, waterBombShakeMagnitude);

        yield return WaitForAnimatorStateEnd(waterBombAttackStateName, waterBombAnimEndFallbackSeconds);

        isAttacking = false;
    }

    void SpawnAllWaterBombs()
    {
        if (waterBombPrefab == null) return;

        if (laserOrigins != null && laserOrigins.Length > 0)
        {
            foreach (var origin in laserOrigins)
            {
                if (origin == null) continue;

                if (AudioManager.GetInstance() != null)
                    AudioManager.GetInstance().PlayWaterMissileShootSfx();
                GameObject bomb = Instantiate(waterBombPrefab, origin.position, Quaternion.identity);
                var script = bomb.GetComponent<BossProjectile>();
                if (script == null) continue;

                if (player != null)
                    script.Launch(player.position);
                else
                {
                    Vector3 dir = (origin.position - transform.position).normalized;
                    script.LaunchDirection(dir);
                }
            }
            return;
        }

        if (firePoint == null) return;

        if (AudioManager.GetInstance() != null)
            AudioManager.GetInstance().PlayWaterMissileShootSfx();
        GameObject single = Instantiate(waterBombPrefab, firePoint.position, Quaternion.identity);
        var singleScript = single.GetComponent<BossProjectile>();
        if (singleScript != null && player != null)
            singleScript.Launch(player.position);
    }

    private IEnumerator AttackLaser()
    {
        isAttacking = true;

        // [Telegraph] 蓄力預警
        if (chargeEffectPrefab != null) Instantiate(chargeEffectPrefab, laserMouthTransform != null ? laserMouthTransform.position : transform.position, Quaternion.identity);
        if (chargeSound != null && GetComponent<AudioSource>()) GetComponent<AudioSource>().PlayOneShot(chargeSound);

        PlayRoomMapLaserDelayed(laserMapAnimDelay);

        // 1. 播放隨機雷射攻擊動畫
        if (animator && laserAnimTriggers.Length > 0)
        {
            string randomTrigger = laserAnimTriggers[Random.Range(0, laserAnimTriggers.Length)];
            animator.SetTrigger(randomTrigger);
        }
        else if (animator)
        {
            animator.SetTrigger("LaserAttack");
        }
        
        // 2. 等待動畫播放到發射點 (嘴巴張開)
        yield return new WaitForSeconds(laserDelay);

        // 小BOSS 發射雷射音效（開啟傷害前）
        if (AudioManager.GetInstance() != null) AudioManager.GetInstance().PlaySFX("怪獸激光");

        // 3. 開啟傷害判定 + 鏡頭持續晃動（至雷射結束）
        if (laserHitbox != null)
            laserHitbox.enabled = true;

        StartLaserSustainShake();

        try
        {
            yield return WaitForAnyAnimatorStateEnd(laserBeamStateNames, laserDuration);
        }
        finally
        {
            StopBossSustainShake();
            if (laserHitbox != null)
                laserHitbox.enabled = false;
        }

        isAttacking = false;
    }

    /// <summary>雷射動畫結束時可掛 Animation Event 呼叫。</summary>
    public void StopLaserCameraShake()
    {
        StopBossSustainShake();
    }



    private IEnumerator AttackDash()
    {
        isAttacking = true;
        Debug.Log("Boss Dash: Start Sequence");

        // 1. [New] 播放衝撞前置動畫 (PreDash)
        if (animator) 
        {
            // [Fix] 強制播放狀態，避免因 Transition 設定問題導致不播放
            animator.Play(preDashStateName);
            
            // 重要：等待兩幀以確保 StateInfo 更新
            yield return null; 
            yield return null;
            
            // 獲取當前狀態 ("PreDash")
            var state = animator.GetCurrentAnimatorStateInfo(0);
            
            // 獲取當前狀態 ("PreDash") 的 Hash，用於確認狀態沒跑掉
            int stateHash = state.fullPathHash;
            
            // [Fix] 改用 normalizedTime 檢查，確保與動畫播放進度完全同步
            // 且確保不會等到切換回 Idle 才消失
            float timer = 0f;
            float maxWait = state.length > 0.5f ? state.length : 1.5f; // 安全上限
            
            Debug.Log($"Boss Dash: Forced Play '{preDashStateName}', Detected Length: {state.length}s");
            
            while (timer < maxWait)
            {
                var info = animator.GetCurrentAnimatorStateInfo(0);
                
                // 如果狀態變了 (例如提前切換回 Idle)，立即中斷
                if (info.fullPathHash != stateHash) break;
                
                // 如果播放進度 >= 1 (播完了)，立即中斷
                if (info.normalizedTime >= 1.0f) break;
                
                timer += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(1.5f); // 無 Animator 時的預設等待
        }

        // 2. 消失前特效
        if (disappearEffectPrefab != null) Instantiate(disappearEffectPrefab, transform.position, Quaternion.identity);

        // 3. 暫時隱藏 Boss
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers) r.enabled = false;
        
        // 衝撞時改為 Trigger 模式
        var col = GetComponent<Collider2D>();
        if (col != null) 
        {
            col.enabled = false; 
            col.isTrigger = true;
        }

        // 4. 計算衝撞路徑 (畫面外 -> 畫面外)
        Vector2 camPos = Vector2.zero;
        if (Camera.main != null) camPos = Camera.main.transform.position;
        
        Vector2 dashStartDir;
        if (firePoint != null)
        {
            dashStartDir = (firePoint.position - initialPosition).normalized;
            if (dashStartDir == Vector2.zero) dashStartDir = Vector2.up;
        }
        else
        {
            dashStartDir = Random.insideUnitCircle.normalized;
        }

        // 加大距離確保完全在畫面外 (30單位)
        Vector2 startPos = camPos + dashStartDir * 30f; 
        Vector2 targetPoint = player != null ? (Vector2)player.position : camPos;
        Vector2 dashDir = (targetPoint - startPos).normalized;
        Vector2 endPos = startPos + dashDir * 60f; 

        // 5.瞬間移動到起點
        transform.position = new Vector3(startPos.x, startPos.y, initialPosition.z);

        // 6. [New] 播放警告動畫 (Prefab)
        // 衝撞前警告音效：不論是否有 Prefab，都先播一次
        if (AudioManager.GetInstance() != null) AudioManager.GetInstance().PlaySFX("警告聲");
        if (dashWarningPrefab != null)
        {
             // [Fix] 回歸畫面中心，不旋轉，且強制 Z=0 確保顯示
             Vector3 warningPos = new Vector3(camPos.x, camPos.y, 0f);
             GameObject warning = Instantiate(dashWarningPrefab, warningPos, Quaternion.identity);
             
             // [Fix] 確保警告會消失 (以防 Prefab 沒寫自動銷毀)
             Destroy(warning, dashWarningDuration);
             
             yield return new WaitForSeconds(dashWarningDuration);
        }
        else
        {
            yield return new WaitForSeconds(dashWarningDuration);
        }

        Debug.Log("Boss Dash: Warning finished, dashing now.");

        PlayRoomMapDash();
        StartBossSustainShake(dashSustainShakeMagnitude);

        // 7. 衝撞開始 (現身)
        if (AudioManager.GetInstance() != null) AudioManager.GetInstance().PlaySFX("怪獸衝擊");
        float moveX = endPos.x - startPos.x;
        float facingScale = Mathf.Abs(transform.localScale.x);
        if (moveX > 0)
             transform.localScale = new Vector3(-facingScale, transform.localScale.y, transform.localScale.z);
        else
             transform.localScale = new Vector3(facingScale, transform.localScale.y, transform.localScale.z);

        foreach (var r in renderers) if (r != null) r.enabled = true;
        if (col != null) col.enabled = true;
        
        // [Fix] 強制播放衝撞動畫
        if (animator) animator.Play(dashStateName);
        
        // 8. 衝撞移動（關閉 drag、每幀維持速度，避免被阻力拖慢）
        float dashRbDrag = rb != null ? rb.drag : 0f;
        if (rb != null)
        {
            rb.drag = 0f;
            rb.velocity = dashDir * dashSpeed;
        }

        float dashPathLen = Vector2.Distance(startPos, endPos);
        float timeout = dashPathLen / Mathf.Max(dashSpeed, 1f) + 2f;
        float dist = Vector2.Distance(transform.position, endPos);
        while (dist > 1f && timeout > 0f)
        {
            if (rb != null)
                rb.velocity = dashDir * dashSpeed;
            else
                transform.position = Vector3.MoveTowards(transform.position, endPos, dashSpeed * Time.deltaTime);
            dist = Vector2.Distance(transform.position, endPos);
            float dot = Vector2.Dot(dashDir, (Vector2)transform.position - endPos);
            if (dot > 0) break;
            
            // [Fix] 確保動畫持續播放 (若沒開啟 Loop Time，這裡可以強制 Replay，但建議開啟 Loop)
            // if (animator) 
            // {
            //     var info = animator.GetCurrentAnimatorStateInfo(0);
            //     if (info.normalizedTime >= 1f && !info.loop) animator.Play(dashStateName, 0, 0f);
            // }
            
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.drag = dashRbDrag;
        }

        StopBossSustainShake();

        // 9. 結束處理
        foreach (var r in renderers) if (r != null) r.enabled = false;
        if (col != null) col.enabled = false;
        
        yield return new WaitForSeconds(0.5f);

        // 10. 回歸
        transform.position = initialPosition;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        
        foreach (var r in renderers) if (r != null) r.enabled = true;
        
        if (col != null) 
        {
             // 恢復碰撞
            col.isTrigger = false;
            col.enabled = true;
        }
        
        isAttacking = false;
        Debug.Log("Boss Dash: Finished.");
    }

    BossRoomMapAnimation ResolveRoomMap() =>
        roomMapAnimation != null ? roomMapAnimation : BossRoomMapAnimation.Instance;

    void PlayRoomMapLaserDelayed(float delay) => ResolveRoomMap()?.PlayLaserDelayed(delay);

    void PlayRoomMapDash() => ResolveRoomMap()?.PlayDash();

    static void ShakeCamera(float duration, float magnitude)
    {
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(duration, magnitude);
    }

    void StartLaserSustainShake() => StartBossSustainShake(laserSustainShakeMagnitude);

    void StartBossSustainShake(float magnitude)
    {
        if (CameraShake.Instance != null)
            CameraShake.Instance.StartSustainShake(magnitude);
    }

    void StopBossSustainShake()
    {
        if (CameraShake.Instance != null)
            CameraShake.Instance.StopSustainShake();
    }

    IEnumerator WaitForAnimatorFirePoint(string stateName, float normalizedThreshold, float fallbackSeconds)
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
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (!string.IsNullOrEmpty(stateName) && info.IsName(stateName) && info.normalizedTime >= normalizedThreshold)
                yield break;
            yield return null;
        }
    }

    public override void TakeDamage(int amount)
    {
        if (currentState == BossState.Death || currentState == BossState.Idle) return;

        base.TakeDamage(amount);
        
        if (healthBar != null) 
        {
            healthBar.UpdateHealth(currentHP);
            Debug.Log($"[BossController] TakeDamage: HP={currentHP}, Updating UI");
        }
        else
        {
            Debug.LogWarning($"[BossController] TakeDamage: HealthBar is NULL! HP={currentHP}");
        }
    }

    // [Removed] 移除暴走相關方法
    // private void EnterRageMode() { ... }
    // private void ExitRageMode() { ... }

    protected override void StartDamageFlash()
    {
        if (flashMaterial == null)
        {
            // 如果沒設定材質，使用預設的顏色閃爍
            base.StartDamageFlash();
            return;
        }

        if (isFlashing) return;
        if (flashRenderers == null || flashRenderers.Length == 0) return;
        StartCoroutine(BossMaterialFlashCoroutine());
    }

    private IEnumerator BossMaterialFlashCoroutine()
    {
        isFlashing = true;
        float elapsed = 0f;
        
        // 記錄原材質
        var originalMaterials = new Material[flashRenderers.Length];
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null) originalMaterials[i] = flashRenderers[i].material;
        }
        
        while (elapsed < flashDuration)
        {
            // 換材質 (白)
            for (int i = 0; i < flashRenderers.Length; i++) 
            {
                if (flashRenderers[i]) flashRenderers[i].material = flashMaterial;
            }
            yield return new WaitForSeconds(flashInterval);
            
            // 還原
            for (int i = 0; i < flashRenderers.Length; i++) 
            {
                if (flashRenderers[i]) flashRenderers[i].material = originalMaterials[i];
            }
            yield return new WaitForSeconds(flashInterval);
            
            elapsed += flashInterval * 2f;
        }
        
        // 確保最後完全還原
        for (int i = 0; i < flashRenderers.Length; i++) 
        {
            if (flashRenderers[i]) flashRenderers[i].material = originalMaterials[i];
        }
        isFlashing = false;
    }

    void ReportSmallBossKillAchievements()
    {
        if (AchievementManager.Instance == null)
            return;

        AchievementManager.Instance.AddProgress("Kill_BigOctopus", 1);
        if (!string.IsNullOrEmpty(enemyID))
            AchievementManager.Instance.AddProgress($"Kill_{enemyID}", 1);
    }

    protected override void Die()
    {
        StopIntroRoarSfx();
        StopBossSustainShake();
        currentState = BossState.Death;
        
        // 記錄死亡
        PlayerPrefs.SetInt(bossID + "_Dead", 1);
        PlayerPrefs.Save();

        ReportSmallBossKillAchievements();

        // BaseEnemy2D.Die 會處理動畫等待與銷毀
        base.Die();
        
        // 移除解鎖相機邏輯，讓 BossZoneTrigger 控制即可
        // var cam = Camera.main.GetComponent<CameraFollowClamped>();
        // if (cam != null) cam.UnlockCamera();

        if (healthBar != null) healthBar.gameObject.SetActive(false);
    }

    // 防止 BaseEnemy2D 的接觸攻擊動畫打斷 Boss 的技能動畫
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (isAttacking)
        {
            // [Fix] 衝撞模式下 (Trigger) 也要處理擊退與傷害
            if (other.CompareTag("Player"))
            {
                var pHealth = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.TakeDamage(damageToPlayer);
                    
                    // 擊退
                    Rigidbody2D pRb = other.GetComponent<Rigidbody2D>();
                    if (pRb != null)
                    {
                        Vector2 dir = (other.transform.position - transform.position).normalized;
                        dir += Vector2.up * 0.2f; 
                        pRb.AddForce(dir.normalized * dashKnockbackForce, ForceMode2D.Impulse);
                    }
                    Debug.Log("Boss Dash (Trigger): Hit Player");
                }
            }
            
            DoContactAttack(other); // 確保其他邏輯 (如無敵時間檢查)
            return;
        }
        base.OnTriggerEnter2D(other);
    }

    protected override void OnTriggerStay2D(Collider2D other)
    {
        if (isAttacking)
        {
            DoContactAttack(other);
            return;
        }
        base.OnTriggerStay2D(other);
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        // 衝撞時對玩家造成擊退
        // 衝撞時對玩家造成擊退與傷害
        if (isAttacking && collision.gameObject.CompareTag("Player"))
        {
            // 1. 擊退
            Rigidbody2D pRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (pRb != null)
            {
                Vector2 dir = (collision.transform.position - transform.position).normalized;
                dir += Vector2.up * 0.2f; 
                pRb.AddForce(dir.normalized * dashKnockbackForce, ForceMode2D.Impulse);
            }

            // 2. [Fix] 強制造成傷害 (忽略冷卻時間)
            var pHealth = collision.gameObject.GetComponent<PlayerHealth>()
                          ?? collision.gameObject.GetComponentInParent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.TakeDamage(damageToPlayer);
                Debug.Log("Boss Dash: Hit Player (Damage + Knockback)");
            }
        }

        if (isAttacking)
        {
            DoContactAttack(collision.collider);
            return;
        }
        base.OnCollisionEnter2D(collision);
    }

    protected override void OnCollisionStay2D(Collision2D collision)
    {
        if (isAttacking)
        {
            DoContactAttack(collision.collider);
            return;
        }
        base.OnCollisionStay2D(collision);
    }
}
