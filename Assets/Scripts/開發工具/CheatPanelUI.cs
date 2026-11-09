using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// 作弊系統UI面板
/// 提供圖形界面操作所有作弊功能
/// </summary>
public class CheatPanelUI : MonoBehaviour
{
#if UNITY_EDITOR

    [Header("面板設定")]
    public GameObject panel;
    public KeyCode toggleKey = KeyCode.F12;
    
    [Header("訊息顯示")]
    public TextMeshProUGUI messageText;
    public float messageDisplayTime = 2f;
    
    [Header("狀態顯示")]
    public TextMeshProUGUI statusText;
    
    [Header("金幣控制")]
    public TMP_InputField coinInputField;
    public Button addCoins100Button;
    public Button addCoins1000Button;
    public Button addCoins10000Button;
    public Button setCoinsButton;
    
    [Header("生命值控制")]
    public Button fullHealthButton;
    public Button toggleInvincibleButton;
    
    [Header("資源控制")]
    public Button fullRestoreButton;
    public Button toggleInfiniteOxygenButton;
    public Button toggleInfiniteStaminaButton;
    
    [Header("升級控制")]
    public TMPro.TMP_Dropdown upgradeTypeDropdown;
    public TMP_InputField upgradeLevelInputField;
    public Button setUpgradeLevelButton;
    public Button maxAllUpgradesButton;
    
    [Header("傳送控制")]
    public TMPro.TMP_Dropdown sceneDropdown;
    public Button teleportToSceneButton;
    public Button teleportToMouseButton;
    public TMP_InputField teleportXInput;
    public TMP_InputField teleportYInput;
    public Button teleportToPositionButton;
    [Tooltip("新增：向前傳送（依玩家面向方向）")]
    public TMP_InputField teleportForwardMetersInput;
    public Button teleportForwardButton;
    
    [Header("其他功能")]
    public Button clearAllCheatsButton;
    public Button refreshStatusButton;
    [Tooltip("可選：清除跨場景「寶箱已開」紀錄（快捷鍵：F9 與重置 Boss 一併執行）")]
    public Button resetChestPersistenceButton;

    [Header("舊版場景傳送 UI（自動隱藏）")]
    public Transform sceneTeleportButtonsRoot;
    
    private bool isPanelOpen = false;
    private Coroutine messageCoroutine;
    
    void Start()
    {
        if (panel != null)
            panel.SetActive(false);
        
        SetupButtons();
        SetupDropdowns();
        RefreshSceneList();
        BuildSceneTeleportButtons();
    }

    void BuildSceneTeleportButtons()
    {
        if (sceneTeleportButtonsRoot != null) sceneTeleportButtonsRoot.gameObject.SetActive(false);
    }
    
    void Update()
    {
        // 更新狀態顯示
        if (statusText != null && isPanelOpen)
        {
            UpdateStatusDisplay();
        }
        
        // ESC 關閉面板
        if (isPanelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePanel();
        }
    }
    
    private void SetupButtons()
    {
        // 金幣按鈕
        if (addCoins100Button != null)
            addCoins100Button.onClick.AddListener(() => CheatManager.Instance?.AddCoins(100));
        
        if (addCoins1000Button != null)
            addCoins1000Button.onClick.AddListener(() => CheatManager.Instance?.AddCoins(1000));
        
        if (addCoins10000Button != null)
            addCoins10000Button.onClick.AddListener(() => CheatManager.Instance?.AddCoins(10000));
        
        if (setCoinsButton != null)
            setCoinsButton.onClick.AddListener(OnSetCoins);
        
        // 生命值按鈕
        if (fullHealthButton != null)
            fullHealthButton.onClick.AddListener(() => CheatManager.Instance?.FullRestore());
        
        if (toggleInvincibleButton != null)
            toggleInvincibleButton.onClick.AddListener(() => CheatManager.Instance?.ToggleInvincible());
        
        // 資源按鈕
        if (fullRestoreButton != null)
            fullRestoreButton.onClick.AddListener(() => CheatManager.Instance?.FullRestore());
        
        if (toggleInfiniteOxygenButton != null)
            toggleInfiniteOxygenButton.onClick.AddListener(() => CheatManager.Instance?.ToggleInfiniteOxygen());
        
        if (toggleInfiniteStaminaButton != null)
            toggleInfiniteStaminaButton.onClick.AddListener(() => CheatManager.Instance?.ToggleInfiniteStamina());
        
        // 升級按鈕
        if (setUpgradeLevelButton != null)
            setUpgradeLevelButton.onClick.AddListener(OnSetUpgradeLevel);
        
        if (maxAllUpgradesButton != null)
            maxAllUpgradesButton.onClick.AddListener(MaxAllUpgrades);
        
        // 傳送按鈕
        if (teleportToSceneButton != null)
            teleportToSceneButton.onClick.AddListener(OnTeleportToScene);
        
        if (teleportToMouseButton != null)
            teleportToMouseButton.onClick.AddListener(() => CheatManager.Instance?.TeleportToMouse());
        
        if (teleportToPositionButton != null)
            teleportToPositionButton.onClick.AddListener(OnTeleportToPosition);

        if (teleportForwardButton != null)
            teleportForwardButton.onClick.AddListener(OnTeleportForward);
        
        // 其他按鈕
        if (clearAllCheatsButton != null)
            clearAllCheatsButton.onClick.AddListener(() => CheatManager.Instance?.ClearAllCheats());
        
        if (refreshStatusButton != null)
            refreshStatusButton.onClick.AddListener(UpdateStatusDisplay);

        if (resetChestPersistenceButton != null)
            resetChestPersistenceButton.onClick.AddListener(() => CheatManager.Instance?.ResetAllChestPersistence());
    }
    
