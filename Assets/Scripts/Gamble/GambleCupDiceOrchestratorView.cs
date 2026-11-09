using System.Collections;
using UnityEngine;

/// <summary>
/// 同時跑「骰鍾三段」與「骰子點數演出」：<see cref="GambleCupSequenceView"/> 會在開蓋末停格，
/// 結算確認後才關蓋；骰子可與開蓋並行，或改為等 <see cref="GambleDiceRevealBridge.ReleaseDice"/>（Animation Event）後才播。
/// </summary>
public class GambleCupDiceOrchestratorView : DiceRollViewBase
{
    [SerializeField] private GambleCupSequenceView cupSequence;
    [SerializeField] private DiceRollViewBase dicePresentation;

    [Header("骰子出現時機（可選）")]
    [Tooltip("勾選後：骰子演出延後到開蓋 Animation Clip 裡的 Event 呼叫 ReleaseDice 之後才開始")]
    [SerializeField] private bool waitForRevealAnimationEvent;
    [SerializeField] private GambleDiceRevealBridge revealBridge;
    [Tooltip("有等事件時，超過秒數仍沒觸發則強制開始骰子並印警告（避免卡死）")]
    [SerializeField] private float revealEventFallbackSeconds = 12f;

    public override IEnumerator PlayDiceAnimation(int d1, int d2, int d3)
    {
        bool cupDone = false;
        bool diceDone = false;

        revealBridge?.ResetGate();

        if (cupSequence != null)
            StartCoroutine(RunThenFlag(cupSequence.PlayDiceAnimation(d1, d2, d3), () => cupDone = true));
        else
            cupDone = true;

        if (dicePresentation != null)
            StartCoroutine(RunDiceWithOptionalRevealWait(d1, d2, d3, () => diceDone = true));
        else
            diceDone = true;

        yield return new WaitUntil(() => cupDone && diceDone);
    }

    public override IEnumerator PlayPostJudgePresentation()
    {
        if (cupSequence != null)
            yield return StartCoroutine(cupSequence.PlayPostJudgePresentation());
    }

    IEnumerator RunThenFlag(IEnumerator inner, System.Action done)
    {
        yield return inner;
        done?.Invoke();
    }

    IEnumerator RunDiceWithOptionalRevealWait(int d1, int d2, int d3, System.Action done)
    {
        if (waitForRevealAnimationEvent && revealBridge != null)
        {
            float deadline = Time.unscaledTime + Mathf.Max(0.5f, revealEventFallbackSeconds);
            while (!revealBridge.Released && Time.unscaledTime < deadline)
                yield return null;
            if (!revealBridge.Released)
                Debug.LogWarning("[GambleCupDiceOrchestratorView] 未收到 ReleaseDice 動畫事件，已逾時改為直接播骰子。請在開蓋 Clip 上加 Event → ReleaseDice。");
        }

        yield return StartCoroutine(dicePresentation.PlayDiceAnimation(d1, d2, d3));
        done?.Invoke();
    }
}
