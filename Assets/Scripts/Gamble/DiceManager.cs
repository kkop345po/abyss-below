using UnityEngine;
using UnityEngine.Scripting;

/// <summary>
/// 賭博「數值預算制」邏輯：在動畫播放前即決定骰子點數、輸贈與獎勵道具。
/// 與演出層解耦；僅負責計算與連敗統計（Mercy）。
/// </summary>
[Preserve]
public class DiceManager : MonoBehaviour
{
    public static DiceManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapOnLoad()
    {
        EnsureExists();
    }

    /// <summary>輸出檔／未載入 Land 時仍確保有骰子邏輯（與 <see cref="BuffManager.EnsureExists"/> 相同策略）。</summary>
    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        var existing = Object.FindFirstObjectByType<DiceManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (!existing.gameObject.activeInHierarchy)
                existing.gameObject.SetActive(true);
            return;
        }

        var go = new GameObject(nameof(DiceManager));
        go.AddComponent<DiceManager>();
    }

    [Header("機率（數值預算）")]
    [Tooltip("不含幸運加權時的基礎獲勝機率（0–1）")]
    [Range(0f, 1f)]
    [SerializeField] private float baseWinChance = 0.48f;

    [Tooltip("LuckFactor 每 1 單位對獲勝機率的加權（可為負）")]
    [SerializeField] private float luckChancePerUnit = 0.04f;

    [Header("保底")]
    [SerializeField] private int mercyLossesRequired = 3;

    [Header("投注")]
    [SerializeField] private int minBet = 10;

    [Header("依賭注區間的勝率加減（加在基礎與幸運之後，最後仍 Clamp01）")]
    [Tooltip("低於此值用 Win Chance Delta Low（例：押 50）")]
    [SerializeField] private int midBetThreshold = 100;
    [Tooltip("達此值以上用 Win Chance Delta High（例：押 150）；介於中與高之間用 Mid（例：押 100）")]
    [SerializeField] private int highBetThreshold = 150;
    [Tooltip("低賭注區：加算勝率（例 0.04 = +4%，較友善）")]
    [SerializeField] private float winChanceDeltaLowTier = 0.04f;
    [Tooltip("中賭注區")]
    [SerializeField] private float winChanceDeltaMidTier = 0f;
    [Tooltip("高賭注區（獎勵最好，預設略降勝率）")]
    [SerializeField] private float winChanceDeltaHighTier = -0.06f;

    [Header("隱藏豹子（三個相同）")]
    [Tooltip("押大/押小時，玩家獲勝後額外觸發「三顆同點」的機率（0–1）。觸發時骰子會強制變成三同，且花枝丸數量為一般獲勝的倍數。")]
    [Range(0f, 1f)]
    [SerializeField] private float hiddenLeopardChanceOnWin = 0.03f;

    [Tooltip("隱藏豹子獎勵倍率（1=無加成；預設 4）")]
    [SerializeField] private int hiddenLeopardRewardItemMultiplier = 4;

    public int MinBet => minBet;

    /// <summary>連續輸球次數（用於 Mercy；不寫入存檔）。</summary>
    public int ConsecutiveLosses { get; private set; }

    /// <summary>可由關卡／裝備等外部系統覆寫的幸運值，預設 0。</summary>
    public float LuckFactor { get; set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 在扣款與播放動畫「之前」呼叫：回傳完整結算資料（含三顆骰子點數）。
    /// </summary>
    public bool TryResolveGamble(int betAmount, GambleBetSide bet, out GambleOutcome outcome)
    {
        outcome = default;
        if (betAmount < minBet)
        {
            Debug.LogWarning($"[DiceManager] 投注低於下限 {minBet}");
            return false;
        }

        var tier = GambleItemIds.TierFromBetAmount(betAmount);
        int d1, d2, d3;
        bool rollWin;
        bool mercyUsed;

        // （相容）舊版 LeopardSix：目前已改為隱藏豹子，若仍有舊按鈕呼叫，視為一般押大。
        if (bet == GambleBetSide.LeopardSix)
            bet = GambleBetSide.Big;

        {
            bool mercy = ConsecutiveLosses >= mercyLossesRequired;
            float betDelta = GetWinChanceDeltaForBetAmount(betAmount);
            float p = Mathf.Clamp01(baseWinChance + LuckFactor * luckChancePerUnit + betDelta);
            rollWin = mercy || Random.value < p;
            mercyUsed = mercy && rollWin;
            bool betIsBig = bet == GambleBetSide.Big;
            RollDiceMatchingBet(betIsBig, rollWin, out d1, out d2, out d3);
        }

        int sum = d1 + d2 + d3;
        bool sumIsBig = sum >= 11;
        bool isLeopard = (d1 == d2 && d2 == d3);
        int leopardPip = isLeopard ? d1 : 0;

        // 隱藏豹子：在「贏」且未本來就是三同時，有機率強制轉為三同（仍需符合大/小區間）
        if (rollWin && !isLeopard && hiddenLeopardChanceOnWin > 0f && Random.value < Mathf.Clamp01(hiddenLeopardChanceOnWin))
        {
            int pip = ChooseLeopardPipForBet(sumIsBig);
            d1 = d2 = d3 = pip;
            sum = d1 + d2 + d3;
            sumIsBig = sum >= 11;
            isLeopard = true;
            leopardPip = pip;
        }

        // 規則：出豹子（三顆同點）必定算玩家獲勝，不受押大/押小影響。
        // 這會覆蓋前面的 win chance / 骰點區間判定（可能讓「原本輸」變成「贏」）。
        if (isLeopard && !rollWin)
        {
            rollWin = true;
            mercyUsed = false;
        }

        int rewardQty = rollWin ? Mathf.Max(1, isLeopard ? hiddenLeopardRewardItemMultiplier : 1) : 0;

        outcome = new GambleOutcome
        {
            Dice1 = d1,
            Dice2 = d2,
            Dice3 = d3,
            DiceSum = sum,
            IsSumBig = sumIsBig,
            PlayerBet = bet,
            PlayerWon = rollWin,
            BetAmount = betAmount,
            RewardTier = tier,
            RewardItemId = rollWin ? GambleItemIds.TierToItemId(tier) : null,
            RewardQuantity = rewardQty,
            UsedMercyWin = mercyUsed,
            IsLeopard = isLeopard,
            LeopardPip = leopardPip
        };

        if (rollWin)
            ConsecutiveLosses = 0;
        else
            ConsecutiveLosses++;

        return true;
    }

    /// <summary>重設連敗計數（例如離開賭場 NPC 時可選用）。</summary>
    public void ResetMercyCounter()
    {
        ConsecutiveLosses = 0;
    }

    float GetWinChanceDeltaForBetAmount(int betAmount)
    {
        if (betAmount >= highBetThreshold)
            return winChanceDeltaHighTier;
        if (betAmount >= midBetThreshold)
            return winChanceDeltaMidTier;
        return winChanceDeltaLowTier;
    }

    static void RollDiceMatchingBet(bool betBig, bool playerWon, out int d1, out int d2, out int d3)
    {
        bool needBigSum = playerWon ? betBig : !betBig;
        int minSum = needBigSum ? 11 : 3;
        int maxSum = needBigSum ? 18 : 10;

        d1 = d2 = d3 = 1;
        for (int i = 0; i < 512; i++)
        {
            d1 = Random.Range(1, 7);
            d2 = Random.Range(1, 7);
            d3 = Random.Range(1, 7);
            int s = d1 + d2 + d3;
            if (s >= minSum && s <= maxSum)
                return;
        }
        d1 = needBigSum ? 6 : 1;
        d2 = needBigSum ? 6 : 1;
        d3 = needBigSum ? 6 : 1;
    }

    static int ChooseLeopardPipForBet(bool sumIsBig)
    {
        // 三同時：1,1,1=3；2,2,2=6；3,3,3=9；4,4,4=12；5,5,5=15；6,6,6=18
        // 小：1~3，大：4~6
        return sumIsBig ? Random.Range(4, 7) : Random.Range(1, 4);
    }
}
