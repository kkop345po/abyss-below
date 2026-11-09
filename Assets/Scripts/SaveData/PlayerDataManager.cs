using System;
using UnityEngine;

public static class PlayerDataManager
{
    /// <summary>金幣數值變更時觸發（含 +=／-=／=）；參數為變更後的數量。</summary>
    public static event Action<int> CoinsChanged;

    public static int Coins
    {
        get => PlayerPrefs.GetInt("Coins", 0);
        set
        {
            int clamped = Mathf.Max(0, value);
            int prev = PlayerPrefs.GetInt("Coins", 0);
            PlayerPrefs.SetInt("Coins", clamped);
            if (clamped != prev)
                CoinsChanged?.Invoke(clamped);
        }
    }

    public static int SpeedLevel
    {
        get => PlayerPrefs.GetInt("Upgrade_Speed", 0);
        set => PlayerPrefs.SetInt("Upgrade_Speed", Mathf.Clamp(value, 0, MaxLevel));
    }

    public static int OxygenLevel
    {
        get => PlayerPrefs.GetInt("Upgrade_OxygenEfficiency", 0);
        set => PlayerPrefs.SetInt("Upgrade_OxygenEfficiency", Mathf.Clamp(value, 0, MaxLevel));
    }

    public static int StaminaLevel
    {
        get => PlayerPrefs.GetInt("Upgrade_Stamina", 0);
        set => PlayerPrefs.SetInt("Upgrade_Stamina", Mathf.Clamp(value, 0, MaxLevel));
    }

    /// <summary>魚叉武器強化階級 0～5（0＝未升級，5＝已解鎖全部階段）。</summary>
    public static int WeaponUpgradeLevel
    {
        get => PlayerPrefs.GetInt("WeaponUpgradeLevel", 0);
        set => PlayerPrefs.SetInt("WeaponUpgradeLevel", Mathf.Clamp(value, 0, WeaponUpgradeMaxLevel));
    }

    public const int WeaponUpgradeMaxLevel = 5;

    public static int MaxLevel => 5;

    public static int[] UpgradeCosts => new int[] { 50, 80, 140, 230, 450 };

    public static void ResetData()
    {
        BuffManager.ClearAllBuffsStatic();
        SaveSlotSystem.DeleteAllSaveFiles();
        AchievementManager.ClearRuntimeAchievementProgressKeyCache();
        PlayerPrefs.DeleteAll();
        InventoryManager.ApplyGlobalPrefsResetToInventory();
        Save();
        CoinsChanged?.Invoke(Coins);
    }

    /// <summary>只清除任務進度（不影響金幣／背包／升級等）。</summary>
    public static void ClearQuestProgress()
    {
        PlayerPrefs.DeleteKey(QuestManager.QuestPlayerPrefsIdKey);
        PlayerPrefs.DeleteKey(QuestManager.QuestPlayerPrefsProgressKey);
        Save();
    }

    public static string EquippedWeapon1
    {
        get => PlayerPrefs.GetString("EquippedWeapon1", "");
        set => PlayerPrefs.SetString("EquippedWeapon1", value);
    }

    public static string EquippedWeapon2
    {
        get => PlayerPrefs.GetString("EquippedWeapon2", "");
        set => PlayerPrefs.SetString("EquippedWeapon2", value);
    }

    public static string EquippedItem
    {
        get => PlayerPrefs.GetString("EquippedItem", "");
        set => PlayerPrefs.SetString("EquippedItem", value);
    }

    // ���⪬�A���[���ݩ�
    public static int CurrentHealth
    {
        get => PlayerPrefs.GetInt("CurrentHealth", -1); // -1 ���ܥ���l��
        set => PlayerPrefs.SetInt("CurrentHealth", Mathf.Max(0, value));
    }

    public static int SavedMaxHealth
    {
        get => PlayerPrefs.GetInt("SavedMaxHealth", -1);
        set => PlayerPrefs.SetInt("SavedMaxHealth", Mathf.Max(0, value));
    }

    public static float CurrentOxygen
    {
        get => PlayerPrefs.GetFloat("CurrentOxygen", -1f); // -1 ���ܥ���l��
        set => PlayerPrefs.SetFloat("CurrentOxygen", Mathf.Max(0f, value));
    }

    public static float CurrentStamina
    {
        get => PlayerPrefs.GetFloat("CurrentStamina", -1f); // -1 ���ܥ���l��
        set => PlayerPrefs.SetFloat("CurrentStamina", Mathf.Max(0f, value));
    }

    public static void Save() => PlayerPrefs.Save();

    /// <summary>
    /// 死亡回陸地等情境：清掉持久化血氧體力，讓新場景以「滿／預設」重生（PlayerPrefs 無值時 getter 為 -1）。
    /// </summary>
    public static void ClearPersistedVitalsForFreshSpawn()
    {
        PlayerPrefs.DeleteKey("CurrentHealth");
        PlayerPrefs.DeleteKey("SavedMaxHealth");
        PlayerPrefs.DeleteKey("CurrentOxygen");
        PlayerPrefs.DeleteKey("CurrentStamina");
        Save();
    }
}
