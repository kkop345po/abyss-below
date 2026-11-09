using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveSlotData
{
    [Header("存檔元數據")]
    public string saveVersion = "1.0";
    public string saveTime; // ISO 8601 格式
    public string sceneName;
    public float playTimeInSeconds;
    
    [Header("玩家狀態")]
    public int coins;
    public int currentHealth;
    /// <summary>存檔當下最大血量；舊槽位為 0 時載入會依 100 上限做比例還原。</summary>
    public int savedMaxHealth;
    public float currentOxygen;
    public float currentStamina;
    
    [Header("升級等級")]
    public int speedLevel;
    public int oxygenLevel;
    public int staminaLevel;
    public int weaponUpgradeLevel;
    
    [Header("裝備")]
    public string equippedWeapon1;
    public string equippedWeapon2;
    public string equippedItem;
    
    [Header("背包")]
    public List<InventoryItemData> inventoryItems = new List<InventoryItemData>();

    [Header("任務（與 QuestManager PlayerPrefs 同步；舊存檔無此區塊時不覆寫任務）")]
    public bool hasQuestSnapshot;
    public string questCurrentId;
    public string questObjectiveProgressJson;

    [Header("成就（ACH_UNLOCKED_* / ACH_PROG_*；舊存檔無則不覆寫）")]
    public bool hasAchievementSnapshot;
    public List<AchievementPrefEntry> achievementPrefs = new List<AchievementPrefEntry>();
    
    [Header("場景位置")]
    public Vector3 playerPosition;
    
    /// <summary>
    /// 創建當前遊戲狀態的存檔
    /// </summary>
    public static SaveSlotData CreateCurrent()
    {
        // 若 vitals key 被刻意清除（=-1，用來表示「下一場景滿狀態」），
        // 直接存 -1 會在 ApplyToGame 時被 clamp 成 0，導致載入後變 0HP/0氧/0體力。
        // 因此存檔時一律以「實際運行中的值」為準，沒有 runtime 時才退回 PlayerDataManager。
        int resolvedHealth = PlayerDataManager.CurrentHealth;
        int resolvedMaxHealth = PlayerDataManager.SavedMaxHealth;
        float resolvedOxygen = PlayerDataManager.CurrentOxygen;
        float resolvedStamina = PlayerDataManager.CurrentStamina;

        if (resolvedHealth < 0)
        {
            var ph = GameObject.FindObjectOfType<PlayerHealth>();
            if (ph != null)
            {
                resolvedHealth = ph.GetCurrentHealth();
                resolvedMaxHealth = ph.GetMaxHealth();
            }
        }
        else if (resolvedMaxHealth < 0)
        {
            var ph = GameObject.FindObjectOfType<PlayerHealth>();
            if (ph != null) resolvedMaxHealth = ph.GetMaxHealth();
        }
        if (resolvedOxygen < 0f)
        {
            var om = GameObject.FindObjectOfType<OxygenManager>();
            if (om != null) resolvedOxygen = om.GetCurrentOxygen();
        }
        if (resolvedStamina < 0f)
        {
            var ps = GameObject.FindObjectOfType<PlayerStamina>();
            if (ps != null) resolvedStamina = ps.GetCurrentStamina();
        }

        if (resolvedHealth <= 0)
        {
            var ph = GameObject.FindObjectOfType<PlayerHealth>();
            if (ph != null)
            {
                resolvedHealth = ph.GetMaxHealth();
                resolvedMaxHealth = ph.GetMaxHealth();
            }
        }

        var data = new SaveSlotData
        {
            saveTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            coins = PlayerDataManager.Coins,
            currentHealth = Mathf.Max(0, resolvedHealth),
            savedMaxHealth = Mathf.Max(0, resolvedMaxHealth),
            currentOxygen = Mathf.Max(0f, resolvedOxygen),
            currentStamina = Mathf.Max(0f, resolvedStamina),
            speedLevel = PlayerDataManager.SpeedLevel,
            oxygenLevel = PlayerDataManager.OxygenLevel,
            staminaLevel = PlayerDataManager.StaminaLevel,
            weaponUpgradeLevel = PlayerDataManager.WeaponUpgradeLevel,
            equippedWeapon1 = PlayerDataManager.EquippedWeapon1,
            equippedWeapon2 = PlayerDataManager.EquippedWeapon2,
            equippedItem = PlayerDataManager.EquippedItem
        };
        
        // 收集背包數據
        if (InventoryManager.Instance != null)
        {
            foreach (var item in InventoryManager.Instance.items)
            {
                data.inventoryItems.Add(new InventoryItemData
                {
                    itemId = item.itemId,
                    itemName = item.itemName,
                    itemType = item.itemType.ToString(),
                    quantity = item.quantity
                });
            }
        }

        data.hasQuestSnapshot = true;
        data.questCurrentId = PlayerPrefs.GetString(QuestManager.QuestPlayerPrefsIdKey, "");
        data.questObjectiveProgressJson = PlayerPrefs.GetString(QuestManager.QuestPlayerPrefsProgressKey, "");

        if (AchievementManager.Instance != null)
        {
            data.hasAchievementSnapshot = true;
            data.achievementPrefs = AchievementManager.Instance.BuildAchievementSaveList();
        }
        
        // 收集玩家位置
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            data.playerPosition = player.transform.position;
        }
        
        return data;
    }
    
    /// <summary>
    /// 應用存檔數據到遊戲
    /// </summary>
    public void ApplyToGame()
    {
        // 從槽位還原快照：執行中的花枝丸 Buff 不屬於存檔，必須清空以免與舊進度混用。
        BuffManager.ClearAllBuffsStatic();

        PlayerDataManager.Coins = coins;
        // 槽位若為 0HP（死亡殘留等），勿寫入 PlayerPrefs，讓進場以滿血初始化
        if (currentHealth <= 0)
        {
            PlayerPrefs.DeleteKey("CurrentHealth");
            PlayerPrefs.DeleteKey("SavedMaxHealth");
        }
        else
        {
            PlayerDataManager.CurrentHealth = currentHealth;
            PlayerDataManager.SavedMaxHealth = savedMaxHealth > 0 ? savedMaxHealth : 0;
        }
        PlayerDataManager.CurrentOxygen = currentOxygen;
        PlayerDataManager.CurrentStamina = currentStamina;
        PlayerDataManager.SpeedLevel = speedLevel;
        PlayerDataManager.OxygenLevel = oxygenLevel;
        PlayerDataManager.StaminaLevel = staminaLevel;
        PlayerDataManager.WeaponUpgradeLevel = weaponUpgradeLevel;
        PlayerDataManager.EquippedWeapon1 = equippedWeapon1;
        PlayerDataManager.EquippedWeapon2 = equippedWeapon2;
        PlayerDataManager.EquippedItem = equippedItem;
        
        // 恢復背包
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.items.Clear();
            foreach (var itemData in inventoryItems)
            {
                var item = new InventoryItem
                {
                    itemId = itemData.itemId,
                    itemName = itemData.itemName,
                    itemType = ParseItemType(itemData.itemType),
                    quantity = itemData.quantity
                };
                InventoryManager.Instance.items.Add(item);
            }
            InventoryManager.Instance.SaveInventory();
        }

        if (hasQuestSnapshot)
        {
            PlayerPrefs.SetString(QuestManager.QuestPlayerPrefsIdKey, questCurrentId ?? "");
            PlayerPrefs.SetString(QuestManager.QuestPlayerPrefsProgressKey, questObjectiveProgressJson ?? "");
            PlayerPrefs.Save();
            QuestManager.Instance?.ApplyPersistedQuestFromPrefsAfterSaveSlotLoad();
        }

        if (hasAchievementSnapshot && achievementPrefs != null && achievementPrefs.Count > 0)
            AchievementManager.Instance?.ApplyAchievementSnapshot(achievementPrefs);

        WeaponUpgradeService.RefreshWeaponStatsForLoadedPlayer();
        
        // 玩家座標改由 SaveSlotSystem.LoadFromSlot 在「同場景載入」時套用，避免與跨場景載入重複。
    }
    
    private ItemType ParseItemType(string typeStr)
    {
        if (Enum.TryParse<ItemType>(typeStr, out var type))
            return type;
        return ItemType.Material;
    }
    
    /// <summary>
    /// 獲取格式化的存檔時間
    /// </summary>
    public string GetFormattedTime()
    {
        if (DateTime.TryParse(saveTime, out var dt))
        {
            return dt.ToString("yyyy/MM/dd HH:mm");
        }
        return "未知時間";
    }
}

[System.Serializable]
public class InventoryItemData
{
    public string itemId;
    public string itemName;
    public string itemType;
    public int quantity;
}

[System.Serializable]
public class AchievementPrefEntry
{
    public string key;
    public int value;
}
