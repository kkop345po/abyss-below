using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    [Header("UI 參考")]
    [Tooltip("整張教學圖片與黑底的父物件 (Panel)")]
    public GameObject tutorialPanel;

    [Header("自動顯示設定")]
    [Tooltip("是否在進入場景(或開場動畫後)自動顯示一次？")]
    public bool autoShowOnStart = true;
    [Tooltip("從場景開始到顯示的延遲時間 (秒)，可用來配合開場動畫長度")]
    public float delayBeforeShow = 2.0f;

    private bool isShowing = false;
    private float timer = 0f;
    private bool hasShownAuto = false;
    /// <summary>僅當本腳本自行 PushPause 時才 Pop，避免與 PauseOnEnable 重複增減。</summary>
    private bool pauseOwnedByThisTutorial;
    private float _lastShowUnscaledTime = -999f;

    void Start()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    void Update()
    {
        // 處理自動顯示邏輯（配合開場動畫）
        if (autoShowOnStart && !hasShownAuto)
        {
            // 使用 unscaledDeltaTime 避免因為某些動畫影響 Time.timeScale
            timer += Time.unscaledDeltaTime;
            if (timer >= delayBeforeShow)
            {
                ShowTutorial();
                hasShownAuto = true; // 確保只自動跳出一次
            }
        }

        // 隨便按一個鍵位後將該圖片消失
        if (isShowing && Input.anyKeyDown)
        {
            CloseTutorial();
        }
    }

    /// <summary>
    /// 供 Button OnClick 等呼叫。注意：按鈕事件裡請拖入「場景中」挂有本腳本的物件，勿拖專案裡的 .cs 檔。
    /// </summary>
    public void ShowTutorial()
    {
        if (tutorialPanel == null)
        {
            Debug.LogWarning("[TutorialUI] 未指派 tutorialPanel，無法顯示教學。請在與此腳本同一個 GameObject（或同一套 UI）上指派教學 Panel。");
            return;
        }

        // 若面板已顯示就不重複 PushPause（避免計數疊加）
        if (tutorialPanel.activeSelf)
            return;

        // 防連點／同幀重入（按鈕 + 自動顯示等）
        if (Time.unscaledTime - _lastShowUnscaledTime < 0.2f)
            return;
        _lastShowUnscaledTime = Time.unscaledTime;

        pauseOwnedByThisTutorial = tutorialPanel.GetComponent<PauseOnEnable>() == null;
        tutorialPanel.SetActive(true);
        isShowing = true;
        if (pauseOwnedByThisTutorial)
            PanelPauseManager.PushPause();
    }

    /// <summary>
    /// 關閉教學圖片
    /// </summary>
    public void CloseTutorial()
    {
        if (tutorialPanel == null) return;

        bool wasActive = tutorialPanel.activeSelf;
        if (!wasActive)
        {
            if (pauseOwnedByThisTutorial)
            {
                PanelPauseManager.PopPause();
                pauseOwnedByThisTutorial = false;
            }
            isShowing = false;
            return;
        }

        tutorialPanel.SetActive(false);
        isShowing = false;
        if (pauseOwnedByThisTutorial)
        {
            PanelPauseManager.PopPause();
            pauseOwnedByThisTutorial = false;
        }

        if (PanelPauseManager.IsPausedByPanel && Time.timeScale == 0f)
            PanelPauseManager.ForceResumeAll();
    }
}
