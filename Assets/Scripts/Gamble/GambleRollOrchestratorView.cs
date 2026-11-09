using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 同時播放「全畫面／前景 <see cref="Animator"/> 動畫」與子節點的骰子演出（<see cref="DiceRollViewBase"/>），
/// 兩條都結束後才交還給 <see cref="GambleUIController"/> 顯示 Judge。
/// 僅使用 Unity 動畫系統（Animator），不使用序列圖與 Video。
/// </summary>
public class GambleRollOrchestratorView : DiceRollViewBase
{
    [Header("骰子演出（沿用 DiceRollView / DiceRollImageView 等）")]
    [SerializeField] private DiceRollViewBase dicePresentation;

    [Header("畫面動畫（Animator，與骰子並行）")]
    [FormerlySerializedAs("videoOrEffectRoot")]
    [Tooltip("演出用父物件：播放前可選開啟、結束後關閉（例如全畫面特效層）")]
    [SerializeField] private GameObject screenEffectRoot;
    [SerializeField] private Animator screenAnimator;
    [Tooltip("在 Animator Controller 裡設定的 Trigger 名稱")]
    [SerializeField] private string animatorRollTrigger = "Roll";
    [Tooltip("要讀取播放進度的 Layer，預設 0")]
    [SerializeField] private int animatorLayerIndex;
    [Tooltip("若填寫：會等到該 State 播完（normalizedTime ≥ 1）；留空則改用下方「秒數」")]
    [SerializeField] private string waitUntilStateName = "";
    [Tooltip("未填 State 名、或僅想固定等幾秒時使用（秒）")]
    [SerializeField] private float rollAnimationDuration = 2.5f;

    bool _diceDone;
    bool _screenDone;

    public override IEnumerator PlayDiceAnimation(int d1, int d2, int d3)
    {
        _diceDone = false;
        _screenDone = false;
        StartCoroutine(RunDiceThenFlag(d1, d2, d3));
        StartCoroutine(RunScreenThenFlag());
        yield return new WaitUntil(() => _diceDone && _screenDone);
    }

    IEnumerator RunDiceThenFlag(int d1, int d2, int d3)
    {
        if (dicePresentation != null)
            yield return StartCoroutine(dicePresentation.PlayDiceAnimation(d1, d2, d3));
        _diceDone = true;
    }

    IEnumerator RunScreenThenFlag()
    {
        yield return StartCoroutine(ScreenAnimatorRoutine());
        _screenDone = true;
    }

    IEnumerator ScreenAnimatorRoutine()
    {
        if (screenEffectRoot != null)
            screenEffectRoot.SetActive(true);

        if (screenAnimator == null)
        {
            float fallback = Mathf.Max(0f, rollAnimationDuration);
            if (fallback > 0f)
                yield return new WaitForSeconds(fallback);
            if (screenEffectRoot != null)
                screenEffectRoot.SetActive(false);
            yield break;
        }

        if (!string.IsNullOrEmpty(animatorRollTrigger))
            screenAnimator.SetTrigger(animatorRollTrigger);

        if (!string.IsNullOrEmpty(waitUntilStateName))
            yield return WaitAnimatorStateComplete(screenAnimator, animatorLayerIndex, waitUntilStateName);
        else
        {
            float t = Mathf.Max(0f, rollAnimationDuration);
            if (t > 0f)
                yield return new WaitForSeconds(t);
        }

        if (screenEffectRoot != null)
            screenEffectRoot.SetActive(false);
    }

    static IEnumerator WaitAnimatorStateComplete(Animator anim, int layer, string stateName)
    {
        float timeout = Time.time + 60f;
        while (Time.time < timeout)
        {
            if (anim.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
                break;
            yield return null;
        }

        while (Time.time < timeout)
        {
            var info = anim.GetCurrentAnimatorStateInfo(layer);
            if (info.IsName(stateName) && info.normalizedTime >= 0.99f && !anim.IsInTransition(layer))
                yield break;
            yield return null;
        }
    }
}
