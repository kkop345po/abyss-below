using UnityEngine;

public enum WeaponUpgradeAttemptResult
{
    Success,
    MaxLevel,
    MissingManagers,
    NotEnoughGold,
    NotEnoughMaterials
}

/// <summary>
/// 武器強化：檢查金幣／素材並寫入 <see cref="PlayerDataManager.WeaponUpgradeLevel"/>，並通知場上 <see cref="PlayerController"/>。
/// </summary>
public static class WeaponUpgradeService
{
    public static WeaponUpgradeAttemptResult TryUpgradeNext(out string message)
    {
        message = null;
        int cur = PlayerDataManager.WeaponUpgradeLevel;
        if (cur >= WeaponUpgradeBalance.MaxLevel)
        {
            message = "已達最高武器等級。";
            return WeaponUpgradeAttemptResult.MaxLevel;
        }

        if (InventoryManager.Instance == null)
        {
            message = "找不到 InventoryManager。";
            return WeaponUpgradeAttemptResult.MissingManagers;
        }

        if (!WeaponUpgradeBalance.TryGetCostForNextLevel(cur, out int gold, out MaterialType matA, out int needA, out MaterialType matB, out int needB))
        {
            message = "無法讀取消耗設定。";
            return WeaponUpgradeAttemptResult.MissingManagers;
        }

        if (PlayerDataManager.Coins < gold)
        {
            message = $"金幣不足（需要 {gold}）。";
            return WeaponUpgradeAttemptResult.NotEnoughGold;
        }

        if (InventoryManager.Instance.GetMaterialCount(matA) < needA)
        {
            message = $"素材不足（需要 {matA} x{needA}）。";
            return WeaponUpgradeAttemptResult.NotEnoughMaterials;
        }

        if (needB > 0 && matB != MaterialType.None && InventoryManager.Instance.GetMaterialCount(matB) < needB)
        {
            message = $"素材不足（需要 {matB} x{needB}）。";
            return WeaponUpgradeAttemptResult.NotEnoughMaterials;
        }

        if (CoinManager.Instance != null)
            CoinManager.Instance.SpendCoins(gold);
        else
            PlayerDataManager.Coins -= gold;

        InventoryManager.Instance.RemoveMaterial(matA, needA);
        if (needB > 0 && matB != MaterialType.None)
            InventoryManager.Instance.RemoveMaterial(matB, needB);
        InventoryManager.Instance.SaveInventory();
        InventoryManager.NotifyInventoryChanged();

        PlayerDataManager.WeaponUpgradeLevel = cur + 1;
        SaveSystem.SavePlayerData();

        NotifyPlayerApplyWeapon();

        // 成就：全副武裝（武器升級滿級）
        if (AchievementManager.Instance != null &&
            PlayerDataManager.WeaponUpgradeLevel >= PlayerDataManager.WeaponUpgradeMaxLevel)
        {
            AchievementManager.Instance.SetProgress("Collection_AllWeapons", 1);
        }
        message = $"武器升級成功！目前 Lv.{PlayerDataManager.WeaponUpgradeLevel}";
        return WeaponUpgradeAttemptResult.Success;
    }

    /// <summary>讀檔套用後呼叫，讓場上玩家立刻套用武器時間縮放。</summary>
    public static void RefreshWeaponStatsForLoadedPlayer() => NotifyPlayerApplyWeapon();

    static void NotifyPlayerApplyWeapon()
    {
        var players = Object.FindObjectsOfType<PlayerController>();
        foreach (var p in players)
            p.ApplyWeaponUpgrades();
    }

    /// <summary>供 UI 顯示素材圖示；已滿級或讀取消耗失敗時回 false。</summary>
    public static bool TryGetPendingUpgradeMaterials(out MaterialType matA, out int countA, out MaterialType matB, out int countB)
    {
        matA = MaterialType.None;
        countA = 0;
        matB = MaterialType.None;
        countB = 0;
        int cur = PlayerDataManager.WeaponUpgradeLevel;
        if (cur >= WeaponUpgradeBalance.MaxLevel)
            return false;
        if (!WeaponUpgradeBalance.TryGetCostForNextLevel(cur, out _, out matA, out countA, out matB, out countB))
            return false;
        return true;
    }

    /// <summary>供 UI 顯示「下一階」預覽；已滿級時回 false。</summary>
    public static bool TryBuildNextLevelPreview(out string title, out string statLine, out string feelLine, out string costLine)
    {
        title = statLine = feelLine = costLine = "";
        int cur = PlayerDataManager.WeaponUpgradeLevel;
        if (cur >= WeaponUpgradeBalance.MaxLevel)
            return false;

        int nextTier = cur + 1;
        WeaponUpgradeBalance.GetTierCopy(nextTier, out title, out statLine, out feelLine);
        if (!WeaponUpgradeBalance.TryGetCostForNextLevel(cur, out int gold, out MaterialType matA, out int cntA, out MaterialType matB, out int cntB))
            return false;
        costLine = $"金幣 {gold}　＋　{matA} x{cntA}";
        if (cntB > 0 && matB != MaterialType.None)
            costLine += $"　＋　{matB} x{cntB}";
        return true;
    }
}
