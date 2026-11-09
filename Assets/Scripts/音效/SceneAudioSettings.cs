using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掛在「每個場景」任一個物件上，用 Inspector 直接配置：
/// - 進場要播的 BGM（AudioClip）
/// - 進場要播的循環環境音（AudioClip，可多個）
/// </summary>
[DefaultExecutionOrder(110)]
public class SceneAudioSettings : MonoBehaviour
{
    [System.Serializable]
    public class LoopingClip
    {
        [Tooltip("循環音的唯一鍵（同 key 只會存在一個循環音）。建議寫：Ambience、Waves、Bubbles 之類。")]
        public string key = "Ambience";

        public AudioClip clip;

        [Range(0f, 2f)]
        public float volumeScale = 1f;

        [Range(0.1f, 3f)]
        public float pitch = 1f;
    }

    [Header("進場 BGM（可選）")]
    public bool playBgmOnEnter = true;
    public AudioClip bgmClip;
    [Range(0f, 2f)] public float bgmVolumeScale = 1f;
    [Range(0.1f, 3f)] public float bgmPitch = 1f;

    [Header("進場循環環境音（可選，可多個）")]
    [Tooltip("會使用 AudioManager.PlayLoopingSFXFromClip 建立循環 AudioSource。")]
    public List<LoopingClip> loopingClips = new List<LoopingClip>();

    [Header("進場時一併處理")]
    [Tooltip("先停止所有循環環境音，避免上一場景的循環音帶進來")]
    public bool stopAllLoopingSfxOnEnter = true;

    [Tooltip("若正在播同一首 BGM clip，則不重啟")]
    public bool skipIfSameBgmAlreadyPlaying = true;

    void Start() => Apply();

    /// <param name="clearLoopingSfxFirst">為 false 時略過進場先停全部循環環境音。</param>
    public void Apply(bool clearLoopingSfxFirst = true)
    {
        var am = AudioManager.GetInstance();
        if (am == null)
        {
            Debug.LogWarning("[SceneAudioSettings] 找不到 AudioManager，略過場景音訊設定。");
            return;
        }

        if (clearLoopingSfxFirst && stopAllLoopingSfxOnEnter)
            am.StopAllLoopingSFX();

        if (playBgmOnEnter && bgmClip != null)
        {
            if (!skipIfSameBgmAlreadyPlaying || !am.IsBgmPlayingClip(bgmClip))
                am.PlayBGMFromClip(bgmClip, bgmVolumeScale, bgmPitch);
        }

        if (loopingClips != null)
        {
            for (int i = 0; i < loopingClips.Count; i++)
            {
                var x = loopingClips[i];
                if (x == null) continue;
                if (string.IsNullOrWhiteSpace(x.key) || x.clip == null) continue;
                am.PlayLoopingSFXFromClip(x.key, x.clip, x.volumeScale, x.pitch);
            }
        }
    }
}

