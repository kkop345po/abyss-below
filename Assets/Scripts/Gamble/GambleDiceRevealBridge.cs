using UnityEngine;

/// <summary>
/// 掛在與 <b>Animator</b> 同一個 GameObject 上（或動畫事件會回呼到的同一物件）。
/// 在「開蓋」Animation Clip 的指定影格加 <b>Animation Event</b>，函式名填 <see cref="ReleaseDice"/>，
/// 即可讓 <see cref="GambleCupDiceOrchestratorView"/> 在該幀才開始骰子演出（並可選先隱藏骰子區）。
/// </summary>
public class GambleDiceRevealBridge : MonoBehaviour
{
    [Tooltip("可選：開賭前關閉、收到 ReleaseDice 事件後再打開（通常是 DiceArea 父物件）")]
    [SerializeField] private GameObject diceVisualRoot;

    bool _released;

    /// <summary>供 Orchestrator 等待；收到 Animation Event 後為 true。</summary>
    public bool Released => _released;

    void OnDisable()
    {
        _released = false;
    }

    /// <summary>每局開賭前由 Orchestrator 呼叫，重設旗標並可選先藏骰子。</summary>
    public void ResetGate()
    {
        _released = false;
        if (diceVisualRoot != null)
            diceVisualRoot.SetActive(false);
    }

    /// <summary>
    /// 在 Animation 視窗 → 開蓋 Clip → 加到「開蓋完成那一格」的 Event，函式選單填此名稱（區分大小寫）。
    /// </summary>
    public void ReleaseDice()
    {
        _released = true;
        if (diceVisualRoot != null)
            diceVisualRoot.SetActive(true);
    }
}
