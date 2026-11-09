using UnityEngine;

/// <summary>
/// 掛在「每個場景」任一個常駐物件上（建議空物件命名 SceneAudio），用 Inspector 設定進場要播的 BGM。
/// <para>
/// 說明：專案內 <see cref="AudioManager"/> 為 DontDestroyOnLoad 單例，換場時場景內第二個 AudioManager 會被銷毀，
/// 因此各場景 AudioManager 上的 autoPlayBgmName 往往不會生效；請改用本元件做「每場景配樂」。
/// 另請將 <see cref="PlayerController.landBGMName"/> 留空，否則上岸初始化可能覆寫此處設定的 BGM。
/// </para>
/// </summary>
[DefaultExecutionOrder(250)]
public class SceneBackgroundMusic : MonoBehaviour
{
    public enum BgmMode
    {
        [Tooltip("不切換 BGM（沿用上一場景正在播的配樂）")]
        None,
        [Tooltip("使用 AudioManager 內 bgmSounds 已登錄的名稱（須與 name 欄完全一致）")]
        RegisteredName,
        [Tooltip("直接拖入 AudioClip（例如 Assets/Scenes/音效/配樂 下的 mp3），不必先加到 AudioManager 清單")]
        DirectClip
    }

    [Header("進場 BGM")]
    [Tooltip("None：不切換。RegisteredName：用主表名稱。DirectClip：拖檔即可。")]
    public BgmMode mode = BgmMode.DirectClip;

    [Tooltip("mode = RegisteredName 時使用；須與 AudioManager.bgmSounds[].name 一致")]
    public string registeredBgmName;

    [Tooltip("mode = DirectClip 時使用")]
    public AudioClip bgmClip;

    [Range(0f, 2f)]
    [Tooltip("相對於 PlayerPrefs BGM 滑桿的係數（1 = 正常）")]
    public float volumeScale = 1f;

    [Range(0.1f, 3f)]
    public float pitch = 1f;

    [Header("進場時一併處理")]
    [Tooltip("停止海浪／氣泡等循環環境音，避免帶進新場景")]
    public bool stopAllLoopingSfxOnEnter = true;

    [Tooltip("若已在播同一個 clip，則不重啟（僅 DirectClip 有效）")]
    public bool skipIfSameClipAlreadyPlaying = true;

    private void Start() => Apply();

    /// <param name="clearLoopingSfxFirst">為 false 時略過「進場先停全部循環環境音」（供回港轉場等已手動清過再重套用）。</param>
    public void Apply(bool clearLoopingSfxFirst = true)
    {
        var am = AudioManager.GetInstance();
        if (am == null)
        {
            Debug.LogWarning("[SceneBackgroundMusic] 找不到 AudioManager，略過場景 BGM 設定。");
            return;
        }

        if (clearLoopingSfxFirst && stopAllLoopingSfxOnEnter)
            am.StopAllLoopingSFX();

        switch (mode)
        {
            case BgmMode.None:
                return;
            case BgmMode.DirectClip:
                if (bgmClip == null)
                {
                    Debug.LogWarning("[SceneBackgroundMusic] DirectClip 模式但 bgmClip 未指派。");
                    return;
                }
                if (skipIfSameClipAlreadyPlaying && am.IsBgmPlayingClip(bgmClip))
                    return;
                am.PlayBGMFromClip(bgmClip, volumeScale, pitch);
                return;
            case BgmMode.RegisteredName:
                if (string.IsNullOrWhiteSpace(registeredBgmName))
                {
                    Debug.LogWarning("[SceneBackgroundMusic] RegisteredName 模式但 registeredBgmName 為空。");
                    return;
                }
                am.PlayBGM(registeredBgmName.Trim());
                return;
        }
    }
}
