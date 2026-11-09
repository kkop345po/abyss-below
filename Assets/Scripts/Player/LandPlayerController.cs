using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class LandPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    private float initialMoveSpeed;

    [Header("Movement SFX")]
    [Tooltip("須與 AudioManager 的 sfxSounds 名稱一致（陸地專用，與水中 PlayerController 分開）。")]
    [SerializeField] private string landWalkSoundName = "走路新";
    [SerializeField] private float walkSoundInterval = 0.4f;
    private float walkSoundTimer;
    private bool _wasMovingForSfx;

    [Header("陸地循環環境音（與 LandHarborAmbientZone 搭配）")]
    [Tooltip("一般陸地循環環境音（例如海浪聲）。")]
    [SerializeField] private string landAmbientName = "海浪聲";
    [Tooltip("進入港口 Trigger 區時改播的循環環境音（須與 landAmbientName 不同鍵）。")]
    [SerializeField] private string harborLandAmbientName = "港口邊";

    private int _harborAmbientZoneCount;

    [Header("除錯（陸地／港口環境音）")]
    [SerializeField] private bool debugLandHarborAmbient;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerSlow playerSlow;

    private float horizontalInput;

    private readonly string ANIM_IS_MOVING = "IsMoving";

    /// <summary>陸地角色永不在水中；供 <see cref="LandHarborAmbientZone"/> 與除錯語意一致。</summary>
    public bool IsInWater => false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialMoveSpeed = moveSpeed;
        playerSlow = GetComponent<PlayerSlow>();
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
        RecalculateFromUpgrades();
    }

    private static float GetSquidAbilityMultiplier()
    {
        return BuffManager.Instance != null ? BuffManager.Instance.CurrentAbilityMultiplier : 1f;
    }

    private void Start()
    {
        RecalculateFromUpgrades();
        StartCoroutine(CoInitLandAmbient());
    }

    private IEnumerator CoInitLandAmbient()
    {
        yield return new WaitForSeconds(0.1f);
        ApplyLandAmbientLooping();
    }

    /// <summary>與水中 PlayerController 一致：依速度升級套用移動速度。</summary>
    public void RecalculateFromUpgrades()
    {
        float[] bonus = { 0.01f, 0.03f, 0.05f, 0.07f, 0.09f };
        float speedBoost = 1f + bonus[Mathf.Clamp(PlayerDataManager.SpeedLevel, 0, bonus.Length - 1)];
        float abilityMult = GetSquidAbilityMultiplier();
        moveSpeed = initialMoveSpeed * speedBoost * abilityMult;
    }

    void DbgHarbor(string msg)
    {
        if (debugLandHarborAmbient)
            Debug.Log($"[LandPlayerController 港口環境音] {msg}", this);
    }

    /// <summary>供 <see cref="LandHarborAmbientZone"/> 呼叫（陸地專用）。</summary>
    public void NotifyEnterHarborAmbientZone()
    {
        _harborAmbientZoneCount++;
        DbgHarbor($"NotifyEnter：區域計數={_harborAmbientZoneCount}");
        ApplyLandAmbientLooping();
    }

    /// <summary>供 <see cref="LandHarborAmbientZone"/> 呼叫（陸地專用）。</summary>
    public void NotifyExitHarborAmbientZone()
    {
        _harborAmbientZoneCount = Mathf.Max(0, _harborAmbientZoneCount - 1);
        DbgHarbor($"NotifyExit：區域計數={_harborAmbientZoneCount}");
        ApplyLandAmbientLooping();
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
                DbgHarbor("Apply：區域計數>0 但 harborLandAmbientName 為空。");
        }
        else if (!string.IsNullOrEmpty(landAmbientName))
        {
            DbgHarbor($"Apply：播一般陸地循環「{landAmbientName}」");
            am.PlayLoopingSFX(landAmbientName);
        }
        else
            DbgHarbor("Apply：未設定 landAmbientName，未播循環環境音。");
    }

    /// <summary>選單關閉後重套陸地循環環境音（不切 BGM）。</summary>
    public void RefreshEnvironmentLoopingAfterUiClosed() => ApplyLandAmbientLooping();

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            if (_wasMovingForSfx)
            {
                AudioManager.GetInstance()?.StopMovementSFX();
                _wasMovingForSfx = false;
            }
            walkSoundTimer = 0f;
            return;
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");

        animator.SetBool(ANIM_IS_MOVING, horizontalInput != 0);

        if (horizontalInput != 0)
            spriteRenderer.flipX = horizontalInput < 0;

        bool moving = horizontalInput != 0;
        if (moving)
        {
            walkSoundTimer -= Time.deltaTime;
            if (walkSoundTimer <= 0f)
            {
                var am = AudioManager.GetInstance();
                if (am != null && !string.IsNullOrEmpty(landWalkSoundName))
                    am.PlayMovementSFX(landWalkSoundName);
                walkSoundTimer = walkSoundInterval;
            }
        }
        else
        {
            if (_wasMovingForSfx)
                AudioManager.GetInstance()?.StopMovementSFX();
            walkSoundTimer = 0f;
        }
        _wasMovingForSfx = moving;
    }

    [System.Obsolete]
    private void FixedUpdate()
    {
        float slowMult = playerSlow != null ? playerSlow.CurrentSpeedMultiplier : 1f;
        rb.velocity = new Vector2(horizontalInput * moveSpeed * slowMult, rb.velocity.y);
    }
}
