using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 釣魚 UI 動畫演出（對齊 <see cref="GambleCupSequenceView"/>）：
/// Animator 掛在 <see cref="FishingRoot"/> 底下的 UI 物件，須使用 <see cref="Image"/>，勿用 <see cref="SpriteRenderer"/>。
/// </summary>
[DisallowMultipleComponent]
public class FishingAnimationView : MonoBehaviour
{
    [Header("UI 演出根（GameplayPanel 或子物件）")]
    [SerializeField] private GameObject visualRoot;

    [Header("顯示（Canvas 下請用 Image）")]
    [SerializeField] private Image displayImage;

    [Header("Animator（與 FishingAnimator.controller 同層）")]
    [SerializeField] private Animator uiAnimator;
    [SerializeField] private int animatorLayerIndex;
    [SerializeField] private float stateWaitTimeoutSeconds = 12f;
    [SerializeField] private bool useUnscaledTime = true;
    [Tooltip("狀態切換淡入時間；0 = 拋竿結束後立刻切等待")]
    [SerializeField] private float stateCrossFadeSeconds = 0f;

    [Header("開釣前待命（可選）")]
    [SerializeField] private string waitStateName = "";
    [Tooltip("水平翻轉（依玩家面向）；留空則不翻轉")]
    [SerializeField] private RectTransform flipTarget;

    bool _sessionActive;

    /// <summary>程式慣用名 → Controller 內實際 State 名。</summary>
    static string ResolveStateAlias(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
            return stateName;

        switch (stateName.Trim())
        {
            case "拋竿":
                return "釣魚";
            default:
                return stateName.Trim();
        }
    }

    bool IsAnimatorReady =>
        uiAnimator != null
        && uiAnimator.isActiveAndEnabled
        && uiAnimator.gameObject.activeInHierarchy;

    void Awake()
    {
        if (visualRoot == null && uiAnimator != null)
            visualRoot = uiAnimator.gameObject;

        if (uiAnimator == null && visualRoot != null)
            uiAnimator = visualRoot.GetComponent<Animator>();

        if (displayImage == null && visualRoot != null)
            displayImage = visualRoot.GetComponent<Image>();

        ValidateCanvasDisplaySetup();

        HideImmediate();
    }

    void ValidateCanvasDisplaySetup()
    {
        if (visualRoot == null) return;

        var sr = visualRoot.GetComponent<SpriteRenderer>();
        if (sr != null && displayImage == null)
        {
            Debug.LogWarning(
                "[FishingAnimationView] 「" + visualRoot.name + "」掛了 Sprite Renderer 且在 Canvas 底下，" +
                "Game 畫面通常看不到。請移除 Sprite Renderer，改用 UI Image，並讓 Animation Clip 綁定 Image.sprite。",
                visualRoot);
        }

        if (displayImage != null && displayImage.material != null
            && displayImage.material.name.Contains("Lit"))
        {
            Debug.LogWarning(
                "[FishingAnimationView] UI Image 使用了光照材質，若場景無 2D 光可能全黑。建議 Material 留空（預設 UI）或使用 Sprites/Default。",
                displayImage);
        }
    }

    void OnEnable()
    {
        if (!_sessionActive)
            HoldAtWaitState();
    }

    public void ConfigureSession(string waitAnimatorStateName)
    {
        if (!string.IsNullOrWhiteSpace(waitAnimatorStateName))
            waitStateName = waitAnimatorStateName.Trim();
    }

    /// <summary>在開啟 GameplayPanel 前呼叫，避免 OnEnable 把 Animator 定格在待命。</summary>
    public void PrepareSession()
    {
        _sessionActive = true;
        EnsureAnimatorPlaying();
    }

    public void HoldAtWaitState()
    {
        if (!IsAnimatorReady || string.IsNullOrEmpty(waitStateName))
            return;

        string state = ResolveStateAlias(waitStateName);
        uiAnimator.speed = 0f;
        uiAnimator.Play(state, animatorLayerIndex, 0f);
        uiAnimator.Update(0f);
    }

    public void Show(bool flipX)
    {
        _sessionActive = true;
        if (visualRoot != null)
            visualRoot.SetActive(true);

        if (uiAnimator != null)
        {
            uiAnimator.enabled = true;
            uiAnimator.gameObject.SetActive(true);
            uiAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            EnsureAnimatorPlaying();
        }

        ApplyFlip(flipX);
    }

    public void Hide()
    {
        _sessionActive = false;
        HideImmediate();
    }

    void HideImmediate()
    {
        if (visualRoot != null)
            visualRoot.SetActive(false);
    }

    void ApplyFlip(bool flipX)
    {
        if (flipTarget == null)
            return;

        var scale = flipTarget.localScale;
        scale.x = Mathf.Abs(scale.x) * (flipX ? -1f : 1f);
        flipTarget.localScale = scale;
    }

    public void PlayState(string stateName)
    {
        TransitionToState(stateName);
    }

    /// <summary>切換 Animator 狀態（可 CrossFade）。</summary>
    void EnsureAnimatorPlaying()
    {
        if (uiAnimator != null)
            uiAnimator.speed = 1f;
    }

    public void TransitionToState(string stateName)
    {
        if (!IsAnimatorReady || string.IsNullOrEmpty(stateName))
            return;

        stateName = ResolveStateAlias(stateName);
        EnsureAnimatorPlaying();

        if (stateCrossFadeSeconds > 0f)
            uiAnimator.CrossFade(stateName, stateCrossFadeSeconds, animatorLayerIndex, 0f);
        else
            uiAnimator.Play(stateName, animatorLayerIndex, 0f);

        uiAnimator.Update(0f);
    }

