using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 街機式釣魚流程；動畫與結算皆在 UI 上（<see cref="FishingAnimationView"/>、<see cref="FishingSettlementUI"/>）。
/// </summary>
[RequireComponent(typeof(FishingSfxPlayer))]
public class FishingManager : MonoBehaviour
{
    public static FishingManager Instance { get; private set; }

    public bool IsFishing { get; private set; }
    public FishingSessionResult LastSessionResult { get; private set; }

    [Header("UI 演出")]
    [SerializeField] private FishingAnimationView fishingAnimation;
    [SerializeField] private FishingSettlementUI fishingSettlement;
    [SerializeField] private FishingQTE fishingQTE;
    [SerializeField] private FishingUIController fishingUI;

    [Header("玩家（流程中鎖移動）")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LandPlayerController landPlayer;
    [SerializeField] private Rigidbody2D playerRb;

    [Header("機率")]
    [Range(0f, 1f)]
    [SerializeField] private float realBiteChance = 0.7f;

    [Header("等待")]
    [SerializeField] private float waitDelayMin = 2f;
    [SerializeField] private float waitDelayMax = 5f;

    [Header("動畫狀態名（UI Animator / FishingAnimator）")]
    [Tooltip("須與 Animator Controller 內 State 名稱一致；釣魚.controller 預設為「釣魚」非「拋竿」")]
    [SerializeField] private string animCast = "釣魚";
    [SerializeField] private string animWait = "等待";
    [SerializeField] private string animStruggleQte = "掙扎QTE";
    [SerializeField] private string animPullSuccess = "拉起有";
    [SerializeField] private string animPullFail = "拉起無";

    [Header("音效")]
    [SerializeField] private FishingSfxPlayer fishingSfx;
    [Tooltip("勾選時由程式在拋竿開始時播放；不勾則改在「釣魚」動畫 Clip 的 Animation Event 呼叫 PlayCastSfx")]
    [SerializeField] private bool playCastSfxFromCode;
    [SerializeField] private string sfxCast = "拋竿";
    [SerializeField] private string sfxBite = "咬魚餌水面波動";
    [SerializeField] private string sfxReel = "拉線";

    [Header("相機震動")]
    [SerializeField] private float biteShakeDuration = 0.1f;
    [SerializeField] private float biteShakeMagnitude = 0.08f;
    [SerializeField] private float nuclearShakeMultiplier = 2f;

    [Header("垃圾")]
    [SerializeField] private TrashData trashData;

    Coroutine _routine;
    bool _realBite;
    TrashData.TrashEntry _pendingTrash;
    Action<FishingSessionResult> _onSessionComplete;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[FishingManager] 場景內有多個實例，將覆寫 Instance。");

        Instance = this;

        if (fishingUI == null)
            fishingUI = FishingUIController.Instance;

        if (fishingSfx == null)
            fishingSfx = GetComponent<FishingSfxPlayer>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>由 <see cref="FishingUIController"/> 在玩家按下「開始釣魚」後呼叫。</summary>
    public void RunFishingSession(Action<FishingSessionResult> onComplete)
    {
        if (IsFishing)
        {
            onComplete?.Invoke(LastSessionResult);
            return;
        }

        if (!ResolvePlayerReferences())
        {
            onComplete?.Invoke(default);
            return;
        }

        _onSessionComplete = onComplete;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(FishingRoutine());
    }

    public void CancelFishing()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        EndFishingSession();
    }

    bool ResolvePlayerReferences()
    {
        if (landPlayer != null && playerRb != null)
            return true;

        var playerGo = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGo == null)
        {
            Debug.LogWarning("[FishingManager] 找不到 Tag=Player 的角色。");
            return false;
        }

        if (landPlayer == null)
            landPlayer = playerGo.GetComponent<LandPlayerController>();
        if (playerRb == null)
            playerRb = playerGo.GetComponent<Rigidbody2D>();

