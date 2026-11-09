using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 以 UI Image 顯示骰子六面圖：<see cref="faceSprites"/> 須為 6 張，順序為 1 點～6 點。
/// 掛在 DiceArea 上，並將 <see cref="GambleUIController.DiceRollView"/> 指到此元件。
/// </summary>
public class DiceRollImageView : DiceRollViewBase
{
    [Header("骰子圖示")]
    [Tooltip("三顆骰子，各對應一個 Image")]
    [SerializeField] private Image[] diceImages = new Image[3];

    [Tooltip("六面圖：索引 0 = 1 點、索引 5 = 6 點")]
    [SerializeField] private Sprite[] faceSprites = new Sprite[6];

    [Header("演出")]
    [Tooltip("總演出時間（秒）；用來讓骰子畫面停留在結果上（點數不會在期間變動）")]
    [SerializeField] private float stubDuration = 1.2f;

    public override IEnumerator PlayDiceAnimation(int d1, int d2, int d3)
    {
        d1 = Mathf.Clamp(d1, 1, 6);
        d2 = Mathf.Clamp(d2, 1, 6);
        d3 = Mathf.Clamp(d3, 1, 6);

        // 顯示當下就定格成「本局結果」：點數由 DiceManager 預先決定，UI 不再在顯示期間亂跳隨機面。
        // 若仍想要演出效果，請改用杯子/遮罩/縮放等動畫，而非改變點數。
        ApplyFaces(d1, d2, d3);

        if (stubDuration > 0f)
            yield return new WaitForSeconds(stubDuration);
    }

    void ApplyFaces(int v1, int v2, int v3)
    {
        int[] v = { v1, v2, v3 };
        for (int i = 0; i < diceImages.Length && i < 3; i++)
        {
            if (diceImages[i] == null) continue;
            int pip = v[i];
            Sprite s = GetFaceSprite(pip);
            if (s != null)
                diceImages[i].sprite = s;
        }
    }

    Sprite GetFaceSprite(int pip1To6)
    {
        if (faceSprites == null || faceSprites.Length < 6) return null;
        int idx = Mathf.Clamp(pip1To6, 1, 6) - 1;
        return faceSprites[idx];
    }
}
