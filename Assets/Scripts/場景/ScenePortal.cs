using UnityEngine;
using UnityEngine.SceneManagement;

public enum TransitionType
{
    LoadingScene,      // 黑幕漸層轉場（經 SceneLoader，不再進 LoadingScene 場景）
    PixelFade,         // 像素黑幕淡入淡出
    HarborAnimation,   // 海港專用動畫
    DirectLoad         // 直接載入（無轉場）
}

public class ScenePortal : MonoBehaviour
{
    [Header("目標場景")]
    public string targetSceneName;

    [Header("目標入口（可選）")]
    [Tooltip("指定目標場景的生成點 SpawnId（需在目標場景放置 SpawnPoint 並填相同 id）。留空則使用 Default SpawnPoint 或舊的返回位置策略。")]
    public string targetSpawnId;

    [Header("切換方式")]
    public TransitionType transitionType = TransitionType.PixelFade; // 轉場類型
    public bool useLoadingScene = true; // 舊版兼容（當transitionType為LoadingScene時使用）
    public bool requireInteractKey = false;
    public KeyCode interactKey = KeyCode.E;

    [Header("像素淡入淡出設定（當transitionType為PixelFade時）")]
    public bool usePixelFade = true; // 是否使用像素黑幕淡入淡出

    [Header("海港動畫設定（當transitionType為HarborAnimation時）")]
    [Tooltip("出港（或一般海港轉場）用 Animator；目標不是 Land 時使用。")]
    public Animator harborAnimator; // 海港動畫控制器（可選，會自動查找）

    [Tooltip("回港（回到 Land）用 Animator；目標為 Land 時優先使用。若留空則回退使用 harborAnimator。")]
    public Animator returnHarborAnimator;

    [Header("提示（可選）")]
    public GameObject interactPromptUI; // 進入範圍顯示 Press E 提示

    private bool isPlayerInside = false;
    private float _nextTransferAllowedTime = -999f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            if (interactPromptUI != null)
                interactPromptUI.SetActive(requireInteractKey);
            if (!requireInteractKey)
                Transfer();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            if (interactPromptUI != null)
                interactPromptUI.SetActive(false);
        }
    }

    void Update()
    {
        if (requireInteractKey && isPlayerInside && Input.GetKeyDown(interactKey))
        {
            AudioManager.GetInstance()?.PlayButtonSfx();
            Transfer();
        }
    }

    private void Transfer()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[ScenePortal] 未設定目標場景名稱");
            return;
        }

        if (Time.unscaledTime < _nextTransferAllowedTime)
            return;
        _nextTransferAllowedTime = Time.unscaledTime + 0.85f;

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != "LoadingScene")
            SceneHistoryManager.EnsureInstance().SetPreviousScene(currentScene);

        // 設定下一場景生成點（一次性）
        if (!string.IsNullOrEmpty(targetSpawnId))
            SceneSpawnRouter.SetPendingSpawnId(targetSpawnId);
        
        // 記錄傳送門位置（用於返回時在傳送門附近生成）
        if (PlayerPositionManager.Instance != null)
        {
            // 記錄：從當前場景通過此傳送門去目標場景的位置
            Vector3 portalPosition = transform.position;
            PlayerPositionManager.Instance.RecordPortalPosition(currentScene, targetSceneName, portalPosition);
            Debug.Log($"[ScenePortal] 記錄傳送門位置：{portalPosition} (從 {currentScene} 到 {targetSceneName})");
            
            // 也保存當前場景的玩家位置（一般位置記錄）
            PlayerPositionManager.Instance.SaveCurrentPosition();
        }

        // 保存玩家狀態（血量、氧氣、體力）
        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.OnSceneTransition();
            Debug.Log($"[ScenePortal] 保存玩家狀態後準備傳送到：{targetSceneName}");
        }

        // 根據轉場類型選擇載入方式
        switch (transitionType)
        {
            case TransitionType.LoadingScene:
                var loader = FindObjectOfType<SceneLoader>();
                if (useLoadingScene && loader != null)
                {
                    loader.LoadSceneWithLoading(targetSceneName);
                }
                else
                {
                    SceneManager.LoadScene(targetSceneName);
                }
                break;
                
            case TransitionType.PixelFade:
                // 使用像素黑幕淡入淡出
                if (usePixelFade && PixelFadeTransition.Instance != null)
                {
                    PixelFadeTransition.Instance.TransitionToScene(targetSceneName);
                }
                else
                {
                    // 如果PixelFadeTransition未初始化，直接載入
                    Debug.LogWarning("[ScenePortal] PixelFadeTransition未初始化，使用直接載入");
                    SceneManager.LoadScene(targetSceneName);
                }
                break;
                
            case TransitionType.HarborAnimation:
                // 使用海港專用動畫
                bool isReturningToLand = targetSceneName == "Land";
                Animator configured = isReturningToLand
                    ? (returnHarborAnimator != null ? returnHarborAnimator : harborAnimator)
                    : harborAnimator;
                Animator animatorToUse = configured != null ? configured : GetComponent<Animator>();
                if (HarborTransitionManager.Instance != null)
                {
                    HarborTransitionManager.Instance.TransitionToScene(targetSceneName, animatorToUse);
                }
                else
                {
                    Debug.LogWarning("[ScenePortal] HarborTransitionManager未初始化，使用直接載入");
                    SceneManager.LoadScene(targetSceneName);
                }
                break;
                
            case TransitionType.DirectLoad:
                // 直接載入，無轉場
                SceneManager.LoadScene(targetSceneName);
                break;
        }
    }

    /// <summary>給其他腳本（例如帶條件的傳送門）呼叫。</summary>
    public void TriggerTransfer()
    {
        Transfer();
    }
}


