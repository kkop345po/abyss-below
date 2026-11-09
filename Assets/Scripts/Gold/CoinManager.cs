using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;
    public TextMeshProUGUI coinText;

    /// <summary>賭博／商店等需要金幣邏輯時，確保場景內有 CoinManager（輸出檔若未載入含預製體的場景）。</summary>
    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        var existing = Object.FindFirstObjectByType<CoinManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (!existing.gameObject.activeInHierarchy)
                existing.gameObject.SetActive(true);
            return;
        }

        var go = new GameObject(nameof(CoinManager));
        go.AddComponent<CoinManager>();
    }

    private string _lastWarnSceneName;
    private bool _isRetryBinding;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[CoinManager] 初始化並設置為持久化");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SaveSystem.LoadPlayerData(); //  開始時載入

        PlayerDataManager.CoinsChanged -= OnCoinsChanged;
        PlayerDataManager.CoinsChanged += OnCoinsChanged;
    }

    private void Start()
    {
        // 在 Start 中更新 UI，確保 coinText 已經被設置
        UpdateCoinUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            PlayerDataManager.CoinsChanged -= OnCoinsChanged;
    }

    private void OnCoinsChanged(int _)
    {
        UpdateCoinUI();
    }

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
        coinText = null;
        _lastWarnSceneName = null;

        // 場景載入後延遲幾幀再綁定（避免 UI 還沒 Instantiate 完就找不到）
        if (!_isRetryBinding)
            StartCoroutine(RetryBindCoinTextThenUpdate());
    }

    public void AddCoins(int amount)
    {
        PlayerDataManager.Coins += amount;
        UpdateCoinUI();
        SaveSystem.SavePlayerData();

        // [Achievement Hook]
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.AddProgress("TotalGold", amount);
        }
    }

    public void SpendCoins(int amount)
    {
        PlayerDataManager.Coins -= amount;
        UpdateCoinUI();
        SaveSystem.SavePlayerData();
    }

    public bool HasEnoughCoins(int amount)
    {
        return PlayerDataManager.Coins >= amount;
    }

    public void UpdateCoinUI()
    {
        // 檢查 coinText 是否因為場景切換而被銷毀 (Unity Object null check)
        if (coinText != null && coinText.gameObject == null)
        {
            coinText = null;
        }

        // 如果 coinText 未設置，嘗試自動尋找
        if (coinText == null)
        {
            TryBindCoinTextInScene();
        }

        if (coinText != null)
        {
            int coins = PlayerDataManager.Coins;
            coinText.text = coins.ToString();
            Debug.Log($"[CoinManager] 更新金幣UI: {coins}");
        }
        else
        {
            // 避免每幀刷屏：每個場景只警告一次
            string sceneName = SceneManager.GetActiveScene().name;
            if (_lastWarnSceneName != sceneName)
            {
                _lastWarnSceneName = sceneName;
                Debug.LogWarning($"[CoinManager] coinText 未設置，無法更新UI！請在場景「{sceneName}」的 UI 文字命名包含 Coin/Gold/金幣，或在 Inspector 設置 coinText。");
            }
        }
    }

    public void AddCoin(int amount)
    {
        AddCoins(amount);
    }

    private IEnumerator RetryBindCoinTextThenUpdate()
    {
        _isRetryBinding = true;

        // 等待 UI 初始化（最多 10 幀）
        for (int i = 0; i < 10 && coinText == null; i++)
        {
            yield return null;
            TryBindCoinTextInScene();
        }

        _isRetryBinding = false;
        UpdateCoinUI();
    }

    private void TryBindCoinTextInScene()
    {
        // 規則：優先只綁定 HUD 的 CoinBG 內金幣數字，避免誤綁到商店/花費提示。
        // 找不到 CoinBG 才退回全場景掃描。

        bool IsUnder(Transform t, string ancestorName)
        {
            if (t == null) return false;
            for (var p = t; p != null; p = p.parent)
            {
                if (p.name == ancestorName) return true;
            }
            return false;
        }

        bool LooksLikeCoinLabel(TextMeshProUGUI t)
        {
            if (t == null) return false;
            string name = t.name ?? "";
            return name.Contains("Coin") || name.Contains("Gold") || name.Contains("金幣");
        }

        bool LooksNumeric(TextMeshProUGUI t)
        {
            if (t == null) return false;
            if (string.IsNullOrEmpty(t.text)) return false;
            for (int c = 0; c < t.text.Length; c++)
            {
                char ch = t.text[c];
                if (ch < '0' || ch > '9') return false;
            }
            return true;
        }

        // 1) 先找 CoinBG 底下的候選（最符合你的 HUD 結構）
        var texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (texts == null || texts.Length == 0) return;

        TextMeshProUGUI bestInHud = null;
        int bestHudScore = int.MinValue;

        for (int i = 0; i < texts.Length; i++)
        {
            var t = texts[i];
            if (t == null || t.gameObject == null) continue;
            if (!IsUnder(t.transform, "CoinBG")) continue;
            if (!LooksLikeCoinLabel(t)) continue;

            int score = 0;
            string lower = (t.name ?? "").ToLowerInvariant();
            if (lower.Contains("coin")) score += 50;
            if (lower.Contains("gold")) score += 30;
            if ((t.name ?? "").Contains("金幣")) score += 30;
            if (lower.Contains("tmp")) score += 5;
            if (LooksNumeric(t)) score += 10;

            if (score > bestHudScore)
            {
                bestHudScore = score;
                bestInHud = t;
            }
        }

        if (bestInHud != null)
        {
            coinText = bestInHud;
            Debug.Log($"[CoinManager] 自動綁定 HUD 金幣文字(CoinBG): {coinText.name}（scene={coinText.gameObject.scene.name}）");
            return;
        }

        // 2) 找不到 CoinBG 再退回：全場景掃描（仍以「像金幣數字」為主，降低誤綁商店文字機率）
        TextMeshProUGUI best = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < texts.Length; i++)
        {
            var t = texts[i];
            if (t == null || t.gameObject == null) continue;
            if (!LooksLikeCoinLabel(t)) continue;

            string name = t.name ?? "";
            string lower = name.ToLowerInvariant();

            int score = 0;
            if (lower.Contains("coin")) score += 50;
            if (lower.Contains("gold")) score += 30;
            if (name.Contains("金幣")) score += 30;
            if (lower.Contains("tmp")) score += 5;
            if (LooksNumeric(t)) score += 10;

            // 反向扣分：明顯是商店/花費提示的字（避免誤綁）
            if (lower.Contains("shop") || lower.Contains("price") || lower.Contains("cost"))
                score -= 40;

            if (score > bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        if (best != null && bestScore >= 40)
        {
            coinText = best;
            Debug.Log($"[CoinManager] 自動綁定金幣文字(退回掃描): {coinText.name}（scene={coinText.gameObject.scene.name}）");
        }
    }
}
