using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 海港專用動畫轉場管理器
/// 用於海港三個傳送門的特殊動畫轉場
/// </summary>
public class HarborTransitionManager : MonoBehaviour
{
    [Header("動畫設定")]
    [SerializeField] private Animator transitionAnimator; // 轉場動畫控制器
    [SerializeField] private string exitTriggerName = "Exit"; // 出港（或一般）退出動畫觸發器名稱
    [SerializeField] private string enterTriggerName = "Enter"; // 進入動畫觸發器名稱（可選）
    [SerializeField] private AnimationClip exitAnimationClip; // 可選：直接指定出港退出動畫Clip以取得正確時長
    [SerializeField] private float overrideExitDurationSeconds = 11f; // 可選：固定等待秒數（>0 時優先生效）

    [Header("回港（回到陸地）動畫設定")]
    [Tooltip("當目標場景為 Land 時，改用此 Trigger 播放「回港」轉場。")]
    [SerializeField] private string returnToLandExitTriggerName = "Return";
    [Tooltip("若不想用 Trigger，可直接播放某個 Animator State（例如把回港動畫放在 Idle），此欄為 State 名稱。留空則改用 Trigger。")]
    [SerializeField] private string returnToLandPlayStateName = "Idle";
    [Tooltip("可選：回港動畫 Clip，用於取得時長。")]
    [SerializeField] private AnimationClip returnToLandExitAnimationClip;
    [Tooltip("可選：回港固定等待秒數（>0 時優先生效）。")]
    [SerializeField] private float overrideReturnToLandExitDurationSeconds = 10f;
    
    [Header("轉場參數")]
    [SerializeField] private float minAnimationDuration = 1f; // 最小動畫播放時間
    [SerializeField] private float animationBufferTime = 0.5f; // 動畫緩衝時間

    [Header("回港載入時機（動畫快結束就開始載入）")]
    [Tooltip("回港動畫剩下多少秒時就開始在背景 LoadSceneAsync；可讓 10 秒動畫在尾段無縫進 Land。")]
    [SerializeField] private float returnToLandPreloadLeadSeconds = 1f;

    [Header("音效（轉場前先停，避免舊場景聲音帶過來）")]
    [SerializeField] private bool stopAudioBeforeTransition = true;
    [SerializeField] private bool stopBgmBeforeTransition = true;
    [SerializeField] private bool stopLoopingSfxBeforeTransition = true;
    [SerializeField] private bool stopMovementSfxBeforeTransition = true;
    [SerializeField] private bool stopActionSfxBeforeTransition = true;

    [Header("轉場物件收尾")]
    [Tooltip("切到新場景後，自動把本次使用的 Animator 物件關閉，避免回到陸地後仍顯示。")]
    [SerializeField] private bool deactivateAnimatorGameObjectAfterTransition = true;
    [Tooltip("關閉前延遲（秒），避免與場景初始化同幀造成閃一下。")]
    [SerializeField] private float deactivateDelaySeconds = 0.05f;

    [Header("遊戲時間")]
    [Tooltip("轉場動畫播放期間將 Time.timeScale 設為 0；請將轉場 Animator 設為 Update Mode = Unscaled Time（或由本腳本自動設定）。")]
    [SerializeField] private bool pauseGameTimeDuringTransition = true;
    
    [Header("當前狀態")]
    [SerializeField] private bool isTransitioning = false;

    private bool _harborTimeFreezeActive;
    private float _savedTimeScaleForHarbor;
    
    private static HarborTransitionManager instance;
    public static HarborTransitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("HarborTransitionManager");
                instance = go.AddComponent<HarborTransitionManager>();
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
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 使用動畫轉場到指定場景
    /// </summary>
    public void TransitionToScene(string sceneName, Animator animator = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[HarborTransition] 轉場正在進行中，忽略重複請求");
            return;
        }
        
        // 使用傳入的Animator或使用預設的
        Animator targetAnimator = animator != null ? animator : transitionAnimator;
        
        if (targetAnimator == null)
        {
            Debug.LogError("[HarborTransition] Animator未設定！使用普通場景載入");
            SceneManager.LoadScene(sceneName);
            return;
        }
        
