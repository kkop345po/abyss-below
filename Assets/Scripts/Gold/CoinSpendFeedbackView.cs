using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 商店／升級／素材兌換等：顯示目前金幣（訂閱 <see cref="PlayerDataManager.CoinsChanged"/>），
/// 並可播放扣款（-X＋遞減）或入帳（+X＋遞增）演出。
/// 變動金幣前請先 <see cref="SetAnimLock"/>（true），再 <c>StartCoroutine(PlaySpendPresentation... / PlayGainPresentation...)</c>。
/// </summary>
public class CoinSpendFeedbackView : MonoBehaviour
{
    [Header("金幣數字")]
    [SerializeField] private TextMeshProUGUI balanceText;

    [Header("加減錢提示（可選，扣款顯示 -X、入帳顯示 +X）")]
    [Tooltip("可留空：若已指定 Change Popup Text，會改為開啟該文字所在的 GameObject")]
    [SerializeField] private GameObject changePopupRoot;
    [SerializeField] private TextMeshProUGUI changePopupText;

    [Header("加減錢提示顏色")]
    [SerializeField] private Color spendPopupColor = new Color(0.95f, 0.28f, 0.28f);
    [SerializeField] private Color gainPopupColor = new Color(0.25f, 0.92f, 0.4f);

    [Header("時間")]
    [SerializeField] private float countAnimDuration = 0.65f;
    [SerializeField] private float popupHoldSeconds = 0.9f;

    [Tooltip("動畫期間是否同步驅動 CoinManager 主畫面金幣字（與賭桌一致）")]
    [SerializeField] private bool syncMainCoinManagerText = true;

    bool _animLock;
    Coroutine _presentationRoutine;
    MonoBehaviour _routineHost;
    readonly List<GameObject> _activatedForPresentation = new List<GameObject>();
    readonly List<GameObject> _activatedPopupObjects = new List<GameObject>();
    /// <summary>執行時由 Shop／升級等指定：與 Inspector 的 Balance Text 一併更新（例如商店大白字 Coin Text）。</summary>
    TextMeshProUGUI _externalBalanceMirror;

    Color _popupTextColorIdle;
    string _balanceDisplayFormat;
    float _popupHoldOverride = -1f;

    public void SetExternalBalanceMirror(TextMeshProUGUI mirror)
    {
        _externalBalanceMirror = mirror;
    }

    /// <summary>綁定升級畫面上的金幣 TMP（與 balanceText 二選一或並用鏡像）。</summary>
    public void BindBalanceText(TextMeshProUGUI text)
    {
        balanceText = text;
        RefreshBalanceText();
    }

    /// <summary>例：持有金幣 {0}；{0} 會替換成數字。</summary>
    public void SetBalanceDisplayFormat(string format)
    {
        _balanceDisplayFormat = string.IsNullOrWhiteSpace(format) ? null : format.Trim();
        RefreshBalanceText();
    }

    public void BindBalanceDisplay(TextMeshProUGUI text, string format)
    {
        BindBalanceText(text);
        SetBalanceDisplayFormat(format);
    }

    public void BindChangePopupText(TextMeshProUGUI text)
    {
        changePopupText = text;
        if (changePopupText != null)
            _popupTextColorIdle = changePopupText.color;
    }

    public void ResolveReferencesFromUpgradePanel(Transform upgradePanelRoot)
    {
        if (upgradePanelRoot == null) return;
        if (balanceText == null)
            balanceText = FindTmp(upgradePanelRoot, "CoinText") ?? FindTmp(upgradePanelRoot, "CoinBalanceText");
        if (changePopupText == null)
            changePopupText = FindTmp(upgradePanelRoot, "CoinChangeText");
        if (changePopupRoot == null && changePopupText != null)
            changePopupRoot = changePopupText.gameObject;
    }