    private void SetupDropdowns()
    {
        // 升級類型下拉選單
        if (upgradeTypeDropdown != null)
        {
            upgradeTypeDropdown.ClearOptions();
            upgradeTypeDropdown.AddOptions(new System.Collections.Generic.List<string> 
            { 
                "速度", "氧氣效率", "體力" 
            });
        }
        
        // 場景下拉選單（在RefreshSceneList中設置）
    }
    
    private void RefreshSceneList()
    {
        if (sceneDropdown != null) sceneDropdown.gameObject.SetActive(false);
        if (teleportToSceneButton != null) teleportToSceneButton.gameObject.SetActive(false);
    }
    
    public void TogglePanel()
    {
        isPanelOpen = !isPanelOpen;
        if (panel != null)
        {
            panel.SetActive(isPanelOpen);
            
            // 暫停/恢復遊戲
            Time.timeScale = isPanelOpen ? 0f : 1f;
            
            // 更新狀態顯示
            if (isPanelOpen)
            {
                UpdateStatusDisplay();
            }
        }
    }
    
    private void OnSetCoins()
    {
        if (coinInputField != null && int.TryParse(coinInputField.text, out int amount))
        {
            CheatManager.Instance?.SetCoins(amount);
        }
        else
        {
            ShowMessage("請輸入有效的數字");
        }
    }
    
    private void OnSetUpgradeLevel()
    {
        if (upgradeTypeDropdown != null && upgradeLevelInputField != null)
        {
            UpgradeManager.UpgradeType type = (UpgradeManager.UpgradeType)upgradeTypeDropdown.value;
            if (int.TryParse(upgradeLevelInputField.text, out int level))
            {
                CheatManager.Instance?.SetUpgradeLevel(type, level);
            }
            else
            {
                ShowMessage("請輸入有效的等級（0-5）");
            }
        }
    }
    
    private void MaxAllUpgrades()
    {
        int maxLevel = PlayerDataManager.MaxLevel;
        CheatManager.Instance?.SetUpgradeLevel(UpgradeManager.UpgradeType.Speed, maxLevel);
        CheatManager.Instance?.SetUpgradeLevel(UpgradeManager.UpgradeType.OxygenEfficiency, maxLevel);
        CheatManager.Instance?.SetUpgradeLevel(UpgradeManager.UpgradeType.Stamina, maxLevel);
        ShowMessage("所有升級已達最大等級");
    }
    
    private void OnTeleportToScene()
    {
        // 場景作弊傳送已移除。
    }
    
    private void OnTeleportToPosition()
    {
        if (teleportXInput != null && teleportYInput != null)
        {
            if (float.TryParse(teleportXInput.text, out float x) && 
                float.TryParse(teleportYInput.text, out float y))
            {
                Vector3 pos = new Vector3(x, y, 0);
                CheatManager.Instance?.TeleportToPosition(pos);
            }
            else
            {
                ShowMessage("請輸入有效的座標");
            }
        }
    }

    private void OnTeleportForward()
    {
        float meters = 6f;
        if (teleportForwardMetersInput != null && !string.IsNullOrWhiteSpace(teleportForwardMetersInput.text))
        {
            if (!float.TryParse(teleportForwardMetersInput.text, out meters))
            {
                ShowMessage("請輸入有效距離（公尺）");
                return;
            }
        }
        CheatManager.Instance?.TeleportForward(meters);
    }
    
    private void UpdateStatusDisplay()
    {
        if (statusText == null || CheatManager.Instance == null) return;
        
        string status = "=== 作弊狀態 ===\n";
        status += $"無敵模式: {(CheatManager.Instance.IsInvincible ? "開啟" : "關閉")}\n";
        status += $"無限氧氣: {(CheatManager.Instance.IsInfiniteOxygen ? "開啟" : "關閉")}\n";
        status += $"無限體力: {(CheatManager.Instance.IsInfiniteStamina ? "開啟" : "關閉")}\n";
        status += $"速度倍數: {CheatManager.Instance.SpeedMultiplier}x\n";
        status += $"\n=== 遊戲狀態 ===\n";
        status += $"金幣: {PlayerDataManager.Coins}\n";
        status += $"速度等級: {PlayerDataManager.SpeedLevel}\n";
        status += $"氧氣等級: {PlayerDataManager.OxygenLevel}\n";
        status += $"體力等級: {PlayerDataManager.StaminaLevel}\n";
        status += $"當前場景: {SceneManager.GetActiveScene().name}\n";
        status += $"\n{CheatSceneTeleportTable.BuildCheatHelpText()}";

        statusText.text = status;
        
        // 更新按鈕文字
        if (toggleInvincibleButton != null)
        {
            var text = toggleInvincibleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = $"無敵模式: {(CheatManager.Instance.IsInvincible ? "關閉" : "開啟")}";
        }
        
        if (toggleInfiniteOxygenButton != null)
        {
            var text = toggleInfiniteOxygenButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = $"無限氧氣: {(CheatManager.Instance.IsInfiniteOxygen ? "關閉" : "開啟")}";
        }
        
        if (toggleInfiniteStaminaButton != null)
        {
            var text = toggleInfiniteStaminaButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = $"無限體力: {(CheatManager.Instance.IsInfiniteStamina ? "關閉" : "開啟")}";
        }
    }
    
    public void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            
            if (messageCoroutine != null)
                StopCoroutine(messageCoroutine);
            
            messageCoroutine = StartCoroutine(HideMessageAfterDelay());
        }
        
        Debug.Log($"[CheatPanel] {message}");
    }
    
    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSecondsRealtime(messageDisplayTime);
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

#else
    // 輸出版本僅保留空殼，讓既有場景引用有效；不編譯任何測試輸入或作弊邏輯。
    public GameObject panel;
    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        enabled = false;
    }
#endif
}

