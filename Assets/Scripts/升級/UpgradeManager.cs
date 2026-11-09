using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    public enum UpgradeType { Speed, OxygenEfficiency, Stamina }
    public enum UpgradeResult { Success, MaxLevel, NotEnoughCoins }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[UpgradeManager] 初始化並設置為持久化");
            UpgradeUIController.EnsureOnSceneUpgradePanels();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public UpgradeResult TryUpgrade(UpgradeType type)
    {
        int level = GetLevel(type);
        if (level >= PlayerDataManager.MaxLevel)
            return UpgradeResult.MaxLevel;

        int cost = PlayerDataManager.UpgradeCosts[level];
        if (PlayerDataManager.Coins < cost)
            return UpgradeResult.NotEnoughCoins;

        // 使用 CoinManager 扣除金幣並更新 UI
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.SpendCoins(cost);
        }
        else
        {
            PlayerDataManager.Coins -= cost; // Fallback
            SaveSystem.SavePlayerData();
        }

        SetLevel(type, level + 1);
        // SaveSystem.SavePlayerData(); // SpendCoins 裡面已經會存檔了

        NotifyUpgradeAchievements(type);

        // 立即套用到玩家屬性（陸地 / 水中可能是不同 Controller）
        var land = Object.FindObjectOfType<LandPlayerController>();
        if (land != null)
            land.RecalculateFromUpgrades();
        var player = Object.FindObjectOfType<PlayerController>();
        if (player != null)
            player.RecalculateFromUpgrades();

        var health = Object.FindObjectOfType<PlayerHealth>();
        if (health != null)
            health.RecalculateFromUpgrades();

        return UpgradeResult.Success;
    }

    public int GetLevel(UpgradeType type)
    {
        return type switch
        {
            UpgradeType.Speed => PlayerDataManager.SpeedLevel,
            UpgradeType.OxygenEfficiency => PlayerDataManager.OxygenLevel,
            UpgradeType.Stamina => PlayerDataManager.StaminaLevel,
            _ => 0
        };
    }

    private void SetLevel(UpgradeType type, int newLevel)
    {
        switch (type)
        {
            case UpgradeType.Speed: PlayerDataManager.SpeedLevel = newLevel; break;
            case UpgradeType.OxygenEfficiency: PlayerDataManager.OxygenLevel = newLevel; break;
            case UpgradeType.Stamina: PlayerDataManager.StaminaLevel = newLevel; break;
        }
    }

    static void NotifyUpgradeAchievements(UpgradeType type)
    {
        if (AchievementManager.Instance == null)
            return;

        switch (type)
        {
            case UpgradeType.Speed:
                AchievementManager.Instance.SetProgress("Level_Speed", PlayerDataManager.SpeedLevel);
                break;
            case UpgradeType.OxygenEfficiency:
                AchievementManager.Instance.SetProgress("Level_Oxygen", PlayerDataManager.OxygenLevel);
                break;
            case UpgradeType.Stamina:
                AchievementManager.Instance.SetProgress("Level_Stamina", PlayerDataManager.StaminaLevel);
                break;
        }
    }
}
