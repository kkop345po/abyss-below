using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting;
using TMPro;

/// <summary>
/// 賭博演出與流程：Coroutine 嚴格切分階段；點數與輸贏由 <see cref="DiceManager"/> 預先決定。
/// </summary>
[Preserve]
public class GambleUIController : MonoBehaviour, IGambleNpcTrigger
{
    [Header("根節點")]
    [SerializeField] private GameObject rootPanel;

    [Header("骰子演出（邏輯解耦）")]
    [SerializeField] private DiceRollViewBase diceRollView;

    [Header("流程時間")]
    [Tooltip("動畫結束後，畫面維持在最終點數的最少秒數")]
    [SerializeField] private float stayOnFinalDiceSeconds = 0.8f;

    [Header("Judge 階段 UI")]
    [SerializeField] private GameObject judgePanel;
    [SerializeField] private TextMeshProUGUI judgeDetailText;
    [SerializeField] private Button judgeConfirmButton;

    [Header("End 階段 UI")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button closeEndButton;

    [Header("二段式押注（先金額 → 再大／小／豹子 → 開始）")]
    [Tooltip("選填：有指定時，開啟介面只顯示金額區；選完金額後自動換成「大／小／開始」區")]
    [SerializeField] private GameObject betAmountStepRoot;
    [SerializeField] private GameObject betSideAndStartStepRoot;
    [Tooltip("選填：顯示目前選了幾元、押大／小")]
    [SerializeField] private TextMeshProUGUI selectionSummaryText;

    [Header("本局押注／金幣演出（與賭桌背景同一 Canvas 較佳）")]
    [Tooltip("選金額／選大小时即顯示：目前金幣與本局押注說明；真正扣款時才播 -X 彈窗（勿放在會被 HideBettingStepRoots 關掉的 Panel 內）")]
    [SerializeField] private GameObject roundInfoDuringRollRoot;
    [SerializeField] private TextMeshProUGUI roundBetDisplayText;
    [Tooltip("賭桌畫面上顯示「目前金幣」；會與扣款動畫連動（可與主 UI 金幣欄位分開或複製一份）")]
    [SerializeField] private TextMeshProUGUI gambleCoinMirrorText;
    [Tooltip("扣款時跳出的「-50」等提示父物件")]
    [SerializeField] private GameObject coinDeductPopupRoot;
    [SerializeField] private TextMeshProUGUI coinDeductPopupText;
    [Tooltip("金幣數字由扣前遞減到扣後的秒數")]
    [SerializeField] private float coinCountAnimDuration = 0.55f;
    [Tooltip("扣款提示停留多久後關閉")]
    [SerializeField] private float coinPopupHoldSeconds = 0.35f;

    bool _hasPickedAmount;
    bool _hasPickedSide;
    int _pendingBetAmount;
    GambleBetSide _pendingBetSide;

    Coroutine _flow;
    bool _judgeAcknowledged;
    bool _waitingEndChoice;
    bool _chosePlayAgain;
    bool _coinDeductionAnimActive;

    public bool IsFlowRunning => _flow != null;

    void Awake()
    {
        WireJudgeAndEndButtons();
    }

    void OnEnable()
    {
        PlayerDataManager.CoinsChanged += OnPlayerCoinsChanged;
        SyncGambleCoinMirrorFromData();
        WireStartGambleButtonRuntime();
    }

    void WireJudgeAndEndButtons()
    {
        if (judgeConfirmButton != null)
            judgeConfirmButton.onClick.AddListener(OnJudgeConfirmClicked);
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        if (closeEndButton != null)
            closeEndButton.onClick.AddListener(OnCloseEndClicked);
    }

    /// <summary>
    /// 輸出檔常見：只有「開始遊戲」的 Inspector UnityEvent 失效；改由 <see cref="GambleStartButton"/> 程式綁定。
    /// </summary>
    void WireStartGambleButtonRuntime()
    {
        if (betSideAndStartStepRoot == null)
            return;

        var buttons = betSideAndStartStepRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn == null) continue;
            string n = btn.name ?? "";
            if (!n.Contains("開始") && !n.Contains("Start", System.StringComparison.OrdinalIgnoreCase))
                continue;

            btn.transform.SetAsLastSibling();
            GambleStartButton.ApplyRaycastFix(btn);

            var bridge = btn.GetComponent<GambleStartButton>();
            if (bridge == null)
                bridge = btn.gameObject.AddComponent<GambleStartButton>();
            bridge.Bind(this);
            return;
        }
    }

    void OnDisable()
    {
        PlayerDataManager.CoinsChanged -= OnPlayerCoinsChanged;
    }

    void OnPlayerCoinsChanged(int newCoins)
    {
        if (_coinDeductionAnimActive || gambleCoinMirrorText == null) return;
        gambleCoinMirrorText.text = newCoins.ToString();
    }

    /// <summary>NPC 觸發：顯示根面板；實際開賭需再呼叫 <see cref="BeginGamble"/> 或二段式 <see cref="StartGambleFromSelection"/>。</summary>
    public void OpenGambleFromNpc()
    {
        if (rootPanel != null)
            rootPanel.SetActive(true);
        ResetBettingStepsForNewSession();
        WireStartGambleButtonRuntime();
    }

    /// <summary>由 UI 選好金額與大小後開始一整輪流程（單鍵直開賭時用）。</summary>
    public void BeginGamble(int betAmount, GambleBetSide betSide)
    {
        ClearPendingBetSelection();
        if (rootPanel != null)
            rootPanel.SetActive(true);
        if (_flow != null)
            StopCoroutine(_flow);
        _flow = StartCoroutine(GambleFlowCoroutine(betAmount, betSide));
    }

    // —— 二段式：Button 依序綁「選金額」→「選大／小」→「開始遊戲」——

    /// <summary>第一步：選押注金額（可綁多顆 Button，或 SelectBetAmount(int) 並在欄位填數字）。</summary>
    public void SelectBetAmount(int amount)
    {
        _pendingBetAmount = amount;
        _hasPickedAmount = true;
        if (betAmountStepRoot != null)
            betAmountStepRoot.SetActive(false);
        if (betSideAndStartStepRoot != null)
            betSideAndStartStepRoot.SetActive(true);
        RefreshSelectionSummary();
        ShowRoundHudForSelectionPhase();
        WireStartGambleButtonRuntime();
    }

    public void SelectBetAmount10() => SelectBetAmount(10);
    public void SelectBetAmount30() => SelectBetAmount(30);
    public void SelectBetAmount50() => SelectBetAmount(50);
    public void SelectBetAmount100() => SelectBetAmount(100);
    public void SelectBetAmount150() => SelectBetAmount(150);

    /// <summary>第二步：押小（總和 3–10）。</summary>
    public void SelectBetSideSmall()
    {
        _pendingBetSide = GambleBetSide.Small;
        _hasPickedSide = true;
        RefreshSelectionSummary();
        ShowRoundHudForSelectionPhase();
    }

    /// <summary>第二步：押大（總和 11–18）。</summary>
    public void SelectBetSideBig()
    {
        _pendingBetSide = GambleBetSide.Big;
        _hasPickedSide = true;
        RefreshSelectionSummary();
        ShowRoundHudForSelectionPhase();
    }

    /// <summary>
    /// （相容保留）舊版：押「豹子（三個6）」。
    /// 目前豹子改為隱藏機制：三顆相同即為豹子，押大/押小都可能觸發；因此此選項已不再使用。
    /// </summary>
    public void SelectBetSideLeopardSix()
    {
        _hasPickedSide = false;
        RefreshSelectionSummary();
        ShowSelectionHintError("豹子已改為隱藏機制：押大/押小皆可能觸發（三顆同點）。請選擇大或小。");
    }

    /// <summary>給「開始遊戲」按鈕用（名稱較短，輸出檔 UnityEvent 較不易失效）；內部仍走 <see cref="StartGambleFromSelection"/>。</summary>
    [Preserve]
    public void OnClickStartGambleButton()
    {
        StartGambleFromSelection();
    }

    /// <summary>第三步：依目前選擇開始賭局（請先完成金額與大／小／豹子）。</summary>
    [Preserve]
    public void StartGambleFromSelection()
    {
        DiceManager.EnsureExists();
        CoinManager.EnsureExists();
        BuffManager.EnsureExists();
        InventoryManager existingInv = InventoryManager.Instance;
        if (existingInv == null)
        {
            var inv = Object.FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);
            if (inv != null && !inv.gameObject.activeInHierarchy)
                inv.gameObject.SetActive(true);
        }

        if (DiceManager.Instance == null || CoinManager.Instance == null)
        {
            const string msg = "無法開始：缺少 DiceManager 或 CoinManager。請確認場景有 CoinManager／InventorySystem 預製體。";
            Debug.LogWarning("[GambleUIController] " + msg);
            ShowSelectionHintError(msg);
            return;
        }

        if (!_hasPickedAmount)
        {
            Debug.LogWarning("[GambleUIController] 請先選擇押注金額");
            ShowSelectionHintError("請先選擇押注金額");
            return;
        }

        if (_pendingBetAmount < DiceManager.Instance.MinBet)
        {
            ShowSelectionHintError($"金額至少 {DiceManager.Instance.MinBet}");
            return;
        }

        if (!_hasPickedSide)
        {
            Debug.LogWarning("[GambleUIController] 請選擇押大、押小");
            ShowSelectionHintError("請選擇押大、押小");
            return;
        }

        BeginGamble(_pendingBetAmount, _pendingBetSide);
    }

    /// <summary>回到選金額步驟（可綁「上一步」按鈕）。</summary>
    public void GoBackToBetAmountStep()
    {
        _hasPickedAmount = false;
        _hasPickedSide = false;
        if (betAmountStepRoot != null)
            betAmountStepRoot.SetActive(true);
        if (betSideAndStartStepRoot != null)
            betSideAndStartStepRoot.SetActive(false);
        RefreshSelectionSummary();
        ShowRoundHudForSelectionPhase();
    }

    void ClearPendingBetSelection()
    {
        _hasPickedAmount = false;
        _hasPickedSide = false;
        _pendingBetAmount = 0;
        _pendingBetSide = GambleBetSide.Small;
    }

    void ResetBettingStepsForNewSession()
    {
        ClearPendingBetSelection();
        if (betAmountStepRoot != null)
            betAmountStepRoot.SetActive(true);
        if (betSideAndStartStepRoot != null)
            betSideAndStartStepRoot.SetActive(false);
        RefreshSelectionSummary();
        if (coinDeductPopupRoot != null)
            coinDeductPopupRoot.SetActive(false);
        ShowRoundHudForSelectionPhase();
    }

    void RefreshSelectionSummary()
    {
        if (selectionSummaryText == null) return;
        string amt = _hasPickedAmount ? $"{_pendingBetAmount} 金幣" : "（未選）";
        string side = !_hasPickedSide ? "（未選）" : BetSideDisplayName(_pendingBetSide);
        selectionSummaryText.text = $"押注金額：{amt}\n大／小：{side}\n選好後請按「開始遊戲」";
    }

    void ShowSelectionHintError(string message)
    {
        if (selectionSummaryText != null)
            selectionSummaryText.text = message;
        else if (roundBetDisplayText != null)
            roundBetDisplayText.text = message;
        else if (judgeDetailText != null)
        {
            SetPanel(judgePanel, true);
            judgeDetailText.text = message;
        }
    }

    void SetRoundRollHudVisible(bool on)
    {
        if (roundInfoDuringRollRoot != null)
            roundInfoDuringRollRoot.SetActive(on);
    }

    /// <summary>二段式押注階段：顯示 HUD、同步金幣文字、依目前選項更新押注說明（尚未扣款）。</summary>
    void ShowRoundHudForSelectionPhase()
    {
        SetRoundRollHudVisible(true);
        SyncGambleCoinMirrorFromData();
        RefreshRoundBetDisplayFromPending();
    }

    void RefreshRoundBetDisplayFromPending()
    {
        if (roundBetDisplayText == null) return;
        if (!_hasPickedAmount)
        {
            roundBetDisplayText.text = "本局押注：請先選擇金額";
            return;
        }

        if (!_hasPickedSide)
        {
            roundBetDisplayText.text = $"本局押注：{_pendingBetAmount} 金幣（請選擇大／小）";
            return;
        }

        string sideStr = BetSideDisplayName(_pendingBetSide);
        roundBetDisplayText.text = $"本局押注：{_pendingBetAmount} 金幣（{sideStr}）";
    }

    void UpdateRoundBetDisplay(int bet, GambleBetSide side)
    {
        if (roundBetDisplayText == null) return;
        string sideStr = BetSideDisplayName(side);
        roundBetDisplayText.text = $"本局押注：{bet} 金幣（{sideStr}）";
    }

    static string BetSideDisplayName(GambleBetSide side)
    {
        switch (side)
        {
            case GambleBetSide.Big: return "大";
            default: return "小";
        }
    }

    void SyncGambleCoinMirrorFromData()
    {
        if (gambleCoinMirrorText != null)
            gambleCoinMirrorText.text = PlayerDataManager.Coins.ToString();
    }

    /// <summary>扣款後：跳出 -X、金幣數字遞減，並與 CoinManager 主 UI 同步。</summary>
    IEnumerator PlayCoinDeductionPresentation(int goldBefore, int spent)
    {
        int goldAfter = PlayerDataManager.Coins;
        TMP_Text globalCoin = CoinManager.Instance != null ? CoinManager.Instance.coinText : null;

        if (coinDeductPopupRoot != null)
        {
            coinDeductPopupRoot.SetActive(true);
            if (coinDeductPopupText != null)
                coinDeductPopupText.text = $"-{spent}";
        }

        float dur = Mathf.Max(0.05f, coinCountAnimDuration);
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            int v = Mathf.RoundToInt(Mathf.Lerp(goldBefore, goldAfter, k));
            string s = v.ToString();
            if (gambleCoinMirrorText != null)
                gambleCoinMirrorText.text = s;
            if (globalCoin != null)
                globalCoin.text = s;
            yield return null;
        }

        if (gambleCoinMirrorText != null)
            gambleCoinMirrorText.text = goldAfter.ToString();
        CoinManager.Instance?.UpdateCoinUI();

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, coinPopupHoldSeconds));
        if (coinDeductPopupRoot != null)
            coinDeductPopupRoot.SetActive(false);
    }

    /// <summary>
    /// 給 Button OnClick 用：Unity 下拉選單通常不會列出含自訂 enum 的 <see cref="BeginGamble(int, GambleBetSide)"/>，
    /// 請改選本方法並在欄位填金額（押「小」總和 3–10）。
    /// </summary>
    public void BeginGambleSmall(int betAmount)
    {
        BeginGamble(betAmount, GambleBetSide.Small);
    }

    /// <summary>同上，押「大」（總和 11–18）。</summary>
    public void BeginGambleBig(int betAmount)
    {
        BeginGamble(betAmount, GambleBetSide.Big);
    }

    /// <summary>同上，押「豹子」（三顆皆為 6）。</summary>
    public void BeginGambleLeopardSix(int betAmount)
    {
        Debug.LogWarning("[GambleUIController] 豹子已改為隱藏機制（不再可選）。BeginGambleLeopardSix 會改為押「大」。");
        BeginGamble(betAmount, GambleBetSide.Big);
    }

    /// <summary>
    /// 無參數，給 Button OnClick 測試用（避免忘記填 int 導致金額為 0 而流程直接結束）。
    /// </summary>
    public void BeginGambleTest50Big()
    {
        BeginGamble(50, GambleBetSide.Big);
    }

    /// <summary>無參數測試：押小 30。</summary>
    public void BeginGambleTest30Small()
    {
        BeginGamble(30, GambleBetSide.Small);
    }

    public void CloseRoot()
    {
        SetRoundRollHudVisible(false);
        if (rootPanel != null)
            rootPanel.SetActive(false);
        SetPanel(judgePanel, false);
        SetPanel(endPanel, false);
    }

    IEnumerator GambleFlowCoroutine(int betAmount, GambleBetSide betSide)
    {
        DiceManager.EnsureExists();
        CoinManager.EnsureExists();

        // —— Start：扣除金幣，決定點數與輸贏（數值預算已完成於此步驟內）——
        if (CoinManager.Instance == null || DiceManager.Instance == null || InventoryManager.Instance == null)
        {
            yield return StartCoroutine(AbortFlowAndNotifyUser(
                "無法開賭：場景裡找不到 CoinManager／DiceManager／InventoryManager。\n請在「第一個載入的持久場景」建立空物件並掛這三個管理器（教學見 GambleSystemSetup.md 步驟 2～5）。"));
            yield break;
        }

        if (betAmount < DiceManager.Instance.MinBet)
        {
            yield return StartCoroutine(AbortFlowAndNotifyUser(
                $"押注金額無效：目前為 {betAmount}，至少需要 {DiceManager.Instance.MinBet}。\n請在 Button 的 OnClick 裡選 BeginGambleBig(int)／BeginGambleSmall(int) 並填金額，或改用 BeginGambleTest50Big（無參數測試）。"));
            yield break;
        }

        if (!CoinManager.Instance.HasEnoughCoins(betAmount))
        {
            yield return StartCoroutine(AbortFlowAndNotifyUser(
                $"金幣不足：需要 {betAmount}，目前約 {PlayerDataManager.Coins}。"));
            yield break;
        }

        int coinsBeforeSpend = PlayerDataManager.Coins;
        SetRoundRollHudVisible(true);
        UpdateRoundBetDisplay(betAmount, betSide);
        SyncGambleCoinMirrorFromData();

        _coinDeductionAnimActive = true;
        try
        {
            CoinManager.Instance.SpendCoins(betAmount);
            yield return StartCoroutine(PlayCoinDeductionPresentation(coinsBeforeSpend, betAmount));
        }
        finally
        {
            _coinDeductionAnimActive = false;
        }

        if (!DiceManager.Instance.TryResolveGamble(betAmount, betSide, out GambleOutcome outcome))
        {
            CoinManager.Instance.AddCoins(betAmount);
            SyncGambleCoinMirrorFromData();
            CoinManager.Instance.UpdateCoinUI();
            yield return StartCoroutine(AbortFlowAndNotifyUser("賭局結算失敗（內部錯誤），已退還金幣。"));
            yield break;
        }

        SetPanel(judgePanel, false);
        SetPanel(endPanel, false);
        // 進入骰子／結果階段時關閉押注 UI（含 BetSideStep），避免與 JudgePanel 疊顯
        HideBettingStepRoots();

        // —— Animation：呼叫 PlayDiceAnimation（與純邏輯解耦）——
        // 中文註解：此 yield 等待點 — 直到自訂骰子動畫／Animator 播完為止。
        if (diceRollView != null)
            yield return StartCoroutine(diceRollView.PlayDiceAnimation(outcome.Dice1, outcome.Dice2, outcome.Dice3));
        else
            Debug.LogWarning("[GambleUIController] 未指定 diceRollView，略過動畫。");

        // —— Wait & Stay：動畫結束後須停留在最終點數狀態一段時間，不可立刻關閉骰子畫面——
        // 中文註解：此 yield 等待點 — 強制停留，避免結果一出現就被 UI 切走。
        if (stayOnFinalDiceSeconds > 0f)
            yield return new WaitForSeconds(stayOnFinalDiceSeconds);

        // —— Judge：顯示輸贏 UI，贏則發放「花枝丸」道具（非直接給 Buff）——
        SetPanel(judgePanel, true);
        if (judgeDetailText != null)
            judgeDetailText.text = BuildJudgeMessage(outcome);

        // 允許 RewardItemId 填「道具名稱」或「itemId」：由 InventoryManager.ResolveItemData 兼容處理
        if (outcome.PlayerWon && !string.IsNullOrEmpty(outcome.RewardItemId) && outcome.RewardQuantity > 0)
            InventoryManager.Instance.AddItem(outcome.RewardItemId, outcome.RewardQuantity);

        _judgeAcknowledged = false;
        // 中文註解：等待點 — 玩家閱讀結果並按下確認，才進入 End。
        yield return new WaitUntil(() => _judgeAcknowledged);
        // 中文註解：結算確認後、關 Judge 前 — 例：骰鍾關蓋（見 GambleCupSequenceView.PlayPostJudgePresentation）。
        if (diceRollView != null)
            yield return StartCoroutine(diceRollView.PlayPostJudgePresentation());
        SetPanel(judgePanel, false);

        // —— End：確認後才結束本輪演出，並可選再玩一次或關閉——
        SetPanel(endPanel, true);
        _waitingEndChoice = true;
        _chosePlayAgain = false;
        // 中文註解：此 yield 等待點 — 玩家按下「再玩一次」或「關閉」後才往下執行並結束 Coroutine。
        yield return new WaitUntil(() => !_waitingEndChoice);

        SetPanel(endPanel, false);
        if (!_chosePlayAgain)
            CloseRoot();
        else
            ResetBettingStepsForNewSession();

        _flow = null;
    }

    string BuildJudgeMessage(GambleOutcome o)
    {
        string size = o.IsSumBig ? "大" : "小";
        string bet = BetSideDisplayName(o.PlayerBet);
        string result = o.PlayerWon ? "獲勝" : "落敗";
        string dice = $"{o.Dice1}+{o.Dice2}+{o.Dice3}={o.DiceSum}（{size}）";
        string leopard = o.IsLeopard ? $"\n（隱藏豹子：三同 {o.LeopardPip}）" : "";
        string reward = "";
        if (o.PlayerWon && o.RewardQuantity > 0 && !string.IsNullOrEmpty(o.RewardItemId))
        {
            // 顯示中文名：優先用 ItemDatabase/QuestItemDatabase 的 itemName，找不到才退回顯示 id/原字串
            string displayName = o.RewardItemId;
            if (InventoryManager.Instance != null)
            {
                var data = InventoryManager.Instance.ResolveItemData(o.RewardItemId);
                if (data != null && !string.IsNullOrEmpty(data.itemName))
                    displayName = data.itemName;
            }
            reward = $"\n獲得道具：{displayName} x{o.RewardQuantity}";
        }
        string mercy = o.UsedMercyWin ? "\n（觸發連敗保底）" : "";
        return $"押注：{bet}｜{dice}\n{result}{leopard}{reward}{mercy}";
    }

    void OnJudgeConfirmClicked()
    {
        _judgeAcknowledged = true;
    }

    void OnPlayAgainClicked()
    {
        _chosePlayAgain = true;
        _waitingEndChoice = false;
    }

    void OnCloseEndClicked()
    {
        _chosePlayAgain = false;
        _waitingEndChoice = false;
    }

    static void SetPanel(GameObject panel, bool on)
    {
        if (panel != null)
            panel.SetActive(on);
    }

    void HideBettingStepRoots()
    {
        if (betAmountStepRoot != null)
            betAmountStepRoot.SetActive(false);
        if (betSideAndStartStepRoot != null)
            betSideAndStartStepRoot.SetActive(false);
    }

    /// <summary>
    /// 流程無法開始時：顯示 Judge 說明、等確認、再顯示 End（與正常結束一致，避免玩家以為按鈕壞了）。
    /// </summary>
    IEnumerator AbortFlowAndNotifyUser(string message)
    {
        Debug.LogWarning("[GambleUIController] " + message);
        SetPanel(endPanel, false);
        HideBettingStepRoots();
        SetRoundRollHudVisible(false);
        if (coinDeductPopupRoot != null)
            coinDeductPopupRoot.SetActive(false);
        if (judgeDetailText != null)
            judgeDetailText.text = message;
        SetPanel(judgePanel, true);
        _judgeAcknowledged = false;
        yield return new WaitUntil(() => _judgeAcknowledged);
        SetPanel(judgePanel, false);
        SetPanel(endPanel, true);
        _waitingEndChoice = true;
        _chosePlayAgain = false;
        yield return new WaitUntil(() => !_waitingEndChoice);
        SetPanel(endPanel, false);
        if (!_chosePlayAgain)
            CloseRoot();
        _flow = null;
    }
}
