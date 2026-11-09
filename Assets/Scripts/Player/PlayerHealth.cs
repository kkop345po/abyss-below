using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    private const int DefaultBaseHealth = 100;
    [Header("Health Settings")]
    public int baseHealth = 100;
    private int currentHealth;
    [Tooltip("體力升級每一級增加多少最大血量（Lv0 時仍為 100HP）。")]
    public int hpPerStaminaLevel = 100;

    [Header("Invincibility Settings")]
    public float invincibilityTime = 1.5f;
    public float invincibilityFlashInterval = 0.15f;
    private bool isInvincible = false;
    private float damageReductionPercentActive = 0f; // 0~1，臨時減傷
    private float damageReductionRemaining = 0f;

    [Header("UI Reference")]
    public Image healthBar;
    public SpriteRenderer playerSprite;

    [Header("Damage Effects")]
    public Color damageFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    public float damageFlashDuration = 0.35f;
    public float damageFlashInterval = 0.08f;
    [Header("持續傷害（中毒等）視覺／音效")]
    public Color dotFlashColor = new Color(1f, 0.05f, 0.05f, 1f);
    public float dotFlashOnSeconds = 0.12f;
    public float dotFlashMinInterval = 0.5f;
    public float dotDamageSfxMinInterval = 0.55f;
    [Tooltip("留空則自動收集子物件 SpriteRenderer（含身體各層）。")]
    public SpriteRenderer[] damageFlashRenderers;
    public Color invincibleColor = Color.white;
    public AudioClip damageSoundClip;
    private AudioSource audioSource;
    private Coroutine damageFlashRoutine;
    private Coroutine dotPulseFlashRoutine;
    private bool isDamageFlashing;
    private float _lastDotFlashTime = -999f;
    private float _lastDotSfxTime = -999f;

    [Header("Death & Respawn Settings")]
    public GameObject deathEffectPrefab;
    [Tooltip("舊版本地重生點；死亡後改為載入陸地場景，此欄位僅保留相容未使用。")]
    public Transform respawnPoint;
    [Tooltip("死亡全螢幕 UI 顯示秒數，結束後載入陸地場景。")]
    public float deathScreenDuration = 7f;
    [Tooltip("死亡後要載入的場景名稱（須與 Build Settings 一致，通常為 Land）。")]
    public string deathLandSceneName = "Land";
    [Tooltip("死亡 UI 第一行文字")]
    public string deathScreenLine1 = "你已死亡";
    [Tooltip("死亡 UI 第二行文字")]
    public string deathScreenLine2 = "即將返回陸地…";
    [Tooltip("死亡 UI 使用的 TMP 字體（由你在 prefab 上指定）。")]
    public TMP_FontAsset deathScreenFont;

    [Header("Knockback Settings")]
    public float knockbackForce = 5f;

    private Rigidbody2D rb;
    private bool isDead = false;
    private Animator animator;
    private PlayerController playerController;
    private int initialBaseHealth;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[PlayerHealth] 場景內有多個 PlayerHealth，保留 {Instance.name}，忽略 {name}");
        }
        else
        {
            Instance = this;
        }

        // 保底：避免 Prefab/Inspector 被清成 0 造成「進場就是 0HP」。
        if (baseHealth <= 0)
            baseHealth = DefaultBaseHealth;
        FindComponents();
        EnsureDamageOverTimeComponent();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>陸地／水中預製體皆需能中毒；缺元件時自動補上。</summary>
    void EnsureDamageOverTimeComponent()
    {
        if (GetComponentInChildren<PlayerDamageOverTime>(true) != null)
            return;
        gameObject.AddComponent<PlayerDamageOverTime>();
    }

    void Start()
    {
        FindUIReferences();
        if (baseHealth <= 0)
            baseHealth = DefaultBaseHealth;
        initialBaseHealth = Mathf.Max(DefaultBaseHealth, baseHealth);
        SetMaxHP(CalculateMaxHP());
    }

    void OnEnable()
    {
        FindUIReferences();
    }

    private void FindComponents()
    {
        // 基本組件
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        if (playerSprite == null)
            playerSprite = GetComponent<SpriteRenderer>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
        }
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    public void RefreshHealthUI()
    {
        FindUIReferences();
        UpdateHealthUI();
    }

    private void FindUIReferences()
    {
        // 血條（海中場景常未在 Prefab 指定，需執行期尋找）
        if (healthBar == null)
        {
            var hpBar = GameObject.Find("HP Foreground");
            if (hpBar != null)
                healthBar = hpBar.GetComponent<Image>();
        }
        if (healthBar == null)
        {
            var bars = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i] != null && bars[i].name == "HP Foreground")
                {
                    healthBar = bars[i];
                    break;
                }
            }
        }

        // 重生點
        if (respawnPoint == null)
        {
            var respawn = GameObject.Find("RespawnPoint");
            if (respawn != null)
                respawnPoint = respawn.transform;
        }
    }

    int CalculateMaxHP()
    {
        // 規則：體力升級 = 血量升級
        // - Lv0：至少 100HP
        // - Lv>0：以初始 baseHealth（至少 100）為基礎，加上每級增加量
        int level = Mathf.Clamp(PlayerDataManager.StaminaLevel, 0, PlayerDataManager.MaxLevel);
        int per = Mathf.Max(0, hpPerStaminaLevel);
        return Mathf.Max(DefaultBaseHealth, initialBaseHealth) + level * per;
    }

    public void SetMaxHP(int newMax)
    {
        int previousMax = Mathf.Max(1, baseHealth > 0 ? baseHealth : DefaultBaseHealth);
        int previousCurrent = currentHealth > 0 ? currentHealth : -1;

        baseHealth = Mathf.Max(DefaultBaseHealth, newMax);
        currentHealth = ResolveHealthAfterMaxChange(previousMax, previousCurrent);

        UpdateHealthUI();
    }

    /// <summary>死亡／異常存檔可能寫入 0；還原時視為應以滿血進場。</summary>
    int CoalesceZeroSavedHealthToFull(int hp, string context)
    {
        if (hp > 0) return Mathf.Clamp(hp, 1, baseHealth);
        Debug.Log($"[PlayerHealth] {context} 存檔血量為 0，改為滿血: {baseHealth}/{baseHealth}");
        return baseHealth;
    }

    /// <summary>依存檔＋升級後上限還原血量（保留百分比，滿血時維持滿血）。</summary>
    int ResolveHealthAfterMaxChange(int previousMax, int previousCurrent)
    {
        int savedHealth = PlayerDataManager.CurrentHealth;
        int savedMax = PlayerDataManager.SavedMaxHealth;

        if (savedHealth >= 0)
        {
            if (savedHealth <= 0)
            {
                int full = baseHealth;
                SaveCurrentHealth();
                return full;
            }

            int referenceMax = savedMax > 0 ? savedMax : DefaultBaseHealth;

            if (savedMax <= 0 && savedHealth >= referenceMax - 1)
            {
                int full = baseHealth;
                Debug.Log($"[PlayerHealth] 舊存檔滿血({savedHealth})，套用新上限: {full}/{baseHealth}");
                SaveCurrentHealth();
                return full;
            }

            if (referenceMax != baseHealth && referenceMax > 0)
            {
                int scaled = Mathf.Clamp(
                    Mathf.RoundToInt((float)savedHealth * baseHealth / referenceMax),
                    0,
                    baseHealth);
                Debug.Log($"[PlayerHealth] 依比例還原血量: {savedHealth}/{referenceMax} → {scaled}/{baseHealth}");
                SaveCurrentHealth();
                return scaled;
            }

            int clamped = CoalesceZeroSavedHealthToFull(
                Mathf.Clamp(savedHealth, 0, baseHealth),
                "從存檔恢復血量");
            Debug.Log($"[PlayerHealth] 從存檔恢復血量: {clamped}/{baseHealth}");
            SaveCurrentHealth();
            return clamped;
        }

        if (previousCurrent > 0 && previousMax > 0 && previousMax != baseHealth)
        {
            int scaled = Mathf.Clamp(
                Mathf.RoundToInt((float)previousCurrent * baseHealth / previousMax),
                0,
                baseHealth);
            Debug.Log($"[PlayerHealth] 依比例調整血量: {scaled}/{baseHealth}");
            SaveCurrentHealth();
            return scaled;
        }

        int init = baseHealth;
        Debug.Log($"[PlayerHealth] 初始化血量: {init}/{baseHealth}");
        SaveCurrentHealth();
        return init;
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, false);
    }

    /// <param name="ignoreInvincibility">持續傷害（毒胞等）應為 true，不受受擊無敵影響。</param>
    public void TakeDamage(int damage, bool ignoreInvincibility)
    {
        if (isDead) return;
        if (!ignoreInvincibility && isInvincible) return;
        // 套用減傷（四捨五入）
        if (damageReductionPercentActive > 0f)
        {
            float reduced = damage * (1f - Mathf.Clamp01(damageReductionPercentActive));
            damage = Mathf.Max(0, Mathf.RoundToInt(reduced));
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        UpdateHealthUI();
        SaveCurrentHealth();

        if (ignoreInvincibility)
            PlayDoTHitFeedback();
        else
        {
            PlayHitSfx();
            ApplyKnockback();
            PlayDamageFlash();
            isInvincible = true;
            StartCoroutine(InvincibilityCoroutine());
        }

        if (currentHealth <= 0)
            Die();
    }

    void PlayHitSfx()
    {
        if (AudioManager.GetInstance() != null)
            AudioManager.GetInstance().PlaySFX("被攻擊");
        else if (damageSoundClip != null && audioSource != null)
            audioSource.PlayOneShot(damageSoundClip);
    }

    void PlayDoTHitFeedback()
    {
        if (Time.time - _lastDotSfxTime >= dotDamageSfxMinInterval)
        {
            _lastDotSfxTime = Time.time;
            PlayHitSfx();
        }

        if (Time.time - _lastDotFlashTime >= dotFlashMinInterval)
            PlayDoTPulseFlash();
    }

    void ApplyKnockback()
    {
        if (rb != null)
        {
            Vector2 knockbackDir = -transform.up;
            rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
        }
    }

    void EnsureDamageFlashRenderers()
    {
        if (damageFlashRenderers != null && damageFlashRenderers.Length > 0)
            return;
        damageFlashRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if ((damageFlashRenderers == null || damageFlashRenderers.Length == 0) && playerSprite != null)
            damageFlashRenderers = new[] { playerSprite };
    }

    void PlayDamageFlash()
    {
        EnsureDamageFlashRenderers();
        if (damageFlashRenderers == null || damageFlashRenderers.Length == 0)
            return;
        if (damageFlashDuration <= 0f)
            return;
        StopCoroutineSafe(ref dotPulseFlashRoutine);
        StopCoroutineSafe(ref damageFlashRoutine);
        damageFlashRoutine = StartCoroutine(DamageFlashCoroutine());
    }

    /// <summary>中毒等 DoT：短暫紅閃一下即恢復，不疊加整段 DamageFlashCoroutine。</summary>
    void PlayDoTPulseFlash()
    {
        EnsureDamageFlashRenderers();
        if (damageFlashRenderers == null || damageFlashRenderers.Length == 0)
            return;
        if (dotFlashOnSeconds <= 0f)
            return;

        _lastDotFlashTime = Time.time;
        StopCoroutineSafe(ref dotPulseFlashRoutine);
        dotPulseFlashRoutine = StartCoroutine(DoTPulseFlashCoroutine());
    }

    IEnumerator DoTPulseFlashCoroutine()
    {
        var originals = new Color[damageFlashRenderers.Length];
        for (int i = 0; i < damageFlashRenderers.Length; i++)
        {
            if (damageFlashRenderers[i] != null)
                originals[i] = damageFlashRenderers[i].color;
        }

        for (int i = 0; i < damageFlashRenderers.Length; i++)
        {
            if (damageFlashRenderers[i] != null)
                damageFlashRenderers[i].color = dotFlashColor;
        }

        yield return new WaitForSeconds(dotFlashOnSeconds);

        for (int i = 0; i < damageFlashRenderers.Length; i++)
        {
            if (damageFlashRenderers[i] != null)
                damageFlashRenderers[i].color = originals[i];
        }

        dotPulseFlashRoutine = null;
    }

    IEnumerator DamageFlashCoroutine()
    {
        isDamageFlashing = true;
        float elapsed = 0f;
        var originals = new Color[damageFlashRenderers.Length];
        for (int i = 0; i < damageFlashRenderers.Length; i++)
        {
            if (damageFlashRenderers[i] != null)
                originals[i] = damageFlashRenderers[i].color;
        }

        while (elapsed < damageFlashDuration)
        {
            for (int i = 0; i < damageFlashRenderers.Length; i++)
            {
                if (damageFlashRenderers[i] != null)
                    damageFlashRenderers[i].color = damageFlashColor;
            }
            yield return new WaitForSeconds(damageFlashInterval);
            for (int i = 0; i < damageFlashRenderers.Length; i++)
            {
                if (damageFlashRenderers[i] != null)
                    damageFlashRenderers[i].color = originals[i];
            }
            yield return new WaitForSeconds(damageFlashInterval);
            elapsed += damageFlashInterval * 2f;
        }

        for (int i = 0; i < damageFlashRenderers.Length; i++)
        {
            if (damageFlashRenderers[i] != null)
                damageFlashRenderers[i].color = originals[i];
        }

        isDamageFlashing = false;
        damageFlashRoutine = null;
    }

    IEnumerator InvincibilityCoroutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < invincibilityTime)
        {
            yield return null;
            elapsedTime += Time.deltaTime;
        }

        if (!isDamageFlashing && playerSprite != null)
            playerSprite.color = Color.white;
        isInvincible = false;
    }

    // 公開 API：指定秒數的無敵
    public void InvincibleFor(float seconds)
    {
        if (seconds <= 0f) return;
        StopCoroutineSafe(ref invincibleRoutine);
        invincibleRoutine = StartCoroutine(InvincibleForCoroutine(seconds));
    }

    private Coroutine invincibleRoutine;
    private IEnumerator InvincibleForCoroutine(float seconds)
    {
        isInvincible = true;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        if (!isDamageFlashing && playerSprite != null)
            playerSprite.color = Color.white;
        isInvincible = false;
        invincibleRoutine = null;
    }

    // 公開 API：取消無敵狀態 (例如衝刺被打斷時)
    public void CancelInvincible()
    {
        if (invincibleRoutine != null)
        {
            StopCoroutine(invincibleRoutine);
            invincibleRoutine = null;
        }
        isInvincible = false;
        if (!isDamageFlashing && playerSprite != null)
            playerSprite.color = Color.white;
    }

    private void StopCoroutineSafe(ref Coroutine c)
    {
        if (c != null)
        {
            StopCoroutine(c);
            c = null;
        }
    }

    // 公開 API：治療百分比（0~1）
    public void HealPercent(float percent)
    {
        percent = Mathf.Clamp01(percent);
        int add = Mathf.RoundToInt(baseHealth * percent);
        Heal(add);
    }

    // 公開 API：回滿
    public void HealFull()
    {
        currentHealth = baseHealth;
        RefreshHealthUI();
        SaveCurrentHealth();
    }

    // 公開 API：套用減傷百分比一段時間
    public void ApplyDamageReduction(float percent, float duration)
    {
        damageReductionPercentActive = Mathf.Clamp01(percent);
        damageReductionRemaining = Mathf.Max(duration, 0f);
        if (damageReductionRemaining > 0f)
            StartCoroutine(DamageReductionTimer());
    }

    private IEnumerator DamageReductionTimer()
    {
        while (damageReductionRemaining > 0f)
        {
            damageReductionRemaining -= Time.deltaTime;
            yield return null;
        }
        damageReductionPercentActive = 0f;
        damageReductionRemaining = 0f;
    }

    void UpdateHealthUI()
    {
        if (healthBar != null && baseHealth > 0)
            healthBar.fillAmount = (float)currentHealth / baseHealth;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("玩家死亡");

        // 死亡懲罰：金幣與素材扣 30%，消耗品全刪除、任務道具保留
        PlayerDataManager.Coins = Mathf.FloorToInt(PlayerDataManager.Coins * 0.7f);
        InventoryManager.ApplyDeathPenalty(0.3f);
        SaveSystem.SavePlayerData();

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // 依是否持有武器觸發不同死亡動畫
        bool hasWeapon = playerController != null && playerController.HasSpearEquipped;
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Reload");
            animator.SetTrigger(hasWeapon ? "DieArmed" : "DieUnarmed");
        }

        // 鎖定玩家操作
        if (playerController != null)
        {
            playerController.OnPlayerDied();
        }

        // 切場景前先停掉舊場景的 BGM / 循環環境音，避免「海底音效帶回陸地」
        if (AudioManager.GetInstance() != null)
        {
            AudioManager.GetInstance().StopBGM();
            AudioManager.GetInstance().StopAllLoopingSFX();
            AudioManager.GetInstance().StopMovementSFX();
            AudioManager.GetInstance().StopActionSFX();
        }

        PlayerDeathSequence.StartFromPlayerDeath(
            deathLandSceneName,
            deathScreenDuration,
            deathScreenLine1,
            deathScreenLine2,
            deathScreenFont);
    }

    public void Heal(int healAmount)
    {
        // [Achievement Hook] Pre-heal check
        bool wasLowHealth = (float)currentHealth / baseHealth < 0.1f && currentHealth > 0;

        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, baseHealth);

        // [Achievement Hook] Post-heal check
        if (wasLowHealth && AchievementManager.Instance != null)
        {
             // 假設恢復到 10% 以上算成功存活並恢復
             if ((float)currentHealth / baseHealth >= 0.1f)
             {
                 AchievementManager.Instance.AddProgress("Survivor", 1);
             }
        }

        RefreshHealthUI();
        SaveCurrentHealth();
    }

    // 升級後重新計算生命上限（保留當前血量比例，不讓舊 PlayerPrefs 覆蓋場上數值）
    public void RecalculateFromUpgrades()
    {
        int newMax = CalculateMaxHP();
        if (newMax <= 0)
            newMax = DefaultBaseHealth;

        int oldMax = Mathf.Max(1, baseHealth > 0 ? baseHealth : DefaultBaseHealth);
        int oldCurrent = currentHealth > 0 ? currentHealth : Mathf.Clamp(PlayerDataManager.CurrentHealth, 0, oldMax);

        baseHealth = newMax;
        if (oldCurrent > 0 && oldMax > 0)
            currentHealth = oldMax == newMax
                ? Mathf.Clamp(oldCurrent, 0, baseHealth)
                : Mathf.Clamp(Mathf.RoundToInt((float)oldCurrent * baseHealth / oldMax), 0, baseHealth);
        else
            currentHealth = ResolveHealthAfterMaxChange(oldMax, oldCurrent);

        if (currentHealth <= 0)
            currentHealth = baseHealth;

        UpdateHealthUI();
        SaveCurrentHealth();
    }

    // 保存當前血量到 PlayerDataManager
    public void SaveCurrentHealth()
    {
        PlayerDataManager.CurrentHealth = currentHealth;
        PlayerDataManager.SavedMaxHealth = baseHealth;
        PlayerDataManager.Save();
    }

    // 獲取當前血量（供外部調用）
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    // 獲取最大血量（供外部調用）
    public int GetMaxHealth()
    {
        return baseHealth;
    }

    // 設置血量（供外部調用，用於場景切換時同步）
    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, baseHealth);
        UpdateHealthUI();
        SaveCurrentHealth();
        Debug.Log($"[PlayerHealth] 設置血量: {currentHealth}/{baseHealth}");
    }
}
