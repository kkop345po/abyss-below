using UnityEngine;

/// <summary>
/// 給 Animation Event 呼叫的轉接腳本。
/// 掛在「有 Animator 的 Boss 本體」上，事件就能在下拉選單看到這些函式。
/// </summary>
public class BossHurtboxEvents : MonoBehaviour
{
    [Tooltip("指到 Boss 底下的 Hurtboxes 物件（掛 ColliderFrameSwitcher 的那個）。")]
    [SerializeField] private ColliderFrameSwitcher hurtboxes;

    void Awake()
    {
        if (hurtboxes == null)
            hurtboxes = GetComponentInChildren<ColliderFrameSwitcher>(true);
    }

    /// <summary>切換受傷判定幀（HB_0/HB_1/...）。</summary>
    public void HurtboxFrame(int frameIndex)
    {
        if (hurtboxes == null) return;
        hurtboxes.ApplyFrame(frameIndex);
    }

    /// <summary>關閉所有受傷判定（無敵幀）。</summary>
    public void HurtboxOff()
    {
        if (hurtboxes == null) return;
        hurtboxes.DisableAll();
    }
}

