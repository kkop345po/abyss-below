using UnityEngine;

// 🧙 巫魚（大型/被動）：遠程持續傷害型，原地輸出
public class WitchFish : BaseEnemy2D
{
    [Header("法術輸出")]
    public GameObject projectilePrefab;
    [Tooltip("發射口空物件（建議在 Prefab 下建立子物件 ShootPoint 並拖入）；優先於本地偏移。")]
    public Transform shootPoint;
    [Tooltip("無 Shoot Point 時，相對怪物根節點的發射位置（可在 Scene 視圖用青色球體預覽）。")]
    public Vector2 shootLocalOffset = new Vector2(0.8f, 0f);
    [Tooltip("勾選且已指定 Shoot Point 時，發射方向用該物件的 right（本體朝左時請把子物件 X 軸轉向噴口）。")]
    public bool useShootPointRotationForDirection;
    public float shootCooldown = 1.2f;
    public float projectileSpeed = 8f;
    public float projectileLife = 3f;
    [Header("投射物DoT參數（若 prefab 有 WitchProjectile，會被覆寫）")]
    public float projectileDotDps = 3f;
    public float projectileDotDuration = 3f;

    [Header("投射物緩速（可選）")]
    [Tooltip("勾選後，投射物命中時有機率對玩家施加緩速")]
    public bool enableProjectileSlow = true;
    [Range(0f, 1f)]
    public float projectileSlowChance = 0.3f;
    public float projectileSlowDuration = 2f;
    [Range(0.05f, 1f)]
    [Tooltip("緩速時玩家移動速度乘數（0.5 = 移速減半）")]
    public float projectileSlowSpeedMultiplier = 0.5f;

    [Header("投射物命中爆炸動畫（可選）")]
    [Tooltip("勾選後，子彈撞到玩家時播 Animator 爆炸狀態（須在子彈 Controller 建立該狀態）")]
    public bool enableProjectileExplosionAnim;
    [Tooltip("子彈 Animator 內的爆炸狀態名稱")]
    public string projectileExplosionStateName = "Explosion";

    private float lastShootTime = -999f;

    protected override void Awake()
    {
        base.Awake();
        isAggressive = false; // 被動怪
        facingAngleOffset = 180f; // 反向修正
    }

    [Header("瞬移")]
    public float teleportRange = 3f; // 玩家太近觸發瞬移
    public float teleportCooldown = 5f;
    public float teleportRadius = 6f; // 瞬移到多遠
    private float lastTeleportTime = -999f;
    private bool isTeleporting = false;

    protected override void TickBehavior()
    {
        if (player == null) return;
        
        if (isTeleporting) return; // 瞬移中不動作

        float dist = DistanceToPlayer();
        
        // 瞬移邏輯
        if (dist < teleportRange && Time.time - lastTeleportTime > teleportCooldown)
        {
            StartCoroutine(TeleportRoutine());
            return;
        }

        if (dist <= chaseRange)
        {
            // 原地朝玩家方向施法
            TryShoot();
            // 面向玩家（8方向）
            Vector2 dir = (player.position - transform.position);
            FaceByVector(dir);
        }
        else
        {
            Patrol();
        }
    }

    private System.Collections.IEnumerator TeleportRoutine()
    {
        isTeleporting = true;
        lastTeleportTime = Time.time;
        
        // 1. 淡出 / 變透明
        var originalColor = Color.white;
        if (flashRenderers.Length > 0 && flashRenderers[0] != null) originalColor = flashRenderers[0].color;
        
        float fadeTime = 0.3f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            float alpha = 1f - (t / fadeTime);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(0f);
        
        // 2. 移動位置 (隨機選一個較遠的點)
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector2 targetPos = (Vector2)player.position + randomDir * teleportRadius;
        // 簡單防止瞬移到牆裡 (可選：加 Raycast 檢測)
        transform.position = targetPos;
        
        yield return new WaitForSeconds(0.2f);
        
        // 3. 淡入
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            float alpha = (t / fadeTime);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(1f);
        
        isTeleporting = false;
    }

    private void SetAlpha(float a)
    {
        foreach (var sr in flashRenderers)
        {
            if (sr != null)
            {
                Color c = sr.color;
                c.a = a;
                sr.color = c;
            }
        }
    }

    Vector3 GetShootWorldPosition()
    {
        if (shootPoint != null)
            return shootPoint.position;
        return transform.TransformPoint(shootLocalOffset);
    }

    Vector2 GetShootDirection(Vector3 spawnPos)
    {
        if (useShootPointRotationForDirection && shootPoint != null)
            return shootPoint.right;
        if (player == null)
            return transform.right;
        return ((Vector2)player.position - (Vector2)spawnPos).normalized;
    }

    private void TryShoot()
    {
        if (Time.time - lastShootTime < shootCooldown) return;
        lastShootTime = Time.time;
        if (projectilePrefab == null) return;

        Vector3 spawnPos = GetShootWorldPosition();
        Vector2 shootDir = GetShootDirection(spawnPos);
        if (shootDir.sqrMagnitude < 0.0001f)
            shootDir = transform.right;

        var go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        // 設定投射物速度
        var rb2d = go.GetComponent<Rigidbody2D>();
        if (rb2d)
        {
            rb2d.velocity = shootDir * projectileSpeed;
        }
        
        // 若投射物上有 WitchProjectile，注入持續傷害參數
        var wp = go.GetComponent<WitchProjectile>();
        if (wp)
        {
            wp.damagePerSecond = projectileDotDps;
            wp.duration = projectileDotDuration;
            wp.maxLifetime = projectileLife;
            wp.enableSlowOnHit = enableProjectileSlow;
            wp.slowHitChance = projectileSlowChance;
            wp.slowDuration = projectileSlowDuration;
            wp.slowSpeedMultiplier = projectileSlowSpeedMultiplier;
            wp.playExplosionOnPlayerHit = enableProjectileExplosionAnim;
            if (!string.IsNullOrWhiteSpace(projectileExplosionStateName))
                wp.explosionStateName = projectileExplosionStateName.Trim();
        }
        else
        {
            Destroy(go, projectileLife);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!enabled) return;

        Vector3 pos = shootPoint != null ? shootPoint.position : transform.TransformPoint(shootLocalOffset);
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        Gizmos.DrawWireSphere(pos, 0.12f);

        Vector2 previewDir = GetGizmoShootDirection(pos);
        Gizmos.DrawLine(pos, pos + (Vector3)(previewDir * 0.65f));
    }

    /// <summary>Scene 預覽用：與實際 <see cref="GetShootDirection"/> 規則一致。</summary>
    Vector2 GetGizmoShootDirection(Vector3 spawnPos)
    {
        if (Application.isPlaying)
            return GetShootDirection(spawnPos);

        if (useShootPointRotationForDirection && shootPoint != null)
            return shootPoint.right;

        if (shootPoint != null)
        {
            Vector2 fromMouth = (Vector2)shootPoint.position - (Vector2)transform.position;
            if (fromMouth.sqrMagnitude > 0.0001f)
                return fromMouth.normalized;
        }

        return transform.right;
    }
}


