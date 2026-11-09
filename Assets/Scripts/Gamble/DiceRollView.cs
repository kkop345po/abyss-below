using System.Collections;
using UnityEngine;

/// <summary>
/// 骰子演出介面：邏輯層只提供點數，實際動畫由子類或預設協程實作。
/// </summary>
public abstract class DiceRollViewBase : MonoBehaviour
{
    public abstract IEnumerator PlayDiceAnimation(int d1, int d2, int d3);

    /// <summary>
    /// 玩家確認結算（Judge）後、關閉 Judge 面板之前呼叫；例：骰鍾關蓋動畫。預設不做事。
    /// </summary>
    public virtual IEnumerator PlayPostJudgePresentation()
    {
        yield break;
    }
}

/// <summary>
/// 預設占位：無 Animator 時以短暫等待模擬動畫長度，仍會把最終點數寫入 UI。
/// </summary>
public class DiceRollView : DiceRollViewBase
{
    [SerializeField] private float stubDuration = 1.2f;
    [SerializeField] private UnityEngine.UI.Text[] legacyTexts;
    [SerializeField] private TMPro.TextMeshProUGUI[] tmpTexts;

    public override IEnumerator PlayDiceAnimation(int d1, int d2, int d3)
    {
        ApplyFaceTexts(d1, d2, d3);
        if (stubDuration > 0f)
            yield return new WaitForSeconds(stubDuration);
    }

    void ApplyFaceTexts(int d1, int d2, int d3)
    {
        int[] v = { d1, d2, d3 };
        if (legacyTexts != null)
        {
            for (int i = 0; i < legacyTexts.Length && i < 3; i++)
                if (legacyTexts[i] != null) legacyTexts[i].text = v[i].ToString();
        }
        if (tmpTexts != null)
        {
            for (int i = 0; i < tmpTexts.Length && i < 3; i++)
                if (tmpTexts[i] != null) tmpTexts[i].text = v[i].ToString();
        }
    }
}
