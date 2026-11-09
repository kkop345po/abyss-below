using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public const string LoadingSceneName = "LoadingScene";

    public static string nextSceneName;
    private static string previousSceneName;
    private static bool isDirectLoading = false; // 新增：標記是否直接載入場景

    /// <summary>
    /// 給存檔系統等「無法呼叫 LoadSceneWithLoading 實例方法」的情境使用。
    /// 改為全螢幕黑幕漸層轉場（<see cref="PixelFadeTransition"/>）直接載入目標場景，不再經過 LoadingScene。
    /// </summary>
    public static void BeginLoadThroughLoadingScreen(string targetSceneName)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError("[SceneLoader] BeginLoadThroughLoadingScreen：目標場景名稱為空。");
            return;
        }

        targetSceneName = SanitizeGameSceneName(targetSceneName.Trim());

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != LoadingSceneName)
        {
            previousSceneName = currentScene;
            Debug.Log($"[SceneLoader] BeginLoadThroughLoadingScreen 記錄上一場景：{previousSceneName}");
            SceneHistoryManager.EnsureInstance().SetPreviousScene(previousSceneName);
        }

        isDirectLoading = false;
        nextSceneName = "";
        Debug.Log($"[SceneLoader] 黑幕轉場載入：{targetSceneName}");
        // 轉場保存由 PixelFadeTransition 統一處理，避免重複保存（死亡回城等情境會被第二次保存覆蓋）。
        PixelFadeTransition.Instance.TransitionToScene(targetSceneName);
    }

    /// <summary>（已少用）舊 LoadingScene 用；現改黑幕轉場後通常不會顯示。</summary>
    public static Sprite PendingLoadingScreenSprite { get; set; }

    /// <summary>修正舊存檔或誤填的場景名（例如把 Land 打成 Load）。</summary>
    public static string SanitizeGameSceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return sceneName;
        if (sceneName == "Load")
        {
            Debug.LogWarning("[SceneLoader] 場景名「Load」已自動改為「Land」。請改用正確場景名並重新存檔。");
            return "Land";
        }
        return sceneName;
    }

    [Header("UI 元件")]
    public GameObject loadingUI;
    public Slider progressBar;
    [Tooltip("可選：顯示教學圖／自訂載入圖（與選單 SceneLoader 上的「選單用載入圖」擇一使用）")]
    public UnityEngine.UI.Image loadingScreenImage;

    [Header("從主選單開始遊戲（可選）")]
    [Tooltip("現已改黑幕轉場；此欄僅在仍使用舊 LoadingScene 預製或自訂腳本時有意義。")]
    public Sprite menuPreferredLoadingSprite;

    void Awake()
    {
        SaveSystem.LoadPlayerData();
        Debug.Log($"[SceneLoader] 初始化 - 下一個場景：{nextSceneName}，上一個場景：{previousSceneName}，直接載入：{isDirectLoading}");

        ApplyPendingLoadingArt();
    }

    void Start()
    {
        ApplyPendingLoadingArt();
        if (!isDirectLoading && !string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log($"[SceneLoader] 開始載入場景：{nextSceneName}");
            StartCoroutine(LoadSceneAsync(nextSceneName));
            nextSceneName = ""; // 清除nextSceneName
        }
    }

    /// <summary>
    /// 以海港專用動畫進行轉場（僅限陸地→海域流程使用）
    /// 不進入 LoadingScene，交由 HarborTransitionManager 控制動畫與異步載入。
    /// </summary>
    /// <param name="sceneName">目標場景名稱</param>
    /// <param name="transitionAnimator">可選，自訂 Animator；若為 null 則使用預設</param>
    public void LoadSceneWithHarborAnimation(string sceneName, Animator transitionAnimator = null)
    {
        Debug.Log($"[SceneLoader] HarborAnimation -> {sceneName}");
        if (HarborTransitionManager.Instance != null)
            HarborTransitionManager.Instance.TransitionToScene(sceneName, transitionAnimator);
        else
        {
            Debug.LogWarning("[SceneLoader] HarborTransitionManager.Instance 為 null，改以黑幕轉場載入。");
            LoadSceneWithLoading(sceneName);
        }
    }

    /// <summary>黑幕漸層淡入 → 非同步載入目標場景 → 淡出（不再進入 LoadingScene）。</summary>
    public void LoadSceneWithLoading(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneLoader] LoadSceneWithLoading：場景名稱為空。");
            return;
        }
        sceneName = SanitizeGameSceneName(sceneName.Trim());

        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[SceneLoader] LoadSceneWithLoading（黑幕轉場）當前：{currentScene} → 目標：{sceneName}");

        if (currentScene != LoadingSceneName)
        {
            previousSceneName = currentScene;
            Debug.Log($"[SceneLoader] 設置上一個場景：{previousSceneName}");
            SceneHistoryManager.EnsureInstance().SetPreviousScene(previousSceneName);
        }

        isDirectLoading = false;
        nextSceneName = "";
        // 轉場保存由 PixelFadeTransition 統一處理，避免重複保存（死亡回城等情境會被第二次保存覆蓋）。
        PixelFadeTransition.Instance.TransitionToScene(sceneName);
    }

    public void LoadSceneDirectly(string sceneName)
    {
        Debug.Log($"[SceneLoader] 直接載入場景：{sceneName}");
        isDirectLoading = true;
        nextSceneName = "";
        PlayerStateManager.Instance?.OnSceneTransition();
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        Debug.Log($"[SceneLoader] LoadSceneAsync 開始 - 目標場景：{sceneName}");
        
        if (loadingUI != null) loadingUI.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            if (progressBar != null)
                progressBar.value = progress;

            if (operation.progress >= 0.9f)
            {
                Debug.Log($"[SceneLoader] 載入進度完成，準備激活場景：{sceneName}");
                yield return new WaitForSeconds(0.5f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        yield return null;
        SceneAudioReapply.StopOrphanLoopingSfxAndReapplySceneComponents();

        Debug.Log($"[SceneLoader] 場景載入完成：{sceneName}");
    }

    void ApplyPendingLoadingArt()
    {
        Sprite useSprite = PendingLoadingScreenSprite;
        if (useSprite == null && menuPreferredLoadingSprite != null)
            useSprite = menuPreferredLoadingSprite;
        if (useSprite == null || loadingScreenImage == null)
            return;
        loadingScreenImage.sprite = useSprite;
        loadingScreenImage.enabled = true;
        PendingLoadingScreenSprite = null;
    }
}