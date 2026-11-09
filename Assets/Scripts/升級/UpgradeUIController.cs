using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 必須掛在「場景內」的升級 UI（例如 UpgradeplayerPanel），不要與 DontDestroyOnLoad 的 UpgradeManager 同物件，
/// 否則換場後序列化引用與 Button OnClick 會變成 Missing。
/// </summary>
public class UpgradeUIController : MonoBehaviour
{
    [Header("UI 元件")]
    public TextMeshProUGUI descriptionText;
    public GameObject UpgradePanel;

    [Header("升級按鈕文字")]
    public TextMeshProUGUI speedButtonText;
    public TextMeshProUGUI oxygenButtonText;
    public TextMeshProUGUI staminaButtonText;

    [Header("金幣需求說明文字")]
    public TextMeshProUGUI speedCostText;
    public TextMeshProUGUI oxygenCostText;
    public TextMeshProUGUI staminaCostText;

    [Header("目前金幣顯示／扣款演出（可選）")]
    [Tooltip("升級畫面上顯示持有金幣的 TMP（可拖 CoinText）；會與 CoinSpendFeedbackView 同步遞減")]
    [SerializeField] private TextMeshProUGUI coinBalanceText;
    [SerializeField] private CoinSpendFeedbackView coinSpendFeedback;
    [Tooltip("餘額大字；{0} 為數字。例：{0} 或 持有金幣 {0}")]
    [SerializeField] private string coinBalanceFormat = "{0}";
    [Tooltip("一般升級扣款後，-X 提示停留秒數")]
    [SerializeField] private float upgradeSpendPopupHold = 1f;
    [Tooltip("該項目升到滿級當下，-X 提示多停留一會（避免最後一次閃一下就沒）")]
    [SerializeField] private float upgradeMaxLevelSpendPopupHold = 2f;

    [Header("金幣列層級（固定在 Hierarchy 最下方＝顯示在內容之上）")]
    [Tooltip("須為 UpgradePanel 的直接子物件；只改繪製順序，不會改你在 Inspector 設的位置")]
    [SerializeField] private string[] coinBarLayerObjectNames = { "CoinIcon", "CoinText", "CoinChangeText", "coinfeeback" };

    [Header("圖片提示")]
    public Image successImage;
    public Image failImage;
    public Image maxImage;

    [Header("分頁面板")]
    public GameObject characterUpgradePanel;

    [Tooltip("通常為 UpgradePanel 下的 WeaponUpgradePanel；未指定時會自動尋找")]
    public GameObject weaponUpgradePanel;

    [Header("分頁高亮顯示")]
    public GameObject characterTabHighlight;

    [Tooltip("可選：武器分頁選中時顯示的底圖／外框")]
    public GameObject weaponTabHighlight;

    [Header("分頁按鈕（永遠顯示）")]
    [Tooltip("留空時會自動啟用 UpgradePanel 下第一層名為 player／weapon 的子物件。若分頁放在較深層，請手動拖入，避免被包在會關閉的 WeaponUpgradePanel 內。")]
    [SerializeField] private GameObject persistentTabCharacterButton;
    [SerializeField] private GameObject persistentTabWeaponButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterSceneLoaded()
    {
        EnsureOnSceneUpgradePanels();
        WirePlayerShortcutForActiveScene();
    }

    /// <summary>
    /// 在名為 UpgradePanel 的根下若尚無 UpgradeUIController，則自動掛在 UpgradeplayerPanel（或面板根）上。
    /// </summary>
    public static void EnsureOnSceneUpgradePanels()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != "UpgradePanel") continue;
            var go = t.gameObject;
            if (!go.scene.IsValid() || !go.scene.isLoaded) continue;
            if (go.GetComponentInChildren<UpgradeUIController>(true) != null) continue;

            Transform host = t.Find("UpgradeplayerPanel") ?? t;
            host.gameObject.AddComponent<UpgradeUIController>();
        }
    }

    /// <summary>
    /// 快捷鍵「player」按鈕（各場景 HUD）改為綁定目前場景的升級 UI，避免指到已銷毀的 DDOL 元件。
    /// </summary>
    public static void WirePlayerShortcutForActiveScene()
    {
        var host = FindPrimaryController();
        if (host == null) return;

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != "player" || t.gameObject.layer != 5) continue;
            if (!t.gameObject.scene.IsValid() || t.gameObject.scene != SceneManager.GetActiveScene()) continue;
            var btn = t.GetComponent<Button>();
            if (btn == null) continue;
            // 避免把「升級面板」裡名為 player 的分頁按鈕當成 HUD 入口
            if (IsUnderNamedAncestor(t, "UpgradePanel")) continue;

            ResetButtonClickEvents(btn);
            btn.onClick.AddListener(host.OpenUpgradePanel);
        }
    }

    static bool IsUnderNamedAncestor(Transform t, string ancestorName)
    {
        var p = t.parent;
        while (p != null)
        {
            if (p.name == ancestorName) return true;
            p = p.parent;
        }
        return false;
    }

    public static UpgradeUIController FindPrimaryController()
    {
        foreach (var c in Object.FindObjectsByType<UpgradeUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!c.gameObject.scene.IsValid()) continue;
            c.FindUIReferences();
            if (c.UpgradePanel != null) return c;
        }
        return null;
    }

    private static Transform FindDescendant(Transform root, string childName)
    {
        if (root == null) return null;
        if (root.name == childName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var hit = FindDescendant(root.GetChild(i), childName);
            if (hit != null) return hit;
        }
        return null;
    }

    /// <summary>名稱前綴相符即可（略過尾端空白），僅作後備；優先使用 <see cref="ResolveWeaponUpgradePanelObject"/>。</summary>
    static Transform FindDescendantPrefix(Transform root, string namePrefix)
    {
        if (root == null || string.IsNullOrEmpty(namePrefix)) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == null) continue;
            string n = t.name.Trim();
            if (n.StartsWith(namePrefix, System.StringComparison.Ordinal))
                return t;
        }
        return null;
    }

    GameObject ResolveWeaponUpgradePanelObject()
    {
        if (UpgradePanel == null) return null;
        var root = UpgradePanel.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            string n = c.name.Trim();
            if (n == "WeaponUpgradePanel" || n.StartsWith("WeaponUpgrade", System.StringComparison.Ordinal))
                return c.gameObject;
        }

        var exact = FindDescendant(root, "WeaponUpgradePanel");
        if (exact != null) return exact.gameObject;
        return FindDescendantPrefix(root, "WeaponUpgrade")?.gameObject;
    }

    /// <summary>勿把整個 UpgradePanel 當成角色／武器內容，否則 SetPanelGroupVisible 會關掉所有子物件含分頁。</summary>
    void SanitizeUpgradePanelContentRefs()
    {
        if (UpgradePanel == null) return;
        if (characterUpgradePanel == UpgradePanel)
            characterUpgradePanel = null;
        if (weaponUpgradePanel == UpgradePanel)
            weaponUpgradePanel = null;
        if (characterUpgradePanel != null &&
            !characterUpgradePanel.transform.IsChildOf(UpgradePanel.transform))
            characterUpgradePanel = null;
        if (weaponUpgradePanel != null &&
            !weaponUpgradePanel.transform.IsChildOf(UpgradePanel.transform))
            weaponUpgradePanel = null;

        // 常見誤設：把「分頁按鈕」player／weapon 拖進面板欄位；SetPanelGroupVisible 會略過這兩個名稱導致永遠切不過去。
        if (IsReservedTabButtonName(characterUpgradePanel))
            characterUpgradePanel = null;
        if (IsReservedTabButtonName(weaponUpgradePanel))
            weaponUpgradePanel = null;

        if (IsReservedTabButtonName(characterTabHighlight))
            characterTabHighlight = null;
        if (IsReservedTabButtonName(weaponTabHighlight))
            weaponTabHighlight = null;
    }

    static bool IsReservedTabButtonName(GameObject go)
    {
        if (go == null) return false;
        string n = go.name.Trim();
        return n == "player" || n == "weapon";
    }

    private static void ResetButtonClickEvents(Button button)
    {
        if (button == null) return;
        var removePersistent = typeof(UnityEngine.Events.UnityEventBase).GetMethod(
            "RemoveAllPersistentListeners",
            BindingFlags.Instance | BindingFlags.NonPublic);
        removePersistent?.Invoke(button.onClick, null);
        button.onClick.RemoveAllListeners();
    }

    private void Start()
    {
        FindUIReferences();
    }

    private void OnEnable()
    {
        FindUIReferences();
        if (coinSpendFeedback == null)
            PlayerDataManager.CoinsChanged += OnUpgradeCoinsChanged;
        if (coinSpendFeedback != null)
        {
            coinSpendFeedback.EnsureHierarchyActiveForUi();
            SetupUpgradeCoinDisplay();
        }
        RewriteSerializedControlBindings();
        RefreshUI();
        OpenCharacterTab();
    }

    private void OnDisable()
    {
        coinSpendFeedback?.CancelPresentation();
        coinSpendFeedback?.SetExternalBalanceMirror(null);
        PlayerDataManager.CoinsChanged -= OnUpgradeCoinsChanged;
    }

    /// <summary>升級面板共用金幣列（角色／武器分頁皆用）。</summary>
    public CoinSpendFeedbackView GetCoinSpendFeedback() => coinSpendFeedback;

    public void EnsureSharedCoinBarVisible()
    {
        FindUIReferences();
        SetupUpgradeCoinDisplay();
    }

    public void PlayCoinSpendFeedback(int coinsBeforeSpend, int spentGold, bool reachedMaxLevel)
    {
        if (coinSpendFeedback == null || spentGold <= 0) return;
        float hold = reachedMaxLevel ? upgradeMaxLevelSpendPopupHold : upgradeSpendPopupHold;
        coinSpendFeedback.RunSpendPresentation(coinsBeforeSpend, spentGold, this, hold);
    }

    void SetupUpgradeCoinDisplay()
    {
        if (coinSpendFeedback == null) return;

        coinSpendFeedback.EnsureHierarchyActiveForUi();

        if (coinBalanceText == null && UpgradePanel != null)
            coinBalanceText = FindTmpUnder(UpgradePanel.transform, "CoinText")
                              ?? FindTmpUnder(UpgradePanel.transform, "CoinBalanceText");
        if (coinBalanceText == null)
            coinBalanceText = FindTmpUnder(coinSpendFeedback.transform, "CoinText")
                              ?? FindTmpUnder(coinSpendFeedback.transform, "CoinBalanceText");

        if (UpgradePanel != null)
            coinSpendFeedback.ResolveReferencesFromUpgradePanel(UpgradePanel.transform);

        var changePopup = FindTmpUnder(UpgradePanel != null ? UpgradePanel.transform : transform, "CoinChangeText");
        if (changePopup != null)
            coinSpendFeedback.BindChangePopupText(changePopup);

        coinSpendFeedback.SetBalanceDisplayFormat(coinBalanceFormat);
        if (coinBalanceText != null)
        {
            coinSpendFeedback.BindBalanceText(coinBalanceText);
            coinSpendFeedback.SetExternalBalanceMirror(coinBalanceText);
        }

        coinSpendFeedback.RefreshBalanceText();
        PinUpgradeCoinBarLayer();
    }

    /// <summary>將金幣列固定在 UpgradePanel 子階層倒數（Hierarchy 列表最下面），分頁鈕仍保持最上。</summary>
    public void PinUpgradeCoinBarLayer()
    {
        if (UpgradePanel == null || coinBarLayerObjectNames == null) return;

        Transform root = UpgradePanel.transform;
        for (int i = 0; i < coinBarLayerObjectNames.Length; i++)
        {
            string objectName = coinBarLayerObjectNames[i];
            if (string.IsNullOrWhiteSpace(objectName)) continue;

            var tr = FindDescendant(root, objectName.Trim());
            if (tr == null || tr.parent != root) continue;

            tr.SetAsLastSibling();
        }

        BringTabButtonsToFront();
    }

    void OnUpgradeCoinsChanged(int c)
    {
        if (coinSpendFeedback != null || coinBalanceText == null) return;
        coinBalanceText.text = string.Format(coinBalanceFormat, c);
    }

    private void Update()
    {
        if (UpgradePanel != null &&
            UpgradePanel.activeSelf &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    /// <summary>
    /// 公開給靜態搜尋時解析引用；以場景內階層為主，不依賴掛在 DontDestroyOnLoad 上的序列化欄位。
    /// </summary>
    public void FindUIReferences()
    {
        ResolveOuterUpgradePanel();
        SanitizeUpgradePanelContentRefs();

        Transform scope = characterUpgradePanel != null
            ? characterUpgradePanel.transform
            : (UpgradePanel != null ? UpgradePanel.transform : transform);

        if (descriptionText == null)
            descriptionText = FindTmpUnder(scope, "DescriptionText");
        if (descriptionText == null && UpgradePanel != null)
            descriptionText = FindTmpUnder(UpgradePanel.transform, "DescriptionText");

        if (successImage == null)
            successImage = FindImageUnder(scope, "SuccessImage");
        if (failImage == null)
            failImage = FindImageUnder(scope, "FailImage");
        if (maxImage == null)
            maxImage = FindImageUnder(scope, "MaxImage");

        if (characterUpgradePanel == null && UpgradePanel != null)
            characterUpgradePanel = FindDescendant(UpgradePanel.transform, "UpgradeplayerPanel")?.gameObject;
        if (characterUpgradePanel == null)
            characterUpgradePanel = FindDescendant(transform, "UpgradeplayerPanel")?.gameObject;

        if (weaponUpgradePanel == null && UpgradePanel != null)
            weaponUpgradePanel = ResolveWeaponUpgradePanelObject();

        if (characterTabHighlight == null && UpgradePanel != null)
            characterTabHighlight = FindDescendant(UpgradePanel.transform, "CharacterTabHighlight")?.gameObject;
        if (characterTabHighlight == null && scope != null)
            characterTabHighlight = FindDescendant(scope, "CharacterTabHighlight")?.gameObject;

        if (weaponTabHighlight == null && UpgradePanel != null)
            weaponTabHighlight = FindDescendant(UpgradePanel.transform, "WeaponTabHighlight")?.gameObject;

        if (coinSpendFeedback == null && UpgradePanel != null)
            coinSpendFeedback = UpgradePanel.GetComponentInChildren<CoinSpendFeedbackView>(true);
        if (coinSpendFeedback == null)
            coinSpendFeedback = GetComponentInChildren<CoinSpendFeedbackView>(true);
        if (coinBalanceText == null && UpgradePanel != null)
            coinBalanceText = FindTmpUnder(UpgradePanel.transform, "CoinText")
                              ?? FindTmpUnder(UpgradePanel.transform, "CoinBalanceText")
                              ?? FindTmpUnder(scope, "CoinText")
                              ?? FindTmpUnder(scope, "CoinBalanceText");

        ResolveUpgradeButtonTexts(scope);
    }

    private void ResolveOuterUpgradePanel()
    {
        Transform p = transform;
        while (p != null)
        {
            if (p.name == "UpgradePanel")
            {
                UpgradePanel = p.gameObject;
                return;
            }
            p = p.parent;
        }

        var found = GameObject.Find("UpgradePanel");
        if (found != null)
            UpgradePanel = found;
    }

    private static TextMeshProUGUI FindTmpUnder(Transform root, string objectName)
    {
        var tr = FindDescendant(root, objectName);
        return tr != null ? tr.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Image FindImageUnder(Transform root, string objectName)
    {
        var tr = FindDescendant(root, objectName);
        return tr != null ? tr.GetComponent<Image>() : null;
    }

    private void ResolveUpgradeButtonTexts(Transform scope)
    {
        if (scope == null) return;

        void BindRow(string rowName, ref TextMeshProUGUI line, ref TextMeshProUGUI cost)
        {
            var row = FindDescendant(scope, rowName);
            if (row == null) return;
            var tmps = row.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps.Length >= 2)
            {
                if (line == null) line = tmps[0];
                if (cost == null) cost = tmps[1];
            }
            else if (tmps.Length == 1)
            {
                if (line == null) line = tmps[0];
            }

            var costTr = FindDescendant(row, "CostText");
            if (costTr != null && cost == null)
                cost = costTr.GetComponent<TextMeshProUGUI>();
        }

        BindRow("Speed", ref speedButtonText, ref speedCostText);
        BindRow("SpeedButton", ref speedButtonText, ref speedCostText);
        BindRow("Oxygen", ref oxygenButtonText, ref oxygenCostText);
        BindRow("OxygenButton", ref oxygenButtonText, ref oxygenCostText);
        BindRow("Hp", ref staminaButtonText, ref staminaCostText);
        BindRow("Stamina", ref staminaButtonText, ref staminaCostText);
        BindRow("StaminaButton", ref staminaButtonText, ref staminaCostText);
    }

    /// <summary>
    /// 清除 Inspector 裡已失效的 OnClick / EventTrigger，改綁到本元件（跨場景後仍有效）。
    /// </summary>
    private void RewriteSerializedControlBindings()
    {
        FindUIReferences();
        Transform scope = characterUpgradePanel != null
            ? characterUpgradePanel.transform
            : (UpgradePanel != null ? UpgradePanel.transform : transform);
        if (scope == null) return;

        void WireRow(string rowName, UnityEngine.Events.UnityAction onClick, string hover)
        {
            var row = FindDescendant(scope, rowName);
            if (row == null) return;
            var btn = row.GetComponent<Button>();
            if (btn != null)
            {
                ResetButtonClickEvents(btn);
                btn.onClick.AddListener(onClick);
            }

            var trigger = row.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                trigger.triggers.Clear();
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                string h = hover;
                enter.callback.AddListener(_ => ShowDescription(h));
                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => ClearDescription());
                trigger.triggers.Add(enter);
                trigger.triggers.Add(exit);
            }
        }

        WireRow("Speed", UpgradeSpeed, "速度：增加角色移動速度");
        WireRow("SpeedButton", UpgradeSpeed, "速度：增加角色移動速度");
        WireRow("Oxygen", UpgradeOxygen, "肺活量：減少氧氣消耗");
        WireRow("OxygenButton", UpgradeOxygen, "肺活量：減少氧氣消耗");
        WireRow("Hp", UpgradeStamina, "體魄：增加體力");
        WireRow("Stamina", UpgradeStamina, "體魄：增加體力");
        WireRow("StaminaButton", UpgradeStamina, "體魄：增加體力");

        var closeTr = FindDescendant(scope, "Close");
        if (closeTr != null)
        {
            var closeBtn = closeTr.GetComponent<Button>();
            if (closeBtn != null)
            {
                ResetButtonClickEvents(closeBtn);
                closeBtn.onClick.AddListener(ClosePanel);
            }
        }

        WireUpgradePanelTabButtons();
    }

    /// <summary>
    /// 在 UpgradePanel 第一層子物件尋找名為 player／weapon 的 Button，綁定角色／武器分頁。
    /// </summary>
    /// <summary>
    /// 若控制器掛在「角色升級面板」本體上，不可用 SetActive(false) 關閉整個面板（會停用本腳本）。
    /// 此時改為只切換子物件；否則整塊顯示／隱藏。
    /// </summary>
    void SetPanelGroupVisible(GameObject panelRoot, bool visible)
    {
        if (panelRoot == null) return;
        string pn = panelRoot.name.Trim();
        if (pn == "player" || pn == "weapon")
            return;
        if (panelRoot == gameObject)
        {
            var rootGraphic = GetComponent<Image>();
            if (rootGraphic != null)
                rootGraphic.enabled = visible;
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(visible);
        }
        else
            panelRoot.SetActive(visible);
    }

    /// <summary>分頁按鈕不因切換內容而被關閉；請將 player／weapon 放在 UpgradePanel 第一層，或填寫 persistent 欄位。</summary>
    void EnsureTabButtonsStayActive()
    {
        if (UpgradePanel == null) return;
        var root = UpgradePanel.transform;

        void Activate(GameObject go)
        {
            if (go != null && !go.activeSelf)
                go.SetActive(true);
        }

        Activate(persistentTabCharacterButton);
        Activate(persistentTabWeaponButton);

        for (int i = 0; i < root.childCount; i++)
        {
            var go = root.GetChild(i).gameObject;
            string n = go.name.Trim();
            if (n == "player" || n == "weapon")
                Activate(go);
        }
    }

    /// <summary>讓目前顯示的內容面板畫在兄弟節點之上，再把分頁按鈕移到最上層以免被遮住或點不到。</summary>
    void BringContentPanelToFront(GameObject contentPanel)
    {
        if (contentPanel != null && contentPanel.transform.parent == UpgradePanel.transform)
            contentPanel.transform.SetAsLastSibling();
        PinUpgradeCoinBarLayer();
    }

    void BringTabButtonsToFront()
    {
        if (UpgradePanel == null) return;
        var root = UpgradePanel.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            var ch = root.GetChild(i);
            string n = ch.name.Trim();
            if (n == "player" || n == "weapon")
                ch.SetAsLastSibling();
        }

        if (persistentTabCharacterButton != null &&
            persistentTabCharacterButton.transform.parent == root)
            persistentTabCharacterButton.transform.SetAsLastSibling();
        if (persistentTabWeaponButton != null &&
            persistentTabWeaponButton.transform.parent == root)
            persistentTabWeaponButton.transform.SetAsLastSibling();
    }

    void WireUpgradePanelTabButtons()
    {
        if (UpgradePanel == null) return;
        var root = UpgradePanel.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            var ch = root.GetChild(i);
            var btn = ch.GetComponent<Button>();
            if (btn == null) continue;
            string n = ch.name.Trim();
            if (n == "player")
            {
                ResetButtonClickEvents(btn);
                btn.onClick.AddListener(OpenCharacterTab);
            }
            else if (n == "weapon")
            {
                ResetButtonClickEvents(btn);
                btn.onClick.AddListener(OpenWeaponTab);
            }
        }

        if (weaponUpgradePanel != null)
        {
            var closeWeapon = FindDescendant(weaponUpgradePanel.transform, "Close");
            if (closeWeapon != null)
            {
                var cb = closeWeapon.GetComponent<Button>();
                if (cb != null)
                {
                    ResetButtonClickEvents(cb);
                    cb.onClick.AddListener(ClosePanel);
                }
            }
        }
    }

    /// <param name="refreshCoinDisplay">升級成功且即將播放扣款演出時可設 false，避免覆寫遞減動畫。</param>
    public void RefreshUI(bool refreshCoinDisplay = true)
    {
        if (refreshCoinDisplay)
        {
            if (coinSpendFeedback != null)
                coinSpendFeedback.RefreshBalanceText();
            else if (coinBalanceText != null)
                coinBalanceText.text = PlayerDataManager.Coins.ToString();
        }

        int speedLv = PlayerDataManager.SpeedLevel;
        int oxygenLv = PlayerDataManager.OxygenLevel;
        int staminaLv = PlayerDataManager.StaminaLevel;

        if (speedButtonText != null)
            speedButtonText.text = $"速度 (Lv.{speedLv} → {Mathf.Min(speedLv + 1, PlayerDataManager.MaxLevel)})";
        if (oxygenButtonText != null)
            oxygenButtonText.text = $"肺活量 (Lv.{oxygenLv} → {Mathf.Min(oxygenLv + 1, PlayerDataManager.MaxLevel)})";
        if (staminaButtonText != null)
            staminaButtonText.text = $"體力 (Lv.{staminaLv} → {Mathf.Min(staminaLv + 1, PlayerDataManager.MaxLevel)})";

        if (speedCostText != null)
            speedCostText.text = speedLv >= PlayerDataManager.MaxLevel ? "已達最大等級" : $"需 {PlayerDataManager.UpgradeCosts[speedLv]} 金幣";
        if (oxygenCostText != null)
            oxygenCostText.text = oxygenLv >= PlayerDataManager.MaxLevel ? "已達最大等級" : $"需 {PlayerDataManager.UpgradeCosts[oxygenLv]} 金幣";
        if (staminaCostText != null)
            staminaCostText.text = staminaLv >= PlayerDataManager.MaxLevel ? "已達最大等級" : $"需 {PlayerDataManager.UpgradeCosts[staminaLv]} 金幣";
    }

    public void UpgradeSpeed() => TryUpgrade(UpgradeManager.UpgradeType.Speed);
    public void UpgradeOxygen() => TryUpgrade(UpgradeManager.UpgradeType.OxygenEfficiency);
    public void UpgradeStamina() => TryUpgrade(UpgradeManager.UpgradeType.Stamina);

    private Coroutine _feedbackHideRoutine;

    private void TryUpgrade(UpgradeManager.UpgradeType type)
    {
        if (UpgradeManager.Instance == null) return;

        int coinsBefore = PlayerDataManager.Coins;
        int prevLevel = UpgradeManager.Instance.GetLevel(type);
        bool useSpendFeedback = coinSpendFeedback != null;
        if (useSpendFeedback)
            coinSpendFeedback.SetAnimLock(true);

        var result = UpgradeManager.Instance.TryUpgrade(type);

        switch (result)
        {
            case UpgradeManager.UpgradeResult.Success:
                ShowFeedback(successImage, "升級成功");
                break;
            case UpgradeManager.UpgradeResult.MaxLevel:
                ShowFeedback(maxImage, "已達最大等級");
                break;
            case UpgradeManager.UpgradeResult.NotEnoughCoins:
                ShowFeedback(failImage, "金幣不足");
                break;
        }

        int spentGold = prevLevel >= 0 && prevLevel < PlayerDataManager.UpgradeCosts.Length
            ? PlayerDataManager.UpgradeCosts[prevLevel]
            : 0;

        if (useSpendFeedback)
        {
            if (result == UpgradeManager.UpgradeResult.Success && spentGold > 0)
            {
                bool reachedMaxThisUpgrade = UpgradeManager.Instance.GetLevel(type) >= PlayerDataManager.MaxLevel;
                PlayCoinSpendFeedback(coinsBefore, spentGold, reachedMaxThisUpgrade);
            }
            else
                coinSpendFeedback.CancelPresentation();
        }

        RefreshUI(!(useSpendFeedback && result == UpgradeManager.UpgradeResult.Success));
        if (result == UpgradeManager.UpgradeResult.Success)
            PinUpgradeCoinBarLayer();

        if (result == UpgradeManager.UpgradeResult.Success && AchievementManager.Instance != null)
        {
            if (type == UpgradeManager.UpgradeType.Speed)
                AchievementManager.Instance.SetProgress("Level_Speed", PlayerDataManager.SpeedLevel);
            if (type == UpgradeManager.UpgradeType.OxygenEfficiency)
                AchievementManager.Instance.SetProgress("Level_Oxygen", PlayerDataManager.OxygenLevel);
            if (type == UpgradeManager.UpgradeType.Stamina)
                AchievementManager.Instance.SetProgress("Level_Stamina", PlayerDataManager.StaminaLevel);
        }
    }

    private void ShowFeedback(Image icon, string text)
    {
        if (successImage != null) successImage.gameObject.SetActive(false);
        if (failImage != null) failImage.gameObject.SetActive(false);
        if (maxImage != null) maxImage.gameObject.SetActive(false);

        if (descriptionText != null)
            descriptionText.text = text;
        if (icon != null)
        {
            icon.gameObject.SetActive(true);
            if (_feedbackHideRoutine != null)
                StopCoroutine(_feedbackHideRoutine);
            _feedbackHideRoutine = StartCoroutine(HideImageAfterDelay(icon, 2f));
        }
    }

    public void ShowDescription(string desc)
    {
        if (descriptionText != null)
            descriptionText.text = desc;
    }

    public void ClearDescription()
    {
        if (descriptionText != null)
            descriptionText.text = "";
    }

    private IEnumerator HideImageAfterDelay(Image icon, float delay)
    {
        yield return new WaitForSeconds(delay);
        icon.gameObject.SetActive(false);
    }

    public void ClosePanel()
    {
        if (UpgradePanel != null)
            UpgradePanel.SetActive(false);

        CloseAllUIPanels();
    }

    public void OpenUpgradePanel()
    {
        FindUIReferences();
        if (UpgradePanel != null)
        {
            UpgradePanel.SetActive(true);
            var upgradeRt = UpgradePanel.transform as RectTransform;
            if (upgradeRt != null)
                upgradeRt.SetAsLastSibling();

            if (UpgradePanel.GetComponent<PauseOnEnable>() == null)
                UpgradePanel.AddComponent<PauseOnEnable>();
            RewriteSerializedControlBindings();
            RefreshUI();
            OpenCharacterTab();
        }
    }

    private void CloseAllUIPanels()
    {
        GameObject dialoguePanel = GameObject.Find("DialoguePanel");
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        GameObject shopPanel = GameObject.Find("ShopPanel");
        if (shopPanel != null)
            shopPanel.SetActive(false);

        GameObject questPanel = GameObject.Find("QuestPanel");
        if (questPanel != null)
            questPanel.SetActive(false);

        GameObject inventoryPanel = GameObject.Find("InventoryPanel");
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        GameObject pauseMenu = GameObject.Find("PauseMenuUI");
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        GameObject npcUpgradePanel = GameObject.Find("NPCUpgradePanel");
        if (npcUpgradePanel != null)
            npcUpgradePanel.SetActive(false);

        GameObject confirmPanel = GameObject.Find("ConfirmPanel");
        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        GameObject saveSlotPanel = GameObject.Find("SaveSlotPanel");
        if (saveSlotPanel != null)
            saveSlotPanel.SetActive(false);

        GameObject promptUI = GameObject.Find("PromptUI");
        if (promptUI != null)
            promptUI.SetActive(false);

        Debug.Log("升級面板關閉按鈕：已關閉所有UI面板");
    }

    public void OpenCharacterTab()
    {
        FindUIReferences();
        SetPanelGroupVisible(weaponUpgradePanel, false);

        if (characterUpgradePanel != null)
            SetPanelGroupVisible(characterUpgradePanel, true);
        else if (UpgradePanel != null)
        {
            var inner = FindDescendant(UpgradePanel.transform, "UpgradeplayerPanel");
            if (inner != null) SetPanelGroupVisible(inner.gameObject, true);
        }

        if (characterTabHighlight != null) characterTabHighlight.SetActive(true);
        if (weaponTabHighlight != null) weaponTabHighlight.SetActive(false);

        EnsureTabButtonsStayActive();
        BringContentPanelToFront(characterUpgradePanel != null
            ? characterUpgradePanel
            : FindDescendant(UpgradePanel != null ? UpgradePanel.transform : null, "UpgradeplayerPanel")?.gameObject);
        RefreshUI();
    }

    public void OpenWeaponTab()
    {
        FindUIReferences();
        if (weaponUpgradePanel == null && UpgradePanel != null)
            weaponUpgradePanel = ResolveWeaponUpgradePanelObject();

        SetPanelGroupVisible(characterUpgradePanel, false);
        if (characterUpgradePanel == null && UpgradePanel != null)
        {
            var inner = FindDescendant(UpgradePanel.transform, "UpgradeplayerPanel");
            if (inner != null) SetPanelGroupVisible(inner.gameObject, false);
        }

        if (weaponUpgradePanel != null)
        {
            SetPanelGroupVisible(weaponUpgradePanel, true);
            var w = weaponUpgradePanel.GetComponentInChildren<WeaponUpgradeUIController>(true);
            if (w != null) w.RefreshDisplay();
            BringContentPanelToFront(weaponUpgradePanel);
        }
        else
            Debug.LogWarning("[UpgradeUIController] 找不到 WeaponUpgradePanel（請確認 UpgradePanel 底下有該物件，或名稱以 WeaponUpgrade 開頭）。");

        if (characterTabHighlight != null) characterTabHighlight.SetActive(false);
        if (weaponTabHighlight != null) weaponTabHighlight.SetActive(true);

        EnsureSharedCoinBarVisible();
        PinUpgradeCoinBarLayer();

        EnsureTabButtonsStayActive();
        BringTabButtonsToFront();
    }
}
