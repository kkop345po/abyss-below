using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class Sound
{
    [Tooltip("呼叫此音效時使用的名稱（例如 '走路'）")]
    public string name;
    
    [Tooltip("放入 MP3 或 WAV 音效檔")]
    public AudioClip clip;
    
    [Range(0f, 1f)]
    public float volume = 1f;
    
    [Range(0.1f, 3f)]
    public float pitch = 1f;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    /// <summary>
    /// 專案內常用 SFX 名稱表（與 Inspector 的 sfxSounds.name 完全一致）。
    /// 目的：避免到處手打字串拼錯，並提供統一接口呼叫。
    /// </summary>
    public static class SfxName
    {
        // UI / 互動（名稱須與 Resources／Sound 表檔名一致）
        public const string Button = "按按鈕";
        public const string ClickDialogue = "點擊";
        /// <summary>賭博搖骰等。</summary>
        public const string DiceGamble = "骰子新";
        // 統一介面名稱使用「撿錢」
        public const string PickupCoin = "撿錢";
        /// <summary>一般道具／素材撿取；若 Sound 表無此名，會別名回撿錢。</summary>
        public const string PickupItem = "撿東西";
        public const string DiscoverTreasure = "發現寶物";
        public const string SpecialItem = "特殊道具音效";

        // 玩家
        public const string Shoot = "射擊";
        public const string UseMedkit = "用血包";
        public const string GotHit = "被攻擊";
        public const string Breath = "呼吸";
        public const string JumpWater = "跳水";
        public const string Sail = "開船";

        // 移動（建議用 PlayMovementSFX 播放）
        public const string Walk = "走路新";
        public const string Swim = "海底游泳";

        // 敵人 / 戰鬥
        public const string MonsterHurt = "怪物受傷";
        public const string MonsterDie = "怪物死";
        public const string KillFish = "殺魚";
        public const string MonsterRoar = "怪獸吼叫";
        public const string MonsterCharge = "怪獸積光";
        public const string MonsterImpact = "怪獸衝擊";
        public const string Warning = "警告聲";
        public const string OctopusHurt = "章魚受傷";
        public const string OctopusRoar = "章魚怪叫";
        public const string OctopusDie = "章魚死";

        // 物件 / 環境
        public const string SmashFloor = "拍地板";
        public const string FruitExplode = "果實爆炸";
        public const string RockFall = "石頭落下";
        public const string WaterMissile = "水飛彈";
        public const string WaterMissileShoot = "水飛彈射擊";

        // 循環環境音（建議用 PlayLoopingSFX 播放）
        public const string SeaWavesLoop = "海浪聲";
        public const string WaterBubblesLoop = "水底氣泡";
    }

    /// <summary>專案內常用 BGM 名稱表（與 Inspector 的 bgmSounds.name 完全一致）。</summary>
    public static class BgmName
    {
        public const string Harbor = "港口邊";
        public const string Underwater = "水下環境音";
    }

    // ======== SFX 便捷接口（避免各處手打字串） ========
    public void PlayButtonSfx() => PlaySFX(SfxName.Button);
    public void PlayDialogueClickSfx() => PlaySFX(SfxName.ClickDialogue);
    public void PlayDiceGambleSfx() => PlaySFX(SfxName.DiceGamble);
    public void PlayPickupCoinSfx() => PlaySFX(SfxName.PickupCoin);
    public void PlayPickupItemSfx() => PlaySFX(SfxName.PickupItem);
    public void PlayDiscoverTreasureSfx() => PlaySFX(SfxName.DiscoverTreasure);
    public void PlaySpecialItemSfx() => PlaySFX(SfxName.SpecialItem);

    public void PlayShootSfx() => PlaySFX(SfxName.Shoot);
    public void PlayUseMedkitSfx() => PlaySFX(SfxName.UseMedkit);
    public void PlayGotHitSfx() => PlaySFX(SfxName.GotHit);
    public void PlayBreathSfx() => PlaySFX(SfxName.Breath);
    public void PlayJumpWaterSfx() => PlaySFX(SfxName.JumpWater);
    public void PlaySailSfx() => PlaySFX(SfxName.Sail);

    public void PlayWalkMovementSfx() => PlayMovementSFX(SfxName.Walk);
    public void PlaySwimMovementSfx() => PlayMovementSFX(SfxName.Swim);

    public void PlayMonsterHurtSfx() => PlaySFX(SfxName.MonsterHurt);
    public void PlayMonsterDieSfx() => PlaySFX(SfxName.MonsterDie);
    public void PlayKillFishSfx() => PlaySFX(SfxName.KillFish);
    public void PlayMonsterRoarSfx() => PlaySFX(SfxName.MonsterRoar);
    public void PlayMonsterChargeSfx() => PlaySFX(SfxName.MonsterCharge);
    public void PlayMonsterImpactSfx() => PlaySFX(SfxName.MonsterImpact);
    public void PlayWarningSfx() => PlaySFX(SfxName.Warning);
    public void PlayOctopusHurtSfx() => PlaySFX(SfxName.OctopusHurt);
    public void PlayOctopusRoarSfx() => PlaySFX(SfxName.OctopusRoar);
    public void PlayOctopusDieSfx() => PlaySFX(SfxName.OctopusDie);

    public void PlaySmashFloorSfx() => PlaySFX(SfxName.SmashFloor);
    public void PlayFruitExplodeSfx() => PlaySFX(SfxName.FruitExplode);
    /// <summary>第三層毒胞爆發用的一次性音效（目前沿用「果實爆開」）。</summary>
    public void PlayPoisonCellExplodeSfx() => PlayActionSFX(SfxName.FruitExplode);
    public void PlayRockFallSfx() => PlaySFX(SfxName.RockFall);
    public void PlayWaterMissileSfx() => PlaySFX(SfxName.WaterMissile);
    public void PlayWaterMissileShootSfx() => PlaySFX(SfxName.WaterMissileShoot);

    // ======== 循環環境音便捷接口 ========
    public void PlaySeaWavesLoop() => PlayLoopingSFX(SfxName.SeaWavesLoop);
    public void StopSeaWavesLoop() => StopLoopingSFX(SfxName.SeaWavesLoop);
    public void PlayWaterBubblesLoop() => PlayLoopingSFX(SfxName.WaterBubblesLoop);
    public void StopWaterBubblesLoop() => StopLoopingSFX(SfxName.WaterBubblesLoop);

    // ======== BGM 便捷接口 ========
    public void PlayHarborBgm() => PlayBGM(BgmName.Harbor);
    public void PlayUnderwaterBgm() => PlayBGM(BgmName.Underwater);

    /// <summary>
    /// 優先使用已註冊單例；若 Awake 尚未執行或需從關閉物件尋找，則在場景內搜尋（含未啟用的 AudioManager）。
    /// </summary>
    public static AudioManager GetInstance()
    {
        if (Instance != null)
            return Instance;
        var found = Object.FindObjectsOfType<AudioManager>(true);
        return (found != null && found.Length > 0) ? found[0] : null;
    }

    [Header("音量混合器 (Audio Mixer) 可選配")]
    [Tooltip("可以用來進行整體的音量統一管理")]
    public AudioMixer audioMixer;
    
    [Header("音效播放器 (Audio Sources)")]
    [Tooltip("請拖入用來播放BGM的AudioSource (掛載在同物件上)")]
    public AudioSource bgmSource;
    [Tooltip("請拖入用來播放SFX的AudioSource (掛載在同物件上)")]
    public AudioSource sfxSource;
    [Tooltip("走路／游泳等移動音效專用；與 sfx 分開可在停止移動時 Stop，不影響其他短音效。未指派時會與 sfxSource 共用（此時 StopMovementSFX 會一併停掉該來源上所有聲音）。")]
    public AudioSource movementSfxSource;
    [Tooltip("可被中斷的動作音效（蓄力/爆發/持續效果等）：使用 clip+Play 播放，可在停止動作當下 Stop() 立刻切掉。未指派時會回退共用 sfxSource（可能切到其他同來源音效）。")]
    public AudioSource actionSfxSource;

    [Header("該場景會用到的背景音樂清單")]
    public Sound[] bgmSounds;
    
    [Header("該場景會用到的音效清單")]
    public Sound[] sfxSounds;

    [Header("想在場景開始時自動播放的BGM名稱")]
    [Tooltip("例如填寫『水下環境音』，留空則不自動播放")]
    public string autoPlayBgmName;

    [Header("（可選）Resources 自動載入（免手動登錄 Sound 表）")]
    [Tooltip("若 PlaySFX/PlayBGM 找不到 name 對應的 Sound，會嘗試從 Resources 載入。\n" +
             "請把音檔放到 Assets/Resources/Audio/SFX 或 Assets/Resources/SFX 下（可建子資料夾），\n" +
             "並確保檔名（不含副檔名）與你呼叫/動畫事件的名稱一致。")]
    [SerializeField] private bool enableResourcesAutoLoad = true;
    [SerializeField] private string resourcesSfxRoot = "Audio/SFX";
    [SerializeField] private string resourcesBgmRoot = "Audio/BGM";
    [Tooltip("主路徑找不到時，依序嘗試這些 Resources 子資料夾（例：釣魚音在 SFX/）。")]
    [SerializeField] private string[] extraSfxResourceRoots = { "SFX" };

    private readonly Dictionary<string, AudioClip> _resourcesClipCache = new Dictionary<string, AudioClip>();

    [Header("SFX 播放策略")]
    [Tooltip("播放新的 SFX 前先 Stop 該 AudioSource（可避免連續動作音效疊在一起；也可做到「離開動作就停聲」）。")]
    [SerializeField] private bool stopSourceBeforeSfxPlay = true;

    [Header("BGM 音量鎖定")]
    [Tooltip("勾選後：BGM 音量固定為下方數值，忽略任何 UI/程式的 SetBGMVolume。")]
    [SerializeField] private bool lockBgmVolume = true;
    [Range(0f, 1f)]
    [SerializeField] private float lockedBgmVolume = 0.5f;

    [Header("循環環境音（海浪聲／水底氣泡／港口邊等）")]
    [Tooltip("乘在「Sound 表登錄的循環環境音」基礎音量上；不影響 SceneAudioSettings 用 PlayLoopingSFXFromClip 自訂係數的 clip。\n建議 0.25～0.45 較不搶 BGM。")]
    [Range(0f, 1f)]
    [SerializeField] private float loopingAmbientVolumeScale = 0.38f;

    private Dictionary<string, AudioSource> loopingSFXDict = new Dictionary<string, AudioSource>();
    // key -> 基礎音量（用於「直接 AudioClip 循環音」或找不到 Sound 表時的回退）
    private Dictionary<string, float> loopingSfxBaseVolumeDict = new Dictionary<string, float>();

    /// <summary> 片頭／片尾播放時 > 0：停止 BGM 與循環環境音，並靜音共用 SFX（不影響 VideoPlayer 綁定的 AudioSource）。 </summary>
    private int _videoPlaybackMuteStack;

    /// <summary>「選擇回去哪裡」等 UI：保留 BGM，暫停／阻擋 SFX 與循環環境音。</summary>
    private int _choicePanelSfxSuppressStack;

    /// <summary> 目前 BGM 在 Sound 表上的音量係數（不含玩家滑桿／主音量） </summary>
    private float _bgmAssetVolumeScale = 1f;

    public bool IsVideoPlaybackSuppressingGameAudio => _videoPlaybackMuteStack > 0;

    public void PushVideoPlaybackMute()
    {
        _videoPlaybackMuteStack++;
        if (_videoPlaybackMuteStack != 1) return;

        StopBGM();
        var keys = new List<string>(loopingSFXDict.Keys);
        foreach (var k in keys)
            StopLoopingSFX(k);

        if (sfxSource != null)
            sfxSource.mute = true;
        if (movementSfxSource != null)
            movementSfxSource.mute = true;
        StopMovementSFX();
    }

    public void PopVideoPlaybackMute()
    {
        if (_videoPlaybackMuteStack <= 0) return;
        _videoPlaybackMuteStack--;
        if (_videoPlaybackMuteStack > 0) return;

        if (sfxSource != null)
            sfxSource.mute = false;
        if (movementSfxSource != null)
            movementSfxSource.mute = false;
    }

    /// <summary>
    /// 回程選擇面板等：不清 BGM，停止一次性／移動／動作／循環環境 SFX；關閉面板時請呼叫 <see cref="PopChoicePanelSfxSuppress"/>。
    /// </summary>
    public void PushChoicePanelSfxSuppress()
    {
        _choicePanelSfxSuppressStack++;
        if (_choicePanelSfxSuppressStack != 1) return;

        StopAllOneShotSfx();
        StopMovementSFX();
        StopActionSFX();
        StopAllLoopingSFX();
    }

    public void PopChoicePanelSfxSuppress()
    {
        if (_choicePanelSfxSuppressStack <= 0) return;
        _choicePanelSfxSuppressStack--;
        if (_choicePanelSfxSuppressStack > 0) return;

        RefreshAmbientFromActivePlayer();
    }

    static void RefreshAmbientFromActivePlayer()
    {
        var pcs = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < pcs.Length; i++)
        {
            if (pcs[i] != null)
            {
                pcs[i].RefreshEnvironmentLoopingAfterUiClosed();
                return;
            }
        }

        var lands = Object.FindObjectsByType<LandPlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < lands.Length; i++)
        {
            if (lands[i] != null)
            {
                lands[i].RefreshEnvironmentLoopingAfterUiClosed();
                return;
            }
        }
    }

    private static float MasterPref => PlayerPrefs.GetFloat("MasterVolume", 1f);
    private static float BgmPref => PlayerPrefs.GetFloat("BGMVolume", 0.5f);
    private static float SfxPref => PlayerPrefs.GetFloat("SFXVolume", 1f);
    private const string BgmPrefKey = "BGMVolume";

    private float EffectiveBgmPref =>
        lockBgmVolume ? Mathf.Clamp01(lockedBgmVolume) : BgmPref;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        foreach (var kvp in loopingSFXDict)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        loopingSFXDict.Clear();
        loopingSfxBaseVolumeDict.Clear();
        _resourcesClipCache.Clear();
        Instance = null;
    }

    AudioClip TryLoadBgmClip(string name)
    {
        var clip = TryLoadClipFromResources(resourcesBgmRoot, name);
        if (clip != null) return clip;

        string[] fallbacks = { "BGM", "SFX", "Audio/BGM", "Audio/SFX" };
        for (int i = 0; i < fallbacks.Length; i++)
        {
            string root = fallbacks[i];
            if (string.IsNullOrWhiteSpace(root)) continue;
            root = root.Trim().Trim('/');
            if (root == resourcesBgmRoot?.Trim().Trim('/')) continue;
            clip = TryLoadClipFromResources(root, name);
            if (clip != null) return clip;
        }

        return null;
    }

    AudioClip TryLoadClipFromResources(string root, string name)
    {
        if (!enableResourcesAutoLoad) return null;
        if (string.IsNullOrWhiteSpace(name)) return null;

        string basePath = string.IsNullOrWhiteSpace(root) ? "" : root.Trim().Trim('/');
        string n = name.Trim();
        string cacheKey = $"{basePath}::{n}";

        if (_resourcesClipCache.TryGetValue(cacheKey, out var cached))
            return cached;

        AudioClip clip = TryLoadClipFromSingleResourceRoot(basePath, n);

        if (clip == null && extraSfxResourceRoots != null)
        {
            for (int i = 0; i < extraSfxResourceRoots.Length; i++)
            {
                string alt = extraSfxResourceRoots[i];
                if (string.IsNullOrWhiteSpace(alt)) continue;
                alt = alt.Trim().Trim('/');
                if (alt == basePath) continue;

                clip = TryLoadClipFromSingleResourceRoot(alt, n);
                if (clip != null)
                    break;
            }
        }

        _resourcesClipCache[cacheKey] = clip;
        return clip;
    }

    AudioClip TryLoadClipFromSingleResourceRoot(string basePath, string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>(string.IsNullOrEmpty(basePath) ? clipName : $"{basePath}/{clipName}");

        if (clip == null && !string.IsNullOrEmpty(basePath))
        {
            var all = Resources.LoadAll<AudioClip>(basePath);
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (c == null) continue;
                string k = $"{basePath}::{c.name}";
                if (!_resourcesClipCache.ContainsKey(k))
                    _resourcesClipCache[k] = c;
            }

            string cacheKey = $"{basePath}::{clipName}";
            _resourcesClipCache.TryGetValue(cacheKey, out clip);
        }

        return clip;
    }

    private void Start()
    {
        // BGM 音量：可選固定 0.5（避免被 UI/Prefs 覆寫）
        if (lockBgmVolume)
            PlayerPrefs.SetFloat(BgmPrefKey, Mathf.Clamp01(lockedBgmVolume));
        else if (!PlayerPrefs.HasKey(BgmPrefKey))
            PlayerPrefs.SetFloat(BgmPrefKey, 0.5f);

        float masterVol = MasterPref;
        float bgmVol = BgmPref;
        float sfxVol = SfxPref;

        SetMasterVolume(masterVol);
        if (lockBgmVolume)
            SetBGMVolume(Mathf.Clamp01(lockedBgmVolume));
        else
            SetBGMVolume(bgmVol);
        SetSFXVolume(sfxVol);

        if (!string.IsNullOrEmpty(autoPlayBgmName))
            PlayBGM(autoPlayBgmName);
    }

    /// <summary>
    /// 未使用 AudioMixer 時，依 PlayerPrefs 同步 BGM／SFX 來源與循環音音量。
    /// </summary>
    private void RefreshNonMixerChannelVolumes()
    {
        if (audioMixer != null) return;

        float m = MasterPref;
        float b = EffectiveBgmPref;
        float sfx = SfxPref;

        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.volume = _bgmAssetVolumeScale * b * m;

        if (sfxSource != null)
            sfxSource.volume = sfx * m;
        if (movementSfxSource != null)
            movementSfxSource.volume = sfx * m;

        float ambScale = Mathf.Clamp01(loopingAmbientVolumeScale);
        foreach (var kvp in loopingSFXDict)
        {
            if (kvp.Value == null) continue;
            Sound snd = System.Array.Find(sfxSounds, sound => sound.name == kvp.Key);
            float baseVol = snd != null
                ? snd.volume * ambScale
                : (loopingSfxBaseVolumeDict.TryGetValue(kvp.Key, out float v) ? v : 1f);
            kvp.Value.volume = baseVol * sfx * m;
        }
    }

    public void PlayBGM(string soundName)
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("[AudioManager] bgmSource 未指派，無法播放 BGM: " + soundName);
            return;
        }

        string resolvedName = ResolveBgmAlias(soundName);
        Sound s = System.Array.Find(bgmSounds, sound => sound.name == resolvedName);
        if (s == null)
        {
            var clip = TryLoadBgmClip(resolvedName);
            if (clip == null)
            {
                Debug.LogWarning("AudioManager 找不到 BGM 名稱（也找不到 Resources clip）: " + resolvedName + "（原始請求: " + soundName + "）");
                return;
            }
            PlayBGMFromClip(clip, 1f, 1f);
            return;
        }

        if (s.clip == null)
        {
            Debug.LogWarning($"[AudioManager] BGM「{soundName}」的 AudioClip 為空");
            return;
        }

        _bgmAssetVolumeScale = s.volume;
        bgmSource.clip = s.clip;
        bgmSource.pitch = s.pitch;
        bgmSource.loop = true;

        if (audioMixer != null)
        {
            // 若鎖定 BGM 音量：Mixer 的 BGM 參數固定 0dB，實際響度由 AudioSource.volume 控制（避免「Mixer 0.5 × Source 0.5」變太靜）
            if (lockBgmVolume)
            {
                audioMixer.SetFloat("BGMVolume", 0f);
                // BGM 偏好由 PlayerPrefs（鎖定時等於 locked）提供；主音量由 Mixer 的 Master 群組處理，避免重複乘算
                bgmSource.volume = _bgmAssetVolumeScale * EffectiveBgmPref;
            }
            else
            {
                bgmSource.volume = s.volume;
            }
        }
        else
            bgmSource.volume = _bgmAssetVolumeScale * EffectiveBgmPref * MasterPref;

        bgmSource.Play();
    }

    private static string ResolveBgmAlias(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        name = name.Trim();
        // 舊命名相容：去掉「(免費素材網站)」等後綴
        if (name == "水下環境音(免費素材網站)") return BgmName.Underwater;
        return name;
    }

    public void StopBGM()
    {
        if (bgmSource == null) return;
        if (bgmSource.isPlaying)
            bgmSource.Stop();
    }

    /// <summary>
    /// 是否正在播放指定 clip（供 <see cref="SceneBackgroundMusic"/> 等避免重進場景時重啟同一首）。
    /// </summary>
    public bool IsBgmPlayingClip(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return false;
        return bgmSource.isPlaying && bgmSource.clip == clip;
    }

    /// <summary>
    /// 直接以 AudioClip 播放 BGM（不必先登錄在 bgmSounds）。音量邏輯與 <see cref="PlayBGM"/> 一致。
    /// </summary>
    public void PlayBGMFromClip(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("[AudioManager] bgmSource 未指派，無法播放 BGM clip。");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] PlayBGMFromClip：clip 為空。");
            return;
        }

        _bgmAssetVolumeScale = Mathf.Clamp(volumeScale, 0f, 2f);
        bgmSource.clip = clip;
        bgmSource.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        bgmSource.loop = true;

        if (audioMixer != null)
        {
            if (lockBgmVolume)
            {
                audioMixer.SetFloat("BGMVolume", 0f);
                bgmSource.volume = _bgmAssetVolumeScale * EffectiveBgmPref;
            }
            else
            {
                bgmSource.volume = _bgmAssetVolumeScale;
            }
        }
        else
            bgmSource.volume = _bgmAssetVolumeScale * EffectiveBgmPref * MasterPref;

        bgmSource.Play();
    }

    /// <summary>
    /// 停止所有循環環境音（不影響一般一次性 SFX，也不會 mute）。
    /// 用於死亡回陸地/強制切場景時，避免舊場景循環音帶到新場景。
    /// </summary>
    public void StopAllLoopingSFX()
    {
        var keys = new List<string>(loopingSFXDict.Keys);
        foreach (var k in keys)
            StopLoopingSFX(k);
    }

    /// <summary>依 Sound 表或 Resources（含 extraSfxResourceRoots）解析 clip。</summary>
    public AudioClip TryResolveSfxClip(string soundName)
    {
        if (string.IsNullOrWhiteSpace(soundName)) return null;

        string resolvedName = ResolveSfxAlias(soundName.Trim());
        Sound s = System.Array.Find(sfxSounds, sound => sound.name == resolvedName);
        if (s != null && s.clip != null)
            return s.clip;

        return TryLoadClipFromResources(resourcesSfxRoot, resolvedName);
    }

    public void PlaySFX(string soundName)
    {
        PlaySFX(soundName, 1f);
    }

    /// <summary>
    /// 播放一次性 SFX，並額外乘上一個音量倍率（0~1 常用）。
    /// 不會改動 Sound 表的原始音量，只影響這次呼叫。
    /// </summary>
    public void PlaySFX(string soundName, float volumeScale)
    {
        if (_videoPlaybackMuteStack > 0) return;
        if (_choicePanelSfxSuppressStack > 0) return;

        if (sfxSource == null)
        {
            Debug.LogWarning("[AudioManager] sfxSource 未指派，無法播放 SFX: " + soundName);
            return;
        }

        string resolvedName = ResolveSfxAlias(soundName);
        Sound s = System.Array.Find(sfxSounds, sound => sound.name == resolvedName);
        AudioClip clipFromResources = null;
        if (s == null)
            clipFromResources = TryLoadClipFromResources(resourcesSfxRoot, resolvedName);
        // 「撿東西」表／Resources 沒有時，與「撿錢」共用同一支音檔
        if (s == null && clipFromResources == null && soundName == SfxName.PickupItem)
        {
            resolvedName = ResolveSfxAlias(SfxName.PickupCoin);
            s = System.Array.Find(sfxSounds, sound => sound.name == resolvedName);
            if (s == null)
                clipFromResources = TryLoadClipFromResources(resourcesSfxRoot, resolvedName);
        }

        if (s == null && clipFromResources == null)
        {
            Debug.LogWarning("AudioManager 找不到 SFX 名稱（也找不到 Resources clip）: " + resolvedName + "（原始請求: " + soundName + "）");
            return;
        }

        if (s != null && s.clip == null)
        {
            Debug.LogWarning($"[AudioManager] SFX「{soundName}」的 AudioClip 為空");
            return;
        }

        if (stopSourceBeforeSfxPlay) sfxSource.Stop();
        sfxSource.loop = false;
        sfxSource.clip = null;
        float mul = Mathf.Clamp(volumeScale, 0f, 2f);
        if (s != null)
        {
            sfxSource.pitch = s.pitch;
            sfxSource.PlayOneShot(s.clip, s.volume * mul);
        }
        else
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(clipFromResources, 1f * mul);
        }
    }

    AudioSource MovementSfxSourceOrDefault => movementSfxSource != null ? movementSfxSource : sfxSource;
    AudioSource ActionSfxSourceOrDefault => actionSfxSource != null ? actionSfxSource : sfxSource;

    /// <summary> 走路／游泳等移動音效；播在 movementSfxSource（未指派則 sfxSource）。 </summary>
    public void PlayMovementSFX(string soundName)
    {
        if (_videoPlaybackMuteStack > 0) return;
        if (_choicePanelSfxSuppressStack > 0) return;

        var src = MovementSfxSourceOrDefault;
        if (src == null)
        {
            Debug.LogWarning("[AudioManager] movement／sfx Source 未指派，無法播放移動 SFX: " + soundName);
            return;
        }

        string resolvedName = ResolveSfxAlias(soundName);
        Sound s = System.Array.Find(sfxSounds, sound => sound.name == resolvedName);
        if (s == null)
        {
            var clip = TryLoadClipFromResources(resourcesSfxRoot, resolvedName);
            if (clip == null)
            {
                Debug.LogWarning("AudioManager 找不到 SFX 名稱（也找不到 Resources clip）: " + resolvedName);
                return;
            }
            src.loop = false;
            src.clip = null;
            src.pitch = 1f;
            src.Stop();
            src.PlayOneShot(clip, 1f);
            return;
        }

        if (s.clip == null)
        {
            Debug.LogWarning($"[AudioManager] SFX「{soundName}」的 AudioClip 為空");
            return;
        }

        if (stopSourceBeforeSfxPlay) src.Stop();
        src.loop = false;
        src.clip = null;
        src.pitch = s.pitch;
        // 移動音效可能會在短間隔重複觸發；若用 PlayOneShot 會疊加。
        // 先 Stop 可保證同時間只播一段移動音效，避免走太久越疊越吵。
        src.PlayOneShot(s.clip, s.volume);
    }

    /// <summary> 停止移動音效（對 movementSfxSource 或共用的 sfxSource 呼叫 Stop）。 </summary>
    public void StopMovementSFX()
    {
        var src = MovementSfxSourceOrDefault;
        if (src != null)
            src.Stop();
    }

    /// <summary>
    /// 可被中斷的動作音效：用 clip+Play 播放（非 PlayOneShot），可在停止動作當下 StopActionSFX() 立刻切掉。
    /// </summary>
    public void PlayActionSFX(string soundName) => PlayActionSFX(soundName, false);

    public void PlayActionSFX(string soundName, bool loop)
    {
        if (_videoPlaybackMuteStack > 0) return;
        if (_choicePanelSfxSuppressStack > 0) return;

        var src = ActionSfxSourceOrDefault;
        if (src == null)
        {
            Debug.LogWarning("[AudioManager] action／sfx Source 未指派，無法播放動作 SFX: " + soundName);
            return;
        }

        string resolvedName = ResolveSfxAlias(soundName);
        Sound s = System.Array.Find(sfxSounds, sound => sound.name == resolvedName);
        if (s == null)
        {
            var clip = TryLoadClipFromResources(resourcesSfxRoot, resolvedName);
            if (clip == null)
            {
                Debug.LogWarning("AudioManager 找不到 SFX 名稱（也找不到 Resources clip）: " + resolvedName);
                return;
            }
            src.loop = loop;
            src.Stop();
            src.clip = clip;
            src.pitch = 1f;
            src.volume = 1f;
            src.Play();
            return;
        }

        if (s.clip == null)
        {
            Debug.LogWarning($"[AudioManager] SFX「{soundName}」的 AudioClip 為空");
            return;
        }

        if (stopSourceBeforeSfxPlay) src.Stop();
        src.loop = loop;
        src.Stop();
        src.clip = s.clip;
        src.pitch = s.pitch;
        src.volume = s.volume;
        src.Play();
    }

    /// <summary>強制停止當前一次性 SFX（不含循環環境音）。</summary>
    public void StopAllOneShotSfx()
    {
        if (sfxSource != null) sfxSource.Stop();
        if (movementSfxSource != null) movementSfxSource.Stop();
        if (actionSfxSource != null) actionSfxSource.Stop();
    }

    public void StopActionSFX()
    {
        var src = ActionSfxSourceOrDefault;
        if (src != null)
            src.Stop();
    }

    /// <summary>
    /// 保證只播一次的 UI／按鈕類短音效（獨立 AudioSource，不受共用 sfxSource.loop 影響）
    /// </summary>
    public void PlaySFXOnce(string soundName)
    {
        if (_videoPlaybackMuteStack > 0) return;
        if (_choicePanelSfxSuppressStack > 0) return;

        string resolvedName = ResolveSfxAlias(soundName);
        Sound s = System.Array.Find(sfxSounds, sound => sound.name == resolvedName);
        AudioClip clipFromResources = null;
        if (s == null)
            clipFromResources = TryLoadClipFromResources(resourcesSfxRoot, resolvedName);
        if (s == null && clipFromResources == null && soundName == SfxName.PickupItem)
        {
            resolvedName = ResolveSfxAlias(SfxName.PickupCoin);
            s = System.Array.Find(sfxSounds, sound => sound.name == resolvedName);
            if (s == null)
                clipFromResources = TryLoadClipFromResources(resourcesSfxRoot, resolvedName);
        }
        if (s == null && clipFromResources == null)
        {
            Debug.LogWarning("AudioManager PlaySFXOnce 找不到 SFX: " + resolvedName + "（原始: " + soundName + "）");
            return;
        }

        if (s != null && s.clip == null) return;

        var go = new GameObject($"SFX_OneShot_{soundName}");
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        if (s != null)
        {
            src.clip = s.clip;
            src.pitch = s.pitch;
            if (audioMixer != null)
                src.volume = s.volume;
            else
            {
                float m = MasterPref;
                float sfx = SfxPref;
                src.volume = s.volume * sfx * m;
            }
        }
        else
        {
            src.clip = clipFromResources;
            src.pitch = 1f;
            if (audioMixer != null)
                src.volume = 1f;
            else
                src.volume = SfxPref * MasterPref;
        }

        if (sfxSource != null && sfxSource.outputAudioMixerGroup != null)
            src.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;

        src.Play();
        float clipLen = s != null ? s.clip.length : clipFromResources.length;
        float life = clipLen / Mathf.Max(Mathf.Abs(src.pitch), 0.01f) + 0.15f;
        Destroy(go, life);
    }

    public void PlayLoopingSFX(string soundName)
    {
        if (_videoPlaybackMuteStack > 0) return;
        if (_choicePanelSfxSuppressStack > 0) return;

        string resolvedName = ResolveSfxAlias(soundName);
        if (loopingSFXDict.ContainsKey(resolvedName)) return;

        Sound s = System.Array.Find(sfxSounds, sound => sound.name == resolvedName);
        if (s == null)
        {
            var clip = TryLoadClipFromResources(resourcesSfxRoot, resolvedName);
            if (clip == null)
                clip = TryLoadClipFromResources(resourcesBgmRoot, resolvedName);
            if (clip == null)
            {
                // 最後嘗試：僅登錄在 bgmSounds 表上的 clip（例如「港口邊」只放在 BGM 清單時）
                Sound bgm = bgmSounds != null
                    ? System.Array.Find(bgmSounds, x => x != null && x.name == resolvedName)
                    : null;
                if (bgm != null && bgm.clip != null)
                    clip = bgm.clip;
            }
            if (clip == null)
            {
                Debug.LogWarning("AudioManager 找不到循環 SFX 名稱（SFX/BGM Resources 與 bgmSounds 表皆無）: " + resolvedName);
                return;
            }
            float amb = Mathf.Clamp01(loopingAmbientVolumeScale);
            PlayLoopingSFXFromClip(resolvedName, clip, amb, 1f);
            return;
        }

        if (s.clip == null)
        {
            Debug.LogWarning($"[AudioManager] 循環 SFX「{soundName}」的 AudioClip 為空");
            return;
        }

        float sfxMul = 1f;
        if (audioMixer == null)
            sfxMul = SfxPref * MasterPref;

        float ambScale = Mathf.Clamp01(loopingAmbientVolumeScale);
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.clip = s.clip;
        newSource.volume = s.volume * ambScale * sfxMul;
        newSource.pitch = s.pitch;
        newSource.loop = true;
        
        if (sfxSource != null && sfxSource.outputAudioMixerGroup != null)
            newSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;

        newSource.Play();
        loopingSFXDict.Add(resolvedName, newSource);
        loopingSfxBaseVolumeDict[resolvedName] = s.volume;
    }

    /// <summary>目前 Inspector 設定的循環環境音縮放（0～1）。</summary>
    public float LoopingAmbientVolumeScale => Mathf.Clamp01(loopingAmbientVolumeScale);

    public void StopLoopingSFX(string soundName)
    {
        string resolvedName = ResolveSfxAlias(soundName);
        if (loopingSFXDict.TryGetValue(resolvedName, out AudioSource source))
        {
            if (source != null)
            {
                source.Stop();
                Destroy(source);
            }
            loopingSFXDict.Remove(resolvedName);
            loopingSfxBaseVolumeDict.Remove(resolvedName);
        }
    }

    /// <summary>
    /// 直接以 AudioClip 播放循環環境音（不必先登錄在 sfxSounds）。
    /// key 用於避免重複建立相同循環音（同 key 只會有一個）。
    /// </summary>
    public void PlayLoopingSFXFromClip(string key, AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (_videoPlaybackMuteStack > 0) return;
        if (_choicePanelSfxSuppressStack > 0) return;

        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("[AudioManager] PlayLoopingSFXFromClip：key 為空。");
            return;
        }
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] PlayLoopingSFXFromClip：clip 為空。key=" + key);
            return;
        }

        key = key.Trim();
        if (loopingSFXDict.ContainsKey(key)) return;

        float sfxMul = 1f;
        if (audioMixer == null)
            sfxMul = SfxPref * MasterPref;

        float baseVol = Mathf.Clamp(volumeScale, 0f, 2f);

        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.clip = clip;
        newSource.volume = baseVol * sfxMul;
        newSource.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        newSource.loop = true;

        if (sfxSource != null && sfxSource.outputAudioMixerGroup != null)
            newSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;

        newSource.Play();
        loopingSFXDict.Add(key, newSource);
        loopingSfxBaseVolumeDict[key] = baseVol;
    }

    private static string ResolveSfxAlias(string soundName)
    {
        if (string.IsNullOrWhiteSpace(soundName)) return soundName;
        // 舊表誤用「檢錢」為條目名時，對應到正字「撿錢」（勿再把撿錢改成檢錢，否則表內登「撿錢」會播不出）
        if (soundName == "檢錢") return SfxName.PickupCoin;
        // 舊命名相容：去掉「(免費)」「(免費素材網站)」與多餘空格
        if (soundName == "射擊(免費)") return "射擊";
        if (soundName == "被攻擊(免費)") return "被攻擊";
        if (soundName == "呼吸(免費)") return "呼吸";
        if (soundName == "跳水(免費)") return "跳水";
        if (soundName == "走路 (免費)") return "走路";
        if (soundName == "走路") return SfxName.Walk;
        if (soundName == "按鈕") return SfxName.Button;
        if (soundName == "點擊對話") return SfxName.ClickDialogue;
        if (soundName == "骰子") return SfxName.DiceGamble;
        if (soundName == "海底游泳(免費素材網站)") return "海底游泳";
        if (soundName == "水底氣泡(免費)") return "水底氣泡";
        if (soundName == "水下環境音(免費素材網站)") return "水下環境音";
        if (soundName == "發現寶物(免費)") return "發現寶物";
        if (soundName == "果實爆開") return "果實爆炸";
        return soundName;
    }

    public void SetMasterVolume(float volume)
    {
        PlayerPrefs.SetFloat("MasterVolume", volume);
        if (audioMixer != null) 
        {
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
        }
        else
            RefreshNonMixerChannelVolumes();
    }

    public void SetBGMVolume(float volume)
    {
        if (lockBgmVolume)
            volume = Mathf.Clamp01(lockedBgmVolume);

        PlayerPrefs.SetFloat("BGMVolume", volume);
        if (audioMixer != null)
        {
            if (lockBgmVolume)
            {
                audioMixer.SetFloat("BGMVolume", 0f);
                if (bgmSource != null)
                {
                    if (bgmSource.isPlaying)
                        bgmSource.volume = _bgmAssetVolumeScale * volume;
                    else
                        bgmSource.volume = volume;
                }
            }
            else
            {
                audioMixer.SetFloat("BGMVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
                if (bgmSource != null && bgmSource.isPlaying)
                    bgmSource.volume = _bgmAssetVolumeScale;
            }
        }
        else 
        {
            if (bgmSource != null)
            {
                if (bgmSource.isPlaying)
                    bgmSource.volume = _bgmAssetVolumeScale * volume * MasterPref;
                else
                    bgmSource.volume = volume * MasterPref;
            }
        }
    }

    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat("SFXVolume", volume);
        if (audioMixer != null)
        {
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
        }
        else 
        {
            if (sfxSource != null)
                sfxSource.volume = volume * MasterPref;
            if (movementSfxSource != null)
                movementSfxSource.volume = volume * MasterPref;

            float ambScale = Mathf.Clamp01(loopingAmbientVolumeScale);
            foreach (var kvp in loopingSFXDict)
            {
                if (kvp.Value == null) continue;
                Sound snd = System.Array.Find(sfxSounds, sound => sound.name == kvp.Key);
                float baseVol = snd != null
                    ? snd.volume * ambScale
                    : (loopingSfxBaseVolumeDict.TryGetValue(kvp.Key, out float vb) ? vb : 1f);
                kvp.Value.volume = baseVol * volume * MasterPref;
            }
        }
    }
}
