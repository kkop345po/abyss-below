using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 釣魚 UI 總控：選擇 → 演出（GameplayPanel + UI 動畫）→ 結算 → 結束。
/// </summary>
public class FishingUIController : MonoBehaviour
{
    public static FishingUIController Instance { get; private set; }

    [Header("根節點")]
    [SerializeField] private GameObject rootPanel;

    [Header("選擇階段")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private TextMeshProUGUI choiceTitleText;
    [SerializeField] private TextMeshProUGUI choiceDescriptionText;
    [SerializeField] private Button startFishingButton;
    [SerializeField] private Button cancelButton;

    [Header("演出階段（動畫 + QTE，對齊賭博骰子區）")]
    [SerializeField] private GameObject gameplayPanel;

    [Header("結束階段")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TextMeshProUGUI endSummaryText;
    [SerializeField] private Button fishAgainButton;
    [SerializeField] private Button closeEndButton;

    [Header("系統")]
    [SerializeField] private FishingManager fishingManager;

    [Header("時間")]
    [Tooltip("開啟釣魚 UI 時暫停場景 Time.timeScale（與商店面板相同）")]
    [SerializeField] private bool pauseWorldTimeWhileOpen = true;

    [Header("文案")]
    [SerializeField] private string defaultTitle = "釣魚";
    [SerializeField] private string defaultDescription =
        "要在此處釣魚嗎？\n開始後請留意 QTE，在時限內按下 E。";

    string _spotDisplayName;
    bool _waitingEndChoice;
    bool _choseFishAgain;
    Coroutine _openFlow;
    bool _worldTimePaused;

    public bool IsOpen => rootPanel != null && rootPanel.activeSelf;
    public bool BlocksGameplayInput =>
        IsOpen || (fishingManager != null && fishingManager.IsFishing);

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[FishingUIController] 場景內有多個實例。");
        Instance = this;

        if (startFishingButton != null)
            startFishingButton.onClick.AddListener(OnStartFishingClicked);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
        if (fishAgainButton != null)
            fishAgainButton.onClick.AddListener(OnFishAgainClicked);
        if (closeEndButton != null)
            closeEndButton.onClick.AddListener(OnCloseEndClicked);

        CloseRootImmediate();
    }

    void OnDestroy()
    {
        PopWorldPause();
        if (Instance == this)
            Instance = null;
    }

    /// <summary>由釣魚點 E 互動呼叫。</summary>
    public void OpenFromSpot(string spotDisplayName = null)
    {
        if (fishingManager != null && fishingManager.IsFishing)
            return;

        _spotDisplayName = string.IsNullOrWhiteSpace(spotDisplayName) ? defaultTitle : spotDisplayName.Trim();

        if (rootPanel != null)
            rootPanel.SetActive(true);

        PushWorldPause();
        ResetChoicePanel();
        SetPanel(gameplayPanel, false);
        SetPanel(endPanel, false);
    }

    public void EnterGameplayPhase()
    {
        SetPanel(choicePanel, false);
        SetPanel(endPanel, false);
        SetPanel(gameplayPanel, true);
    }

    public void ExitGameplayPhase()
    {
        SetPanel(gameplayPanel, false);
    }

    public void CloseRoot()
    {
        if (_openFlow != null)
        {
            StopCoroutine(_openFlow);
            _openFlow = null;
        }

        CloseRootImmediate();
    }

    void CloseRootImmediate()
    {
        PopWorldPause();
        SetPanel(choicePanel, false);
        SetPanel(gameplayPanel, false);
        SetPanel(endPanel, false);
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    void PushWorldPause()
    {
        if (!pauseWorldTimeWhileOpen || _worldTimePaused)
            return;

        PanelPauseManager.PushPause();
        _worldTimePaused = true;
    }

    void PopWorldPause()
    {
        if (!_worldTimePaused)
            return;

        PanelPauseManager.PopPause();
        _worldTimePaused = false;
    }

    void ResetChoicePanel()
    {
        if (choiceTitleText != null)
            choiceTitleText.text = _spotDisplayName ?? defaultTitle;

        if (choiceDescriptionText != null)
            choiceDescriptionText.text = defaultDescription;

        SetPanel(choicePanel, true);
        AudioManager.GetInstance()?.PlayButtonSfx();
    }

    void OnStartFishingClicked()
    {
        if (fishingManager == null)
        {
            Debug.LogWarning("[FishingUIController] 缺少 FishingManager。");
            return;
        }

        if (fishingManager.IsFishing)
            return;

        AudioManager.GetInstance()?.PlayButtonSfx();

        if (_openFlow != null)
            StopCoroutine(_openFlow);

        _openFlow = StartCoroutine(RunFishingSessionCoroutine());
    }

    void OnCancelClicked()
    {
        AudioManager.GetInstance()?.PlayButtonSfx();
        CloseRoot();
    }

    void OnFishAgainClicked()
    {
        AudioManager.GetInstance()?.PlayButtonSfx();
        _choseFishAgain = true;
        _waitingEndChoice = false;
    }

    void OnCloseEndClicked()
    {
        AudioManager.GetInstance()?.PlayButtonSfx();
        _choseFishAgain = false;
        _waitingEndChoice = false;
    }

    IEnumerator RunFishingSessionCoroutine()
    {
        FishingSessionResult sessionResult = default;
        bool completed = false;

        fishingManager.RunFishingSession(r =>
        {
            sessionResult = r;
            completed = true;
        });

        yield return new WaitUntil(() => completed);

        _openFlow = null;

        if (endSummaryText != null)
            endSummaryText.text = BuildEndSummary(sessionResult);

        SetPanel(endPanel, true);
        _waitingEndChoice = true;
        _choseFishAgain = false;
        yield return new WaitUntil(() => !_waitingEndChoice);

        SetPanel(endPanel, false);

        if (_choseFishAgain)
        {
            if (rootPanel != null)
                rootPanel.SetActive(true);
            _openFlow = StartCoroutine(RunFishingSessionCoroutine());
        }
        else
        {
            CloseRoot();
        }
    }

    static string BuildEndSummary(FishingSessionResult r)
    {
        if (!r.QteSuccess)
            return "本輪沒有釣到東西。";

        if (r.GotFish)
            return string.IsNullOrEmpty(r.ItemDisplayName)
                ? "本輪釣魚成功！"
                : $"本輪釣到了 {r.ItemDisplayName}！";

        return string.IsNullOrEmpty(r.ItemDisplayName)
            ? "本輪釣到了垃圾。"
            : $"本輪釣到了 {r.ItemDisplayName}。";
    }

    static void SetPanel(GameObject panel, bool on)
    {
        if (panel != null)
            panel.SetActive(on);
    }
}
