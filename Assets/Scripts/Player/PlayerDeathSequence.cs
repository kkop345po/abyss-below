using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 玩家死亡後顯示全螢幕 UI，經過一段時間後載入陸地場景（與 ScenePlayerManager 的 land 預製體一致）。
/// 協程掛在 DontDestroyOnLoad 物件上，避免玩家被場景卸載時中斷。
/// </summary>
public class PlayerDeathSequence : MonoBehaviour
{
    private static PlayerDeathSequence _instance;
    private static bool _running;

    [SerializeField] private string defaultLandSceneName = "Land";

    [Header("Fade Settings")]
    [SerializeField] private float fadeInSeconds = 0.35f;
    [SerializeField] private float fadeOutSeconds = 0.35f;

    // 預設會嘗試從 Resources 載入（不含副檔名）
    // 你只要把資產放到 Assets/Resources/ 下，例如：
    // - Assets/Resources/UI/YOU DIED.png
    // - Assets/Resources/Audio/YOU DIED.mp3
    private const string DefaultDeathSpriteResourcePath = "UI/YOU DIED";
    private const string DefaultDeathSfxResourcePath = "Audio/YOU DIED";

    // 允許外部（例如選單或設定）覆蓋資源路徑
    public static string DeathSpriteResourcePathOverride { get; set; }
    public static string DeathSfxResourcePathOverride { get; set; }

    public static void StartFromPlayerDeath(
        string landSceneName,
        float displaySeconds,
        string messageLine1,
        string messageLine2,
        TMP_FontAsset messageFont)
    {
        if (_running)
            return;

        EnsureInstance();
        landSceneName = string.IsNullOrWhiteSpace(landSceneName)
            ? _instance.defaultLandSceneName
            : landSceneName.Trim();

        _instance.StartCoroutine(_instance.Run(landSceneName, displaySeconds, messageLine1, messageLine2, messageFont));
    }

    private static void EnsureInstance()
    {
        if (_instance != null)
            return;

        var go = new GameObject(nameof(PlayerDeathSequence));
        _instance = go.AddComponent<PlayerDeathSequence>();
        DontDestroyOnLoad(go);
    }

    private IEnumerator Run(
        string landScene,
        float displaySeconds,
        string line1,
        string line2,
        TMP_FontAsset messageFont)
    {
        _running = true;
        GameObject uiRoot = null;
        GameObject audioRoot = null;
        float prevTimeScale = Time.timeScale;
        try
        {
            // 需求：死亡時畫面暫停（物理/動畫/移動停止）
            // UI 淡入淡出與等待一律使用 Realtime/unscaled，因此不受暫停影響。
            Time.timeScale = 0f;

            // 先讓死亡動畫有一點時間顯示，再蓋上死亡 UI
            yield return new WaitForSecondsRealtime(0.35f);

            // 播放死亡音效（即使 Time.timeScale=0 也可以正常播放）
            audioRoot = TryPlayDeathSfx();

            uiRoot = BuildDeathUI(line1, line2, messageFont);

            var group = uiRoot != null ? uiRoot.GetComponent<CanvasGroup>() : null;
            if (group != null)
            {
                group.alpha = 0f;
                yield return FadeCanvasGroup(group, 0f, 1f, Mathf.Max(0.01f, fadeInSeconds));
            }

            float holdSeconds = Mathf.Max(0.05f, displaySeconds - 0.35f - fadeInSeconds - fadeOutSeconds);
            yield return new WaitForSecondsRealtime(holdSeconds);

            if (group != null)
                yield return FadeCanvasGroup(group, 1f, 0f, Mathf.Max(0.01f, fadeOutSeconds));
        }
        finally
        {
            // 確保不會卡在暫停狀態
            Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
            if (uiRoot != null)
                Destroy(uiRoot);
            if (audioRoot != null)
                Destroy(audioRoot);
        }

        PlayerDataManager.ClearPersistedVitalsForFreshSpawn();
        // 死亡回城：下一個場景強制從陸地預設出生點（LandDefault）生成
        SceneSpawnRouter.SetPendingSpawnId(SceneSpawnDefaults.LandSpawnId);
        PlayerStateManager.SuppressNextSceneTransitionSave = true;

        _running = false;

        SceneLoader.BeginLoadThroughLoadingScreen(SceneLoader.SanitizeGameSceneName(landScene));
    }

    private GameObject TryPlayDeathSfx()
    {
        string path = string.IsNullOrWhiteSpace(DeathSfxResourcePathOverride)
            ? DefaultDeathSfxResourcePath
            : DeathSfxResourcePathOverride.Trim();

        var clip = Resources.Load<AudioClip>(path);
        if (clip == null)
        {
            Debug.LogWarning($"[PlayerDeathSequence] 找不到死亡音效 AudioClip，請放到 Resources：{path}");
            return null;
        }

        var go = new GameObject("DeathSfx");
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.ignoreListenerPause = true;
        src.spatialBlend = 0f;
        // 額外保險：若某處把 AudioListener 暫停了，這裡仍能播
        AudioListener.pause = false;
        src.PlayOneShot(clip);
        Destroy(go, Mathf.Max(0.1f, clip.length + 0.1f));
        return go;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float seconds)
    {
        if (group == null) yield break;
        float t = 0f;
        group.alpha = from;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float p = seconds <= 0f ? 1f : Mathf.Clamp01(t / seconds);
            group.alpha = Mathf.Lerp(from, to, p);
            yield return null;
        }
        group.alpha = to;
    }

    private GameObject BuildDeathUI(string line1, string line2, TMP_FontAsset messageFont)
    {
        var root = new GameObject("DeathScreenUI");
        root.transform.SetParent(transform, false);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();
        root.AddComponent<CanvasGroup>();

        var dim = new GameObject("Dim");
        dim.transform.SetParent(root.transform, false);
        var dimImg = dim.AddComponent<Image>();
        // 降低遮罩強度，讓底下死亡動畫仍可看見
        dimImg.color = new Color(0f, 0f, 0f, 0.45f);
        var dimRect = dim.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        // 優先顯示 YOU DIED 圖（若 Resources 內有對應資產）
        var deathSprite = Resources.Load<Sprite>(
            string.IsNullOrWhiteSpace(DeathSpriteResourcePathOverride)
                ? DefaultDeathSpriteResourcePath
                : DeathSpriteResourcePathOverride.Trim());

        if (deathSprite != null)
        {
            var imgGo = new GameObject("YouDiedImage");
            imgGo.transform.SetParent(root.transform, false);
            var img = imgGo.AddComponent<Image>();
            img.sprite = deathSprite;
            // 需求：全螢幕鋪滿
            img.preserveAspect = false;
            img.color = Color.white;

            var r = imgGo.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }
        else
        {
            Debug.LogWarning($"[PlayerDeathSequence] 找不到死亡圖片 Sprite，請放到 Resources：{(string.IsNullOrWhiteSpace(DeathSpriteResourcePathOverride) ? DefaultDeathSpriteResourcePath : DeathSpriteResourcePathOverride.Trim())}");
        }

        return root;
    }
}