        return landPlayer != null || playerRb != null;
    }

    bool GetPlayerFlipX()
    {
        var playerGo = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGo == null) return false;

        var sr = playerGo.GetComponent<SpriteRenderer>();
        return sr != null && sr.flipX;
    }

    IEnumerator FishingRoutine()
    {
        IsFishing = true;
        LastSessionResult = default;
        SetPlayerControl(false);

        fishingQTE?.Hide();
        fishingSettlement?.Hide();

        if (fishingAnimation != null)
        {
            fishingAnimation.ConfigureSession(animWait);
            fishingAnimation.PrepareSession();
        }

        ResolveFishingUI()?.EnterGameplayPhase();

        if (fishingAnimation != null)
            fishingAnimation.Show(GetPlayerFlipX());

        if (playCastSfxFromCode)
            PlayCastSfx();

        int earlyPressCount = 0;
        void CountEarlyPress()
        {
            if (Input.GetKeyDown(KeyCode.E))
                earlyPressCount++;
        }

        float waitSeconds = UnityEngine.Random.Range(waitDelayMin, waitDelayMax);
        if (fishingAnimation != null)
            yield return fishingAnimation.RunCastThenWaitLoop(animCast, animWait, waitSeconds, CountEarlyPress);
        else
        {
            float castEnd = Time.unscaledTime + 0.5f;
            while (Time.unscaledTime < castEnd)
            {
                CountEarlyPress();
                yield return null;
            }

            float waitEnd = Time.unscaledTime + waitSeconds;
            while (Time.unscaledTime < waitEnd)
            {
                CountEarlyPress();
                yield return null;
            }
        }

        _realBite = UnityEngine.Random.value <= realBiteChance;
        _pendingTrash = null;
        if (!_realBite && trashData != null)
            _pendingTrash = trashData.RollRandom();

        fishingAnimation?.TransitionToState(animStruggleQte);

        bool nuclear = _pendingTrash != null && _pendingTrash.isNuclearWasteBarrel;
        float shakeMag = biteShakeMagnitude * (nuclear ? nuclearShakeMultiplier : 1f);
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(biteShakeDuration, shakeMag);

        var qteResult = new QTEResult();
        yield return RunStrugglePhase(qteResult, nuclear, earlyPressCount, CountEarlyPress);

        if (!qteResult.Success)
        {
            fishingAnimation?.PlayState(animPullFail);
            if (fishingAnimation != null)
                yield return fishingAnimation.WaitForState(animPullFail);

            if (fishingSettlement != null)
                yield return fishingSettlement.ShowFailureSettlement();

            LastSessionResult = new FishingSessionResult
            {
                QteSuccess = false,
                GotFish = false,
                Title = "沒釣到",
                Detail = "QTE 失敗"
            };

            EndFishingSession();
            yield break;
        }

        fishingAnimation?.PlayState(animPullSuccess);
        PlayReelSfx();
        if (fishingAnimation != null)
            yield return fishingAnimation.WaitForState(animPullSuccess);
        else
            yield return new WaitForSecondsRealtime(0.5f);
        StopReelSfxInternal();

        fishingAnimation?.Hide();

        if (_realBite)
        {
            var dropTable = fishingSettlement != null ? fishingSettlement.MonsterDropTable : null;
            var drop = dropTable != null ? dropTable.RollRandom() : null;
            if (fishingSettlement != null)
                yield return fishingSettlement.ShowFishSettlement(drop);
            if (drop != null)
            {
                LastSessionResult = BuildResultFromFish(drop);
            }
        }
        else if (fishingSettlement != null)
        {
            yield return fishingSettlement.ShowTrashSettlement(_pendingTrash);
            LastSessionResult = BuildResultFromTrash(_pendingTrash);
        }

        EndFishingSession();
    }

    static FishingSessionResult BuildResultFromFish(MonsterDropTable.DropEntry drop)
    {
        string itemId = MonsterDropTable.ResolveItemId(drop);
        string name = itemId;
        Sprite icon = null;
        if (InventoryManager.Instance != null)
        {
            var data = InventoryManager.Instance.ResolveItemData(itemId);
            if (data != null)
            {
                name = data.itemName;
                icon = data.icon;
            }
        }

        return new FishingSessionResult
        {
            QteSuccess = true,
            GotFish = true,
            Title = "釣到了！",
            Detail = $"{name} x{Mathf.Max(1, drop.amount)}",
            ItemId = itemId,
            ItemDisplayName = name,
            ItemIcon = icon,
            ItemAmount = Mathf.Max(1, drop.amount)
        };
    }

    static FishingSessionResult BuildResultFromTrash(TrashData.TrashEntry trash)
    {
        if (trash == null) return default;

        return new FishingSessionResult
        {
            QteSuccess = true,
            GotFish = false,
            Title = "釣到垃圾",
            Detail = trash.displayName,
            ItemDisplayName = trash.displayName,
            ItemIcon = trash.icon,
            ItemAmount = 1
        };
    }

    /// <summary>先進入掙扎動畫，再啟動 QTE（避免 QTE 計時與按鍵在畫面出現前就生效）。</summary>
    IEnumerator RunStrugglePhase(
        QTEResult qteResult,
        bool isNuclearWaste,
        int earlyPressCount,
        System.Action onWhileWaitingStruggleAnim)
    {
        PlayBiteLoopSfx();

        if (fishingAnimation != null)
            yield return fishingAnimation.WaitUntilStateEntered(animStruggleQte, onWhileWaitingStruggleAnim);
        else
        {
            float end = Time.unscaledTime + 0.35f;
            while (Time.unscaledTime < end)
            {
                onWhileWaitingStruggleAnim?.Invoke();
                yield return null;
            }
        }

        StopBiteLoopSfx();

        if (earlyPressCount > 0)
        {
            qteResult.Success = false;
            qteResult.PressCount = earlyPressCount;
            qteResult.RequiredPressCount = fishingQTE != null
                ? fishingQTE.GetRequiredPressCount(isNuclearWaste)
                : 6;
            fishingQTE?.Hide();
            yield break;
        }

        if (fishingQTE != null)
            yield return fishingQTE.RunQTE(qteResult, isNuclearWaste);
        else
            qteResult.Success = Input.GetKeyDown(KeyCode.E);
    }

    void EndFishingSession()
    {
        StopBiteLoopSfx();
        StopReelSfxInternal();
        IsFishing = false;
        _routine = null;
        SetPlayerControl(true);
        fishingQTE?.Hide();
        fishingAnimation?.Hide();
        ResolveFishingUI()?.ExitGameplayPhase();

        var complete = _onSessionComplete;
        _onSessionComplete = null;
        complete?.Invoke(LastSessionResult);
    }

    FishingUIController ResolveFishingUI()
    {
        if (fishingUI != null)
            return fishingUI;
        return FishingUIController.Instance;
    }

    void SetPlayerControl(bool enabled)
    {
        if (landPlayer != null)
            landPlayer.enabled = enabled;

        if (playerRb != null && !enabled)
            playerRb.velocity = Vector2.zero;
    }

    void PlayCastSfx()
    {
        if (TryPlayViaFishingSfx(sfxCast, oneShot: true))
            return;

        AudioManager.GetInstance()?.PlaySFX(sfxCast);
    }

    void PlayReelSfx()
    {
        if (TryPlayViaFishingSfx(sfxReel, stoppable: true))
            return;

        AudioManager.GetInstance()?.PlayActionSFX(sfxReel);
    }

    void StopReelSfxInternal()
    {
        if (fishingSfx != null)
            fishingSfx.StopStoppable();
        else
            AudioManager.GetInstance()?.StopActionSFX();
    }

    void PlayBiteLoopSfx()
    {
        if (TryPlayViaFishingSfx(sfxBite, loop: true))
            return;

        var am = AudioManager.GetInstance();
        if (am == null)
            return;

        string n = sfxBite.Trim();
        var clip = am.TryResolveSfxClip(n);
        if (clip != null)
            am.PlayLoopingSFXFromClip(n, clip, 1f, 1f);
        else
            am.PlayLoopingSFX(n);
    }

    void StopBiteLoopSfx()
    {
        if (fishingSfx != null)
            fishingSfx.StopLoop();
        else
            AudioManager.GetInstance()?.StopLoopingSFX(sfxBite);
    }

    bool TryPlayViaFishingSfx(string clipName, bool oneShot = false, bool loop = false, bool stoppable = false)
    {
        if (fishingSfx == null || string.IsNullOrWhiteSpace(clipName))
            return false;

        if (oneShot)
            return fishingSfx.PlayOneShot(clipName);

        if (loop)
            return fishingSfx.PlayLoop(clipName);

        if (stoppable)
            return fishingSfx.PlayStoppable(clipName);

        return fishingSfx.PlayOneShot(clipName);
    }
}
