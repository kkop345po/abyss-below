using UnityEngine;

/// <summary>
/// 海港專用傳送門
/// 自動配置為使用海港動畫轉場
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ScenePortal))]
public class HarborPortal : MonoBehaviour
{
    [Header("動畫設定")]
    [Tooltip("轉場動畫控制器（留空則自動查找場景中的Animator）")]
    public Animator transitionAnimator;

    [Tooltip("回到 Land 時用的轉場 Animator（留空則沿用上方 transitionAnimator）")]
    public Animator returnToLandAnimator;
    
    [Tooltip("退出動畫觸發器名稱")]
    public string exitTriggerName = "Exit";
    
    [Tooltip("進入動畫觸發器名稱（可選）")]
    public string enterTriggerName = "Enter";
    
    [Header("目標場景")]
    public string targetSceneName;

    private ScenePortal portal;

    void Awake()
    {
        EnsurePortal();
        ApplySettings();
    }

    void Reset()
    {
        EnsurePortal();
        ApplySettings();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            EnsurePortal();
            ApplySettings();
        }
    }

    private void EnsurePortal()
    {
        if (portal == null)
            portal = GetComponent<ScenePortal>();
        
        if (portal == null)
        {
            portal = gameObject.AddComponent<ScenePortal>();
        }
    }

    private void ApplySettings()
    {
        if (portal == null) return;
        
        // 設置目標場景
        portal.targetSceneName = targetSceneName;
        
        // 設置為使用海港動畫轉場
        portal.transitionType = TransitionType.HarborAnimation;
        
        // 設置動畫控制器
        if (transitionAnimator == null)
        {
            // 嘗試查找場景中的Animator（可能是動畫系統的一部分）
            transitionAnimator = FindObjectOfType<Animator>();
        }
        portal.harborAnimator = transitionAnimator;
        portal.returnHarborAnimator = returnToLandAnimator != null ? returnToLandAnimator : transitionAnimator;
        
        // 如果找到了Animator，更新HarborTransitionManager
        if (transitionAnimator != null && HarborTransitionManager.Instance != null)
        {
            HarborTransitionManager.Instance.SetTransitionAnimator(transitionAnimator);
        }
    }
    
    /// <summary>
    /// 設置目標場景（外部調用）
    /// </summary>
    public void SetTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
        ApplySettings();
    }
    
    /// <summary>
    /// 設置動畫控制器（外部調用）
    /// </summary>
    public void SetAnimator(Animator animator)
    {
        transitionAnimator = animator;
        ApplySettings();
    }
}