        StartCoroutine(TransitionCoroutine(sceneName, targetAnimator));
    }

    void ApplyHarborTimeFreezeIfNeeded(Animator animator)
    {
        _harborTimeFreezeActive = false;
        if (!pauseGameTimeDuringTransition) return;

        _savedTimeScaleForHarbor = Time.timeScale;
        Time.timeScale = 0f;
        _harborTimeFreezeActive = true;

        if (animator != null)
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    void RestoreHarborTimeFreezeIfNeeded()
    {
        if (!_harborTimeFreezeActive) return;
        _harborTimeFreezeActive = false;
        Time.timeScale = Mathf.Approximately(_savedTimeScaleForHarbor, 0f) ? 1f : _savedTimeScaleForHarbor;
    }
    
    private IEnumerator TransitionCoroutine(string sceneName, Animator animator)
    {
        isTransitioning = true;
        
        // 保存玩家狀態和位置
        SavePlayerData();

        ApplyHarborTimeFreezeIfNeeded(animator);

        bool isReturningToLand = sceneName == "Land";
        string triggerToUse = isReturningToLand ? returnToLandExitTriggerName : exitTriggerName;
        AnimationClip clipToUse = isReturningToLand ? returnToLandExitAnimationClip : exitAnimationClip;
        float overrideDurationToUse = isReturningToLand ? overrideReturnToLandExitDurationSeconds : overrideExitDurationSeconds;

        if (stopAudioBeforeTransition)
            StopGameAudioForTransition();
        
        // 1. 播放退出動畫
        if (!string.IsNullOrEmpty(triggerToUse) && HasParameter(animator, triggerToUse))
        {
            // 確保物件啟用並可見（避免在停用物件上觸發）
            if (!animator.gameObject.activeInHierarchy)
                animator.gameObject.SetActive(true);
            // 若沒有綁定 Controller，則跳過動畫以避免報錯
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("[HarborTransition] 傳入的 Animator 沒有綁定 Controller，改用無動畫切場");
                AsyncOperation opNoAnim = SceneManager.LoadSceneAsync(sceneName);
                opNoAnim.allowSceneActivation = true;
                while (!opNoAnim.isDone)
                    yield return null;
                RestoreHarborTimeFreezeIfNeeded();
                isTransitioning = false;
                yield break;
            }

            // 確保 Animator 不會卡在 speed=0 或舊狀態；回港常見「顯示了但不播放」多半是這裡。
            animator.enabled = true;
            animator.speed = 1f;
            animator.Rebind();
            animator.Update(0f);

            // 回港：優先用指定 State 直接播放（例如把回港動畫放在 Idle），避免 Trigger/Transition 設定錯導致不播。
            if (isReturningToLand && !string.IsNullOrEmpty(returnToLandPlayStateName))
            {
                animator.Play(returnToLandPlayStateName, 0, 0f);
                Debug.Log($"[HarborTransition] 回港播放 State：{returnToLandPlayStateName}");
            }
            else
            {
                animator.SetTrigger(triggerToUse);
                Debug.Log($"[HarborTransition] 播放轉場動畫：{triggerToUse}（ReturningToLand={isReturningToLand}）");
            }
        }
        else
        {
            // 未設定或找不到觸發參數：僅確保物件啟用，直接依照動畫時長等待
            if (!animator.gameObject.activeInHierarchy)
                animator.gameObject.SetActive(true);

            if (isReturningToLand &&
                animator.runtimeAnimatorController != null &&
                !string.IsNullOrEmpty(returnToLandPlayStateName))
            {
                animator.enabled = true;
                animator.speed = 1f;
                animator.Rebind();
                animator.Update(0f);
                animator.Play(returnToLandPlayStateName, 0, 0f);
                Debug.Log($"[HarborTransition] 回港播放 State（無 Trigger）：{returnToLandPlayStateName}");
            }
        }
        
        // 2. 決定等待時長：優先使用固定秒數，其次使用指定 Clip 長度，最後落到最小播放時間
        float clipLen = (clipToUse != null) ? clipToUse.length : 0f;
        float baseDuration = overrideDurationToUse > 0f ? overrideDurationToUse : clipLen;
        float animationDuration = Mathf.Max(baseDuration, minAnimationDuration, 0f);
        
        // 3. 開始載入時機：回港在動畫尾段就先 LoadSceneAsync（你說的「10秒動畫快結束時載入」）。
        float preloadLead = isReturningToLand ? Mathf.Max(0f, returnToLandPreloadLeadSeconds) : 0f;
        float startLoadAt = Mathf.Max(0f, animationDuration - preloadLead);
        float t = 0f;

        AsyncOperation operation = null;
        while (t < startLoadAt)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        // 4. 確保動畫整段播完（剩餘時間），同時等待載入至少到 0.9
        while (t < animationDuration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        while (operation.progress < 0.9f)
            yield return null;
        
        // 等待一點緩衝時間確保動畫效果完整
        yield return new WaitForSecondsRealtime(animationBufferTime);
        
        // 4. 激活場景
        operation.allowSceneActivation = true;
        
        // 等待場景完全載入
        while (!operation.isDone)
        {
            yield return null;
        }

        // 新場景已啟用：恢復遊戲時間（後續 Wait 須用 Realtime，避免 timeScale 仍為 0 時卡死）
        RestoreHarborTimeFreezeIfNeeded();

        // 轉場前會 StopBGM，且動畫 Event 可能在場景載入後再次 StopBGM；一律重套用目標場景的 SceneBackgroundMusic／SceneAudioSettings。
        yield return null;
        SceneAudioReapply.StopOrphanLoopingSfxAndReapplySceneComponents();
        
        // 5. 在新場景中播放進入動畫（如果需要且參數存在）
        // 注意：這需要在目標場景中也有一個HarborTransitionManager或類似的設置
        yield return new WaitForSecondsRealtime(0.1f); // 等待場景初始化（與 timeScale 無關）
        
        if (!string.IsNullOrEmpty(enterTriggerName))
        {
            // 嘗試在新場景中找到Animator並播放進入動畫
            Animator enterAnimator = FindObjectOfType<Animator>();
            if (enterAnimator != null && HasParameter(enterAnimator, enterTriggerName))
            {
                enterAnimator.SetTrigger(enterTriggerName);
                Debug.Log($"[HarborTransition] 在新場景播放進入動畫：{enterTriggerName}");
            }
        }

        // 6. 收尾：關閉本次使用的轉場物件（避免回到陸地後仍顯示）
        if (deactivateAnimatorGameObjectAfterTransition && animator != null)
        {
            if (deactivateDelaySeconds > 0f)
                yield return new WaitForSecondsRealtime(deactivateDelaySeconds);
            if (animator.gameObject != null)
                animator.gameObject.SetActive(false);
        }
        
        isTransitioning = false;
        Debug.Log($"[HarborTransition] 轉場完成：已載入 {sceneName}");
    }
    
    /// <summary>
    /// 獲取動畫時長
    /// </summary>
    private bool HasParameter(Animator animator, string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName))
            return false;
        // 若 Animator 沒有綁定 Controller，讀取 parameters 會噴：
        // "Animator is not playing an AnimatorController"
        if (animator.runtimeAnimatorController == null)
            return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }
    
    /// <summary>
    /// 保存玩家數據
    /// </summary>
    private void SavePlayerData()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // 1. 記錄場景歷史，確保可以返回
        SceneHistoryManager.EnsureInstance().SetPreviousScene(currentScene);
        Debug.Log($"[HarborTransition] 已記錄上一場景：{currentScene}");

        // 2. 保存玩家位置，確保返回時在正確位置
        if (PlayerPositionManager.Instance != null)
        {
            PlayerPositionManager.Instance.SaveCurrentPosition();
        }
        
        // 3. 保存玩家狀態（血量、氧氣等）
        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.OnSceneTransition();
        }

        // 4. 特殊處理：如果是從陸地出發，將氧氣補滿
        if (currentScene == "Land")
        {
            RefillOxygenToMax();
        }
    }

    void StopGameAudioForTransition()
    {
        var audio = AudioManager.GetInstance();
        if (audio == null) return;
        if (stopBgmBeforeTransition)
            audio.StopBGM();
        if (stopLoopingSfxBeforeTransition)
            audio.StopAllLoopingSFX();
        if (stopMovementSfxBeforeTransition)
            audio.StopMovementSFX();
        if (stopActionSfxBeforeTransition)
            audio.StopActionSFX();
    }

    private void RefillOxygenToMax()
    {
        int level = Mathf.Clamp(PlayerDataManager.OxygenLevel, 0, PlayerDataManager.MaxLevel);
        // 氧氣等級表 (需與 OxygenManager 保持一致)
        // Lv0/Lv1=500, Lv2=2000, Lv3=4000, Lv4=6000, Lv5=10000
        int[] table = { 500, 500, 2000, 4000, 6000, 10000 };
        float maxOxygen = table[Mathf.Clamp(level, 0, table.Length - 1)];
        
        PlayerDataManager.CurrentOxygen = maxOxygen;
        PlayerDataManager.Save();
        Debug.Log($"[HarborTransition] 從陸地出發，氧氣已補滿至：{maxOxygen}");
    }
    
    /// <summary>
    /// 設置轉場動畫器（用於動態設置）
    /// </summary>
    public void SetTransitionAnimator(Animator animator)
    {
        transitionAnimator = animator;
    }
}

