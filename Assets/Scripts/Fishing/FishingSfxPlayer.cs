using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 釣魚專用音效：直接從 Resources/SFX 載入，不依賴 AudioManager 的 Sound 表與面板靜音邏輯。
/// 掛在 <see cref="FishingManager"/> 同一物件上。
/// </summary>
[DisallowMultipleComponent]
public class FishingSfxPlayer : MonoBehaviour
{
    const string DefaultResourcesFolder = "SFX";

    [SerializeField] private string resourcesFolder = DefaultResourcesFolder;
    [Range(0f, 2f)]
    [SerializeField] private float volumeScale = 1f;

    AudioSource _oneShotSource;
    AudioSource _loopSource;
    readonly Dictionary<string, AudioClip> _clipCache = new();

    void Awake()
    {
        _oneShotSource = CreateChildSource("FishingSfx_OneShot", false);
        _loopSource = CreateChildSource("FishingSfx_Loop", true);
    }

    AudioSource CreateChildSource(string objectName, bool loop)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;
        return src;
    }

    string Folder => string.IsNullOrWhiteSpace(resourcesFolder)
        ? DefaultResourcesFolder
        : resourcesFolder.Trim().Trim('/');

    float EffectiveVolume => Mathf.Clamp01(
        PlayerPrefs.GetFloat("MasterVolume", 1f)
        * PlayerPrefs.GetFloat("SFXVolume", 1f)
        * volumeScale);

    public AudioClip LoadClip(string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName))
            return null;

        string key = clipName.Trim();
        if (_clipCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        var clip = Resources.Load<AudioClip>($"{Folder}/{key}");
        if (clip == null)
            Debug.LogWarning($"[FishingSfxPlayer] 找不到 Resources/{Folder}/{key}，請確認音檔存在且檔名一致。");

        _clipCache[key] = clip;
        return clip;
    }

    public bool PlayOneShot(string clipName)
    {
        var clip = LoadClip(clipName);
        if (clip == null || _oneShotSource == null)
            return false;

        _oneShotSource.PlayOneShot(clip, EffectiveVolume);
        return true;
    }

    public bool PlayLoop(string clipName)
    {
        var clip = LoadClip(clipName);
        if (clip == null || _loopSource == null)
            return false;

        _loopSource.loop = true;
        _loopSource.clip = clip;
        _loopSource.volume = EffectiveVolume;
        _loopSource.Play();
        return true;
    }

    public void StopLoop()
    {
        if (_loopSource != null)
            _loopSource.Stop();
    }

    public bool PlayStoppable(string clipName)
    {
        var clip = LoadClip(clipName);
        if (clip == null || _loopSource == null)
            return false;

        _loopSource.loop = false;
        _loopSource.clip = clip;
        _loopSource.volume = EffectiveVolume;
        _loopSource.Play();
        return true;
    }

    public void StopStoppable()
    {
        StopLoop();
    }

    public void StopOneShot()
    {
        if (_oneShotSource != null)
            _oneShotSource.Stop();
    }

    /// <summary>停止循環／可中斷音源與一次性音源。</summary>
    public void StopAll()
    {
        StopLoop();
        StopOneShot();
    }
}
