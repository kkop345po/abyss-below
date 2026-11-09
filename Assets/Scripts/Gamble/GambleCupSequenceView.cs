using System.Collections;
using UnityEngine;

/// <summary>
/// 骰鍾／開蓋類 Animator 三段式：<b>搖骰</b> → <b>開蓋</b>（播完後 <see cref="Animator.speed"/> = 0 停在最後一格）→
/// 等 <see cref="GambleUIController"/> 結算確認後再播 <b>關蓋</b>（<see cref="PlayPostJudgePresentation"/>）。
/// Animator 建議設為 <see cref="AnimatorUpdateMode.UnscaledDeltaTime"/> 以便暫停遊戲時仍播放。
/// </summary>
public class GambleCupSequenceView : DiceRollViewBase
{
    [Header("Animator")]
    [SerializeField] private Animator cupAnimator;
    [SerializeField] private int animatorLayerIndex;
    [Tooltip("與 Controller 裡 State 名稱一致（建議各階一段 Clip）")]
    [SerializeField] private string shakeStateName = "shake";
    [SerializeField] private string openStateName = "open";
    [SerializeField] private string closeStateName = "close";
    [Tooltip("每局開始時先切到此狀態再播搖骰；留空則只做 speed=1，不強制切狀態")]
    [SerializeField] private string optionalResetStateName = "";

    [Header("開賭前不要自動播（重要）")]
    [Tooltip("待命狀態名稱須與 Animator 內完全一致（常見 idle）。該 State 請勿加「自動接到 shake」的 Transition，或刪除該線。")]
    [SerializeField] private string waitStateName = "idle";

    [Header("可選")]
    [SerializeField] private GameObject visualRoot;
    [Tooltip("逾時防卡死（秒，使用與下方相同的時間基準）")]
    [SerializeField] private float stateWaitTimeoutSeconds = 45f;
    [SerializeField] private bool useUnscaledTime = true;
    [Tooltip("關蓋播畢後關閉 visualRoot（再玩一局時會在 PlayDiceAnimation 開回）")]
    [SerializeField] private bool deactivateRootAfterClose = true;

    void Awake()
    {
        HoldAtWaitState();
    }

    void OnEnable()
    {
        HoldAtWaitState();
    }

    /// <summary>開賭前卡住待命：不消耗 Exit Time，自動迴圈不會往前走。</summary>
    public void HoldAtWaitState()
    {
        // 強制待命：避免 Animator Controller 在物件啟用時就自動從 idle 走到 shake，
        // 導致「動畫先開始、音效晚一拍」的體感落差。
        if (cupAnimator == null) return;
        if (string.IsNullOrEmpty(waitStateName)) return;
        cupAnimator.speed = 0f;
        cupAnimator.Play(waitStateName, animatorLayerIndex, 0f);
        cupAnimator.Update(0f);
    }

    public override IEnumerator PlayDiceAnimation(int d1, int d2, int d3)
    {
        if (cupAnimator == null)
            cupAnimator = GetComponent<Animator>();
        if (cupAnimator == null)
        {
            Debug.LogWarning("[GambleCupSequenceView] 找不到 Animator：請在 Inspector 指定 Cup Animator，或掛在与本腳本同一物件上。");
            yield break;
        }

        cupAnimator.enabled = true;
        cupAnimator.gameObject.SetActive(true);

        if (visualRoot != null)
            visualRoot.SetActive(true);

        cupAnimator.speed = 1f;

        if (!string.IsNullOrEmpty(optionalResetStateName))
        {
            cupAnimator.Play(optionalResetStateName, animatorLayerIndex, 0f);
            cupAnimator.Update(0.001f);
        }

        if (!string.IsNullOrEmpty(shakeStateName))
        {
            cupAnimator.Play(shakeStateName, animatorLayerIndex, 0f);
            // 讓 Animator 立刻套用到 shake 狀態，避免音效與動畫起始幀有間隔
            cupAnimator.Update(0f);
            // 搖骰子音效：只在「開始搖」與搖的段落播放；收回去（close）不播
            AudioManager.GetInstance()?.PlayDiceGambleSfx();
            yield return WaitStateComplete(cupAnimator, animatorLayerIndex, shakeStateName);
        }

        if (!string.IsNullOrEmpty(openStateName))
        {
            cupAnimator.Play(openStateName, animatorLayerIndex, 0f);
            yield return WaitStateComplete(cupAnimator, animatorLayerIndex, openStateName);
        }

        cupAnimator.speed = 0f;
    }

    public override IEnumerator PlayPostJudgePresentation()
    {
        if (cupAnimator == null)
            yield break;

        cupAnimator.speed = 1f;

        if (!string.IsNullOrEmpty(closeStateName))
        {
            cupAnimator.Play(closeStateName, animatorLayerIndex, 0f);
            yield return WaitStateComplete(cupAnimator, animatorLayerIndex, closeStateName);
        }

        if (deactivateRootAfterClose && visualRoot != null)
            visualRoot.SetActive(false);
        else
            HoldAtWaitState();
    }

    IEnumerator WaitStateComplete(Animator anim, int layer, string stateName)
    {
        float now() => useUnscaledTime ? Time.unscaledTime : Time.time;
        float deadline = now() + Mathf.Max(1f, stateWaitTimeoutSeconds);
        float failEnterBy = now() + 0.85f;

        while (now() < deadline)
        {
            if (anim.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
                break;
            if (now() >= failEnterBy)
            {
                var cur = anim.GetCurrentAnimatorStateInfo(layer);
                Debug.LogWarning(
                    $"[GambleCupSequenceView] 約 0.85 秒內沒進入 State「{stateName}」。目前 Layer{layer} 狀態 shortNameHash={cur.shortNameHash}（請在 Inspector 把 Shake/Open/Close 名稱改成與 Animator 裡「完全一致」，例如小寫 shake）。");
                yield break;
            }
            yield return null;
        }

        while (now() < deadline)
        {
            var info = anim.GetCurrentAnimatorStateInfo(layer);
            if (info.IsName(stateName) && info.normalizedTime >= 0.99f && !anim.IsInTransition(layer))
                yield break;
            yield return null;
        }

        Debug.LogWarning($"[GambleCupSequenceView] State「{stateName}」在 {stateWaitTimeoutSeconds}s 內未播完，流程中斷。請檢查 Clip 長度、Loop、Transition。");
    }
}
