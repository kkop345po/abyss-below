using UnityEngine;

/// <summary>
/// 魚叉／<see cref="Bullet"/> 武器強化：五階段數值與消耗（金幣＋指定 <see cref="MaterialType"/>）。
/// </summary>
public static class WeaponUpgradeBalance
{
    public const int MaxLevel = 5;

    /// <summary>Lv1–2 相乘：1.2 × 1.3；Lv3 起不再堆疊傷害倍率（改穿透／散射／節奏）。</summary>
    public static float GetDamageMultiplier(int unlockedLevel)
    {
        if (unlockedLevel <= 0) return 1f;
        float m = 1f;
        if (unlockedLevel >= 1) m *= 1.2f;
        if (unlockedLevel >= 2) m *= 1.3f;
        return m;
    }

    /// <summary>Lv4 起三發散射。</summary>
    public static int GetProjectileCount(int unlockedLevel) => unlockedLevel >= 4 ? 3 : 1;

    public static float GetSpreadHalfAngleDegrees() => 14f;

    /// <summary>Lv3 起：可額外貫穿 1 名敵人（第一隻滿傷，第二隻為上一發傷害的 <see cref="PierceDamageFactorAfterHit"/>）。</summary>
    public static int GetPierceBudget(int unlockedLevel) => unlockedLevel >= 3 ? 1 : 0;

    /// <summary>穿過第一名敵人後，下一發命中傷害＝當前傷害 × 此係數（0.3 ≈ 降低 70%）。</summary>
    public const float PierceDamageFactorAfterHit = 0.3f;

    /// <summary>Lv5：攻擊段／換彈／冷卻整體時間 × 此值（愈小愈快）。</summary>
    public static float GetPhaseTimeScale(int unlockedLevel) => unlockedLevel >= 5 ? 0.75f : 1f;

    /// <summary>
    /// 即將從 <paramref name="fromLevel"/> 升到 <paramref name="fromLevel"/>+1 的消耗；fromLevel 範圍 0～4。
    /// 第二組素材：<paramref name="materialCountB"/> 為 0 時可忽略 <paramref name="materialB"/>。
    /// </summary>
    public static bool TryGetCostForNextLevel(
        int fromLevel,
        out int goldCost,
        out MaterialType materialA,
        out int materialCountA,
        out MaterialType materialB,
        out int materialCountB)
    {
        goldCost = 0;
        materialA = MaterialType.None;
        materialCountA = 0;
        materialB = MaterialType.None;
        materialCountB = 0;
        if (fromLevel < 0 || fromLevel >= MaxLevel) return false;

        switch (fromLevel)
        {
            case 0:
                goldCost = 180;
                materialA = MaterialType.海膽冷盤;
                materialCountA = 14;
                materialB = MaterialType.鳳梨蝦球;
                materialCountB = 6;
                break;
            case 1:
                goldCost = 420;
                materialA = MaterialType.鳳梨蝦球;
                materialCountA = 20;
                materialB = MaterialType.蟹黃膏;
                materialCountB = 8;
                break;
            case 2:
                goldCost = 680;
                materialA = MaterialType.蟹黃膏;
                materialCountA = 16;
                materialB = MaterialType.三杯中卷;
                materialCountB = 10;
                break;
            case 3:
                goldCost = 1050;
                materialA = MaterialType.三杯中卷;
                materialCountA = 24;
                materialB = MaterialType.旗魚壽司;
                materialCountB = 12;
                break;
            case 4:
                goldCost = 1600;
                materialA = MaterialType.旗魚壽司;
                materialCountA = 18;
                materialB = MaterialType.砂鍋魚頭;
                materialCountB = 10;
                break;
            default:
                return false;
        }

        return true;
    }

    /// <summary>解鎖後對應階的標題／機制說明／體感（1～5）。</summary>
    public static void GetTierCopy(int tier1To5, out string title, out string statLine, out string feelLine)
    {
        switch (tier1To5)
        {
            case 1:
                title = "攻擊力提升 I";
                statLine = "基礎傷害 +20%（乘在魚叉子彈預設傷害上）";
                feelLine = "打怪變快一點。";
                break;
            case 2:
                title = "攻擊力提升 II";
                statLine = "基礎傷害再 ×1.3（與 Lv.1 相乘）";
                feelLine = "有機會秒殺初級小怪。";
                break;
            case 3:
                title = "穿透";
                statLine = "可再貫穿 1 名敵人；貫穿後該發子彈傷害變為 30%（約降低 70%），每發最多兩隻。";
                feelLine = "排成線的怪變好清。";
                break;
            case 4:
                title = "散射";
                statLine = "每次射擊改為 3 發扇形子彈（共用同一套傷害／穿透規則）。";
                feelLine = "中近距離壓制力大增。";
                break;
            case 5:
                title = "綜合強化";
                statLine = "攻擊硬直、換彈與冷卻整體約快 25%（時間 ×0.75）。";
                feelLine = "輸出節奏更流暢。";
                break;
            default:
                title = "";
                statLine = "";
                feelLine = "";
                break;
        }
    }
}
