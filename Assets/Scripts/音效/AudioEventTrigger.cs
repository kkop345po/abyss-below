using UnityEngine;

/// <summary>
/// 將這個腳本掛載到有 Animator 的物件上 (例如玩家本身、或是開船的 HarbonAnimator)。
/// 就可以在 Animation 視窗的 Animation Event 中呼叫 PlaySFXEvent 來播放音效。
/// </summary>
public class AudioEventTrigger : MonoBehaviour
{
    // 在 Animation Event 的 Function 中選擇這個方法，
    // 然後在 String 參數欄位填寫音效名稱 (例如 "跳水" 或 "開船")
    public void PlaySFXEvent(string soundName)
    {
        string resolved = ResolveLegacySfxName(soundName);
        if (AudioManager.GetInstance() != null)
        {
            AudioManager.GetInstance().PlaySFX(resolved);
        }
        else
        {
            Debug.LogWarning($"[AudioEventTrigger] 找不到 AudioManager，無法播放音效: {resolved}");
        }
    }

    private static string ResolveLegacySfxName(string soundName)
    {
        if (string.IsNullOrWhiteSpace(soundName)) return soundName;

        // 相容舊動畫事件：若仍填舊名「跳水(免費)」，自動改播現在的「跳水」。
        if (soundName == "跳水(免費)") return AudioManager.SfxName.JumpWater;

        return soundName;
    }

    // [新增] 在 Animation Event 的 Function 中選擇這個方法來「播放並持續循環」
    public void PlayLoopingSFXEvent(string soundName)
    {
        if (AudioManager.GetInstance() != null)
            AudioManager.GetInstance().PlayLoopingSFX(soundName);
    }

    // [新增] 在 Animation Event 的 Function 中選擇這個方法來「停止」該循環音效
    public void StopLoopingSFXEvent(string soundName)
    {
        if (AudioManager.GetInstance() != null)
            AudioManager.GetInstance().StopLoopingSFX(soundName);
    }
}