    /// <summary>拋竿播完 → 立刻進入等待並循環，直到 duration 結束（再由流程切掙扎QTE）。</summary>
    public IEnumerator RunCastThenWaitLoop(
        string castStateName,
        string waitStateName,
        float waitLoopDurationSeconds,
        Action onDuringWaitFrame = null)
    {
        waitLoopDurationSeconds = Mathf.Max(0f, waitLoopDurationSeconds);

        if (uiAnimator == null || string.IsNullOrEmpty(castStateName))
        {
            if (!string.IsNullOrEmpty(waitStateName))
                yield return PlayStateLoopingForDuration(waitStateName, waitLoopDurationSeconds, onDuringWaitFrame);
            else if (waitLoopDurationSeconds > 0f)
                yield return WaitSecondsWithCallback(waitLoopDurationSeconds, onDuringWaitFrame);
            yield break;
        }

        yield return null;

        if (!IsAnimatorReady)
        {
            Debug.LogWarning("[FishingAnimationView] Animator 未啟用，無法播放拋竿。請確認 FishingAnimHost 已顯示且掛有 Animator。", this);
            yield break;
        }

        string cast = ResolveStateAlias(castStateName);
        TransitionToState(cast);
        yield return WaitForState(cast);

        if (string.IsNullOrEmpty(waitStateName))
        {
            if (waitLoopDurationSeconds > 0f)
                yield return new WaitForSecondsRealtime(waitLoopDurationSeconds);
            yield break;
        }

        string wait = ResolveStateAlias(waitStateName);
        TransitionToState(wait);
        yield return PlayStateLoopingForDuration(wait, waitLoopDurationSeconds, onDuringWaitFrame);
    }

    /// <summary>在指定秒數內循環播放狀態（每播完一輪重新 Play）。</summary>
    public IEnumerator PlayStateLoopingForDuration(
        string stateName,
        float durationSeconds,
        Action onEachFrame = null)
    {
        durationSeconds = Mathf.Max(0f, durationSeconds);

        if (uiAnimator == null || string.IsNullOrEmpty(stateName))
        {
            if (durationSeconds > 0f)
                yield return WaitSecondsWithCallback(durationSeconds, onEachFrame);
            yield break;
        }

        string resolved = ResolveStateAlias(stateName);
        TransitionToState(resolved);
        float end = Now() + durationSeconds;

        while (Now() < end)
        {
            onEachFrame?.Invoke();

            if (!IsAnimatorReady)
                yield break;

            EnsureAnimatorPlaying();

            var info = uiAnimator.GetCurrentAnimatorStateInfo(animatorLayerIndex);
            if (!info.IsName(resolved))
            {
                TransitionToState(resolved);
            }
            else if (info.normalizedTime >= 0.99f && !uiAnimator.IsInTransition(animatorLayerIndex))
            {
                TransitionToState(resolved);
            }

            yield return null;
        }
    }

    static IEnumerator WaitSecondsWithCallback(float seconds, Action onEachFrame)
    {
        float end = Time.unscaledTime + Mathf.Max(0f, seconds);
        while (Time.unscaledTime < end)
        {
            onEachFrame?.Invoke();
            yield return null;
        }
    }

    /// <summary>等到 Animator 進入指定狀態（不要求播完一輪）。</summary>
    public IEnumerator WaitUntilStateEntered(string stateName, Action onEachFrame = null)
    {
        if (uiAnimator == null || string.IsNullOrEmpty(stateName))
        {
            yield return null;
            yield break;
        }

        string resolved = ResolveStateAlias(stateName);
        float deadline = Now() + Mathf.Max(0.5f, stateWaitTimeoutSeconds);

        while (Now() < deadline)
        {
            onEachFrame?.Invoke();

            if (!uiAnimator.isActiveAndEnabled)
                yield break;

            if (uiAnimator.GetCurrentAnimatorStateInfo(animatorLayerIndex).IsName(resolved))
                yield break;

            yield return null;
        }

        Debug.LogWarning(
            $"[FishingAnimationView] 逾時未進入狀態「{resolved}」。請確認 Controller 與 Transition。",
            this);
    }

    float Now() => useUnscaledTime ? Time.unscaledTime : Time.time;

    public IEnumerator WaitForState(string stateName)
    {
        if (uiAnimator == null || string.IsNullOrEmpty(stateName))
        {
            yield return new WaitForSecondsRealtime(0.5f);
            yield break;
        }

        string resolved = ResolveStateAlias(stateName);
        yield return WaitStateComplete(uiAnimator, animatorLayerIndex, resolved);
    }

    IEnumerator WaitStateComplete(Animator anim, int layer, string stateName)
    {
        stateName = ResolveStateAlias(stateName);
        float deadline = Now() + Mathf.Max(1f, stateWaitTimeoutSeconds);
        bool entered = false;

        while (Now() < deadline)
        {
            if (!anim.isActiveAndEnabled)
                yield break;

            if (anim.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
            {
                entered = true;
                break;
            }

            yield return null;
        }

        if (!entered)
        {
            Debug.LogWarning(
                $"[FishingAnimationView] 無法進入 Animator 狀態「{stateName}」。請確認 Controller 內 State 名稱與 FishingManager 欄位一致。",
                this);
            yield break;
        }

        while (Now() < deadline)
        {
            var info = anim.GetCurrentAnimatorStateInfo(layer);
            if (info.IsName(stateName) && info.normalizedTime >= 0.99f && !anim.IsInTransition(layer))
                yield break;
            yield return null;
        }
    }
}
