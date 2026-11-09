using UnityEngine;

/// <summary>
/// 給 Animator 的 Animation Event 呼叫用：在動畫時間軸上插入音效點。
/// 使用方式：在播放轉場動畫的同一物件（或任意物件）掛此腳本，
/// 然後在 Animation Event 填入函式名 PlaySfx / PlaySfxOnce / PlayActionSfx / StopActionSfx 等。
/// </summary>
public class HarborTransitionSfxBridge : MonoBehaviour
{
    public void PlaySfx(string sfxName)
    {
        AudioManager.GetInstance()?.PlaySFX(sfxName);
    }

    public void PlaySfxOnce(string sfxName)
    {
        AudioManager.GetInstance()?.PlaySFXOnce(sfxName);
    }

    public void PlayActionSfx(string sfxName)
    {
        AudioManager.GetInstance()?.PlayActionSFX(sfxName);
    }

    public void StopActionSfx()
    {
        AudioManager.GetInstance()?.StopActionSFX();
    }

    public void StopBgm()
    {
        AudioManager.GetInstance()?.StopBGM();
    }

    public void StopAllLoopingSfx()
    {
        AudioManager.GetInstance()?.StopAllLoopingSFX();
    }
}