    static TextMeshProUGUI FindTmp(Transform root, string objectName)
    {
        if (root == null) return null;
        foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.gameObject.name == objectName)
                return tmp;
        }
        return null;
    }

    void ApplyBalanceString(string s)
    {
        if (balanceText != null)
            balanceText.text = s;
        if (_externalBalanceMirror != null)
            _externalBalanceMirror.text = s;
    }

    void ApplyBalanceAmount(int amount)
    {
        string s = string.IsNullOrEmpty(_balanceDisplayFormat)
            ? amount.ToString()
            : string.Format(_balanceDisplayFormat, amount);
        ApplyBalanceString(s);
    }

    void OnEnable()
    {
        if (changePopupText != null)
            _popupTextColorIdle = changePopupText.color;
        else if (changePopupRoot != null)
            changePopupText = changePopupRoot.GetComponent<TextMeshProUGUI>();

        PlayerDataManager.CoinsChanged += OnCoinsChanged;
        RefreshBalanceText();
    }

    void OnDisable()
    {
        PlayerDataManager.CoinsChanged -= OnCoinsChanged;
        CancelPresentation();
    }

    void OnCoinsChanged(int newCoins)
    {
        if (_animLock) return;
        if (balanceText == null && _externalBalanceMirror == null) return;
        ApplyBalanceAmount(newCoins);
    }

    public void RefreshBalanceText()
    {
        if (balanceText == null && _externalBalanceMirror == null) return;
        ApplyBalanceAmount(PlayerDataManager.Coins);
    }

    /// <summary>在呼叫 <see cref="PlayerDataManager.Coins"/> 扣款之前設為 true，避免同步事件搶先改字。</summary>
    public void SetAnimLock(bool on) => _animLock = on;

    GameObject GetPopupHost()
    {
        if (changePopupRoot != null) return changePopupRoot;
        if (changePopupText != null) return changePopupText.gameObject;
        return null;
    }

    /// <summary>加減錢提示用獨立 TMP 時為 true；與餘額共用同一個 TMP 時為 false（不可單獨關閉該物件）。</summary>
    bool IsDedicatedPopupText()
    {
        return changePopupText != null && changePopupText != balanceText;
    }

    /// <summary>關閉 changePopupRoot 是否會一併隱藏餘額字（若會則不可整個關 Root）。</summary>
    bool BalanceTextIsUnderPopupRoot()
    {
        if (changePopupRoot == null || balanceText == null) return false;
        return balanceText.transform.IsChildOf(changePopupRoot.transform);
    }

    void ShowTransactionPopup(string line, bool useGainGreenColor)
    {
        EnsureHierarchyActiveForUi();

        if (changePopupText != null)
        {
            if (!changePopupText.gameObject.activeSelf)
            {
                changePopupText.gameObject.SetActive(true);
                _activatedPopupObjects.Add(changePopupText.gameObject);
            }

            changePopupText.text = line;
            changePopupText.color = useGainGreenColor ? gainPopupColor : spendPopupColor;
        }

        GameObject host = GetPopupHost();
        if (host != null && host != changePopupText?.gameObject)
        {
            if (!host.activeSelf)
            {
                host.SetActive(true);
                _activatedPopupObjects.Add(host);
            }
        }
    }

    void HideTransactionPopup()
    {
        if (changePopupText != null)
        {
            changePopupText.color = _popupTextColorIdle;
            if (IsDedicatedPopupText())
            {
                changePopupText.text = string.Empty;
                changePopupText.gameObject.SetActive(false);
            }
            else if (changePopupText == balanceText)
                RefreshBalanceText();
        }

        if (changePopupRoot != null && !BalanceTextIsUnderPopupRoot())
            changePopupRoot.SetActive(false);
    }

    /// <summary>中斷扣款／入帳演出並關閉 -X / +X 提示（例如升級 UI 的 StopAllCoroutines 打斷協程時）。</summary>
    public void CancelPresentation()
    {
        StopPresentationRoutine();
        HideTransactionPopup();
        RestoreInactiveAfterPresentation();
        _animLock = false;
        RefreshBalanceText();
        CoinManager.Instance?.UpdateCoinUI();
    }

    /// <param name="coroutineHost">本物件未啟用時，改在此 MonoBehaviour 上跑協程（例如 UpgradeUIController）。</param>
    public void RunSpendPresentation(int goldBeforeSpend, int spent, MonoBehaviour coroutineHost = null, float popupHoldOverride = -1f)
    {
        _popupHoldOverride = popupHoldOverride;
        BeginPresentationRoutine(SpendPresentationRoutine(goldBeforeSpend, spent), coroutineHost);
    }

    public void RunGainPresentation(int goldBeforeGain, int gained, MonoBehaviour coroutineHost = null, float popupHoldOverride = -1f)
    {
        _popupHoldOverride = popupHoldOverride;
        BeginPresentationRoutine(GainPresentationRoutine(goldBeforeGain, gained), coroutineHost);
    }

    float ResolvePopupHoldSeconds() =>
        _popupHoldOverride >= 0f ? _popupHoldOverride : popupHoldSeconds;

    void BeginPresentationRoutine(IEnumerator routine, MonoBehaviour coroutineHost)
    {
        StopPresentationRoutine();
        EnsureVisibleForPresentation();

        _routineHost = ResolveCoroutineHost(coroutineHost);
        if (_routineHost == null)
        {
            Debug.LogWarning("[CoinSpendFeedbackView] 無法播放金幣演出：請確認呼叫端傳入已啟用的 coroutineHost（例如 UpgradeUIController）。", this);
            CancelPresentation();
            return;
        }

        _presentationRoutine = _routineHost.StartCoroutine(routine);
    }

    void StopPresentationRoutine()
    {
        if (_presentationRoutine != null && _routineHost != null)
            _routineHost.StopCoroutine(_presentationRoutine);
        _presentationRoutine = null;
        _routineHost = null;
    }

    MonoBehaviour ResolveCoroutineHost(MonoBehaviour fallback)
    {
        if (IsRunnableHost(fallback))
            return fallback;
        if (IsRunnableHost(CoinManager.Instance))
            return CoinManager.Instance;
        if (IsRunnableHost(this))
            return this;
        return null;
    }

    static bool IsRunnableHost(MonoBehaviour host)
    {
        return host != null && host.isActiveAndEnabled && host.gameObject.activeInHierarchy;
    }

    /// <summary>升級／商店面板開啟時呼叫，確保本物件與父階層可顯示（不會在演出結束後自動關閉）。</summary>
    public void EnsureHierarchyActiveForUi()
    {
        for (Transform t = transform; t != null; t = t.parent)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
        }
    }

    void EnsureVisibleForPresentation()
    {
        _activatedForPresentation.Clear();
        if (changePopupRoot != null && !changePopupRoot.activeSelf)
        {
            changePopupRoot.SetActive(true);
            _activatedForPresentation.Add(changePopupRoot);
        }
    }

    void RestoreInactiveAfterPresentation()
    {
        for (int i = _activatedPopupObjects.Count - 1; i >= 0; i--)
        {
            if (_activatedPopupObjects[i] != null)
                _activatedPopupObjects[i].SetActive(false);
        }
        _activatedPopupObjects.Clear();

        for (int i = _activatedForPresentation.Count - 1; i >= 0; i--)
        {
            if (_activatedForPresentation[i] != null)
                _activatedForPresentation[i].SetActive(false);
        }
        _activatedForPresentation.Clear();
    }

    IEnumerator SpendPresentationRoutine(int goldBeforeSpend, int spent)
    {
        try
        {
            yield return PlaySpendPresentation(goldBeforeSpend, spent);
        }
        finally
        {
            StopPresentationRoutine();
            RestoreInactiveAfterPresentation();
        }
    }

    IEnumerator GainPresentationRoutine(int goldBeforeGain, int gained)
    {
        try
        {
            yield return PlayGainPresentation(goldBeforeGain, gained);
        }
        finally
        {
            StopPresentationRoutine();
            RestoreInactiveAfterPresentation();
        }
    }

    public IEnumerator PlaySpendPresentation(int goldBeforeSpend, int spent)
    {
        if (spent <= 0)
            yield break;

        try
        {
            EnsureHierarchyActiveForUi();
            int goldAfter = PlayerDataManager.Coins;
            TMP_Text globalCoin = syncMainCoinManagerText && CoinManager.Instance != null
                ? CoinManager.Instance.coinText
                : null;

            ShowTransactionPopup($"-{spent}", false);

            float dur = Mathf.Max(0.05f, countAnimDuration);
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                int v = Mathf.RoundToInt(Mathf.Lerp(goldBeforeSpend, goldAfter, k));
                ApplyBalanceAmount(v);
                if (globalCoin != null)
                    globalCoin.text = v.ToString();
                yield return null;
            }

            ApplyBalanceAmount(goldAfter);
            CoinManager.Instance?.UpdateCoinUI();

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, ResolvePopupHoldSeconds()));

            HideTransactionPopup();
        }
        finally
        {
            _animLock = false;
            _popupHoldOverride = -1f;
            HideTransactionPopup();
        }
    }

    /// <summary>素材賣出等「加金幣」後播放：+X 彈窗與數字由扣前遞增至目前餘額。</summary>
    public IEnumerator PlayGainPresentation(int goldBeforeGain, int gained)
    {
        try
        {
            int goldAfter = PlayerDataManager.Coins;
            TMP_Text globalCoin = syncMainCoinManagerText && CoinManager.Instance != null
                ? CoinManager.Instance.coinText
                : null;

            ShowTransactionPopup($"+{gained}", true);

            float dur = Mathf.Max(0.05f, countAnimDuration);
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                int v = Mathf.RoundToInt(Mathf.Lerp(goldBeforeGain, goldAfter, k));
                ApplyBalanceAmount(v);
                if (globalCoin != null)
                    globalCoin.text = v.ToString();
                yield return null;
            }

            ApplyBalanceAmount(goldAfter);
            CoinManager.Instance?.UpdateCoinUI();

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, ResolvePopupHoldSeconds()));

            HideTransactionPopup();
        }
        finally
        {
            _animLock = false;
            _popupHoldOverride = -1f;
            HideTransactionPopup();
        }
    }
}
