using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 像素黑幕淡入淡出轉場系統
/// 用於一般傳送門的簡單轉場效果
/// </summary>
public class PixelFadeTransition : MonoBehaviour
{
    [Header("轉場設定")]
    [SerializeField] private float fadeInDuration = 0.5f;  // 淡入時間（黑幕出現）
    [SerializeField] private float fadeOutDuration = 0.5f; // 淡出時間（黑幕消失）
    [SerializeField] private float minLoadingTime = 0.3f;  // 最小載入等待時間（確保黑幕可見）
    
    [Header("UI元件（自動生成）")]
    private Image fadeImage; // 全屏黑色遮罩
    private Canvas fadeCanvas;
    
    private static PixelFadeTransition instance;
    public static PixelFadeTransition Instance
    {
        get
        {
            if (instance == null)
            {
                // 如果不存在，創建一個單例
                GameObject go = new GameObject("PixelFadeTransition");
                go.SetActive(true); // 確保是 active 的
                instance = go.AddComponent<PixelFadeTransition>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            // 確保 GameObject 是 active 的
            gameObject.SetActive(true);
            SetupFadeUI();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 設置淡入淡出UI（像素黑幕）
    /// </summary>
    private void SetupFadeUI()
    {
        // 創建Canvas
        fadeCanvas = gameObject.GetComponent<Canvas>();
        if (fadeCanvas == null)
        {
            fadeCanvas = gameObject.AddComponent<Canvas>();
        }
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999; // 確保在最上層
        
        // 添加CanvasScaler以適應不同分辨率
        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // 添加GraphicRaycaster（可選，如果需要點擊檢測）
        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
        
        // 創建全屏Image作為黑幕
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(transform, false);
        
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // 初始透明
        
        // 設置Image為全屏
        RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        // 初始狀態：不可見
        fadeCanvas.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 執行場景轉場（淡入 -> 載入場景 -> 淡出）
    /// </summary>
    public void TransitionToScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[PixelFadeTransition] 場景名稱為空，無法轉場。");
            return;
        }
        sceneName = sceneName.Trim();

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[PixelFadeTransition] 場景「{sceneName}」未加入 Build Settings 或名稱錯誤，無法載入。");
            return;
        }

        if (IsTransitioning)
        {
            Debug.LogWarning("[PixelFadeTransition] 已在轉場中，略過重複請求：" + sceneName);
            return;
        }

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName != "LoadingScene" && activeSceneName != sceneName)
            SceneHistoryManager.EnsureInstance().SetPreviousScene(activeSceneName);

        // 需求：所有場景切換時，下一個場景的血量要沿用上一個場景的數值
        // 這裡做集中保存，覆蓋所有直接呼叫 PixelFadeTransition 的入口（例如 ScenePortalWithUnlock）。
        PlayerStateManager.Instance?.OnSceneTransition();

        // 確保 GameObject 是 active 的，才能啟動協程
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        // 確保 UI 已設置
        if (fadeImage == null || fadeCanvas == null)
        {
            SetupFadeUI();
        }
        
        StartCoroutine(TransitionCoroutine(sceneName));
    }
    
    private IEnumerator TransitionCoroutine(string sceneName)
    {
        IsTransitioning = true;
        try
        {
            if (fadeImage == null || fadeCanvas == null)
                SetupFadeUI();

            fadeCanvas.gameObject.SetActive(true);

            // 1. 淡入（黑幕出現）
            yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"[PixelFadeTransition] LoadSceneAsync 失敗：{sceneName}");
                if (fadeCanvas != null)
                    fadeCanvas.gameObject.SetActive(false);
                yield break;
            }
            operation.allowSceneActivation = false;

            float loadingStartTime = Time.realtimeSinceStartup;

            while (operation.progress < 0.9f)
                yield return null;

            float elapsedTime = Time.realtimeSinceStartup - loadingStartTime;
            if (elapsedTime < minLoadingTime)
                yield return new WaitForSecondsRealtime(minLoadingTime - elapsedTime);

            operation.allowSceneActivation = true;

            while (!operation.isDone)
                yield return null;

            // 3. 淡出（黑幕消失）
            yield return StartCoroutine(Fade(1f, 0f, fadeOutDuration));

            if (fadeCanvas != null)
                fadeCanvas.gameObject.SetActive(false);
        }
        finally
        {
            IsTransitioning = false;
        }
    }
    
    /// <summary>
    /// 淡入淡出動畫
    /// </summary>
    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // 使用unscaledDeltaTime以在暫停時也能工作
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            t = Mathf.SmoothStep(0f, 1f, t);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, endAlpha); // 確保最終值正確
    }
    
    /// <summary>
    /// 檢查是否正在轉場中（可選，用於防止重複觸發）
    /// </summary>
    public bool IsTransitioning { get; private set; }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 場景載入完成後，如果黑幕還在，淡出它
        if (fadeImage != null && fadeImage.color.a > 0)
        {
            StartCoroutine(FadeOutOnSceneLoad());
        }
    }
    
    private IEnumerator FadeOutOnSceneLoad()
    {
        yield return new WaitForSeconds(0.1f); // 等待一幀確保場景已初始化
        if (fadeCanvas != null)
        {
            fadeCanvas.gameObject.SetActive(true);
            yield return StartCoroutine(Fade(fadeImage.color.a, 0f, fadeOutDuration));
            fadeCanvas.gameObject.SetActive(false);
        }
    }
}

