using UnityEngine;

/// <summary>
/// 掛在 <b>FishingAnimHost</b>（與 Animator 同一物件），供 Animation Event 觸發釣魚音效。
/// 在動畫 Clip 時間軸上加入事件：播音 <see cref="PlayCastSfx"/>／<see cref="PlayFishingSfx"/>；停音 <see cref="StopFishingSfx"/>。
/// </summary>
[DisallowMultipleComponent]
public class FishingAnimationSfxBridge : MonoBehaviour
{
    [SerializeField] private FishingSfxPlayer fishingSfx;
    [Tooltip("PlayCastSfx 事件使用的音檔名（Resources/SFX 下，不含副檔名）")]
    [SerializeField] private string castClipName = "拋竿";

    void Awake()
    {
        if (fishingSfx == null)
            fishingSfx = FindFirstObjectByType<FishingSfxPlayer>();
    }

    /// <summary>動畫事件：函式名填 PlayCastSfx（無參數）。</summary>
    public void PlayCastSfx()
    {
        PlayFishingSfx(castClipName);
    }

    /// <summary>動畫事件：函式名 PlayFishingSfx，字串參數填音檔名（例：拋竿）。</summary>
    public void PlayFishingSfx(string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName))
            return;

        string name = clipName.Trim();
        if (fishingSfx != null && fishingSfx.PlayOneShot(name))
            return;

        AudioManager.GetInstance()?.PlaySFX(name);
    }

    /// <summary>動畫事件：函式名填 StopFishingSfx（無參數），停止釣魚專用音源上的所有聲音。</summary>
    public void StopFishingSfx()
    {
        if (fishingSfx != null)
            fishingSfx.StopAll();
    }

    /// <summary>動畫事件：只停止循環／拉線類（共用 loop 音源）。</summary>
    public void StopFishingLoopSfx()
    {
        if (fishingSfx != null)
            fishingSfx.StopLoop();
    }
}
