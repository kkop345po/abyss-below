using System;
using UnityEngine;

/// <summary>玩家押注「大」或「小」。</summary>
public enum GambleBetSide
{
    Small = 0,
    Big = 1,
    /// <summary>
    /// （相容保留）舊版「玩家可選豹子（三個 6）」。
    /// 目前豹子改為「隱藏機制」：三顆骰子點數相同即為豹子，押大/押小皆可能觸發，不再提供此選項。
    /// </summary>
    LeopardSix = 2
}

/// <summary>依投注金額區間決定獲勝時發放的花枝丸道具等級（預設對齊 50／100／150 三檔押注）。</summary>
public enum GambleRewardTier
{
    SmallSquid = 0,   // 例：50（低於 100 金）
    LargeSquid = 1,   // 例：100
    EnhancedSquid = 2 // 例：150（達 150 金以上）
}

/// <summary>賭局結算資料：動畫前即已決定，演出層只負責呈現。</summary>
[Serializable]
public struct GambleOutcome
{
    public int Dice1;
    public int Dice2;
    public int Dice3;
    public int DiceSum;
    public bool IsSumBig;
    public GambleBetSide PlayerBet;
    public bool PlayerWon;
    public int BetAmount;
    public GambleRewardTier RewardTier;
    /// <summary>僅在 PlayerWon 時有效；對應 Inventory 的 itemId。</summary>
    public string RewardItemId;
    /// <summary>僅在 PlayerWon 時有效；一般大／小為 1，豹子中獎為 4。</summary>
    public int RewardQuantity;
    public bool UsedMercyWin;

    /// <summary>隱藏豹子：三顆骰子點數相同。</summary>
    public bool IsLeopard;
    /// <summary>僅在 <see cref="IsLeopard"/> 時有效：豹子的點數（1–6）。</summary>
    public int LeopardPip;
}

/// <summary>花枝丸道具與 Buff 對應的常數 ID（需與 ItemDatabase 一致）。</summary>
public static class GambleItemIds
{
    public const string SmallSquid = "gamble_squid_small";
    public const string LargeSquid = "gamble_squid_large";
    public const string EnhancedSquid = "gamble_squid_enhanced";

    public static string TierToItemId(GambleRewardTier tier)
    {
        switch (tier)
        {
            case GambleRewardTier.SmallSquid: return SmallSquid;
            case GambleRewardTier.LargeSquid: return LargeSquid;
            case GambleRewardTier.EnhancedSquid: return EnhancedSquid;
            default: return SmallSquid;
        }
    }

    /// <summary>與 UI 三鈕 50／100／150 對齊：150→強化、100→大、其餘（含 50）→小。</summary>
    public static GambleRewardTier TierFromBetAmount(int betAmount)
    {
        if (betAmount >= 150) return GambleRewardTier.EnhancedSquid;
        if (betAmount >= 100) return GambleRewardTier.LargeSquid;
        return GambleRewardTier.SmallSquid;
    }
}
