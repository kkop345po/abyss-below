using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    /// <summary>執行期曾寫入過的 ACH_PROG 鍵（含動態 Kill_xxx），供存檔一併序列化。</summary>
    private static readonly HashSet<string> RuntimeProgressKeys = new HashSet<string>();

    /// <summary>非 AchievementData 上、程式內會寫入的進度鍵。</summary>
    private static readonly string[] AdditionalAchievementProgressKeys =
    {
        "TotalGold", "TotalKills", "GameStart", "Survivor", "Kill_SmallBoss", "Kill_BigOctopus", "Collection_AllWeapons",
        "Level_Speed", "Level_Oxygen", "Level_Stamina"
    };

    [Header("資料來源")]
    public List<AchievementData> allAchievements; // 請在 Inspector 中拖入所有成就

    [Header("全副武裝 (ACH_12)")]
    [Tooltip("規則：武器升級達最高等（WeaponUpgradeLevel == Max）時解鎖 Collection_AllWeapons。")]
    public bool unlockAllArmedByWeaponUpgradeMax = true;

    [Header("UI 引用 (可選)")]
    public AchievementUI achievementUI; // 用來顯示通知

    // 執行時緩存
    private HashSet<string> unlockedIDs = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        InventoryManager.OnInventoryChanged += OnInventoryChangedRefreshCollection;
        SceneManager.sceneLoaded += OnSceneLoadedResolveUi;
    }

    private void OnDisable()
    {
        InventoryManager.OnInventoryChanged -= OnInventoryChangedRefreshCollection;
        SceneManager.sceneLoaded -= OnSceneLoadedResolveUi;
    }

    private void OnSceneLoadedResolveUi(Scene scene, LoadSceneMode mode)
    {
        TryResolveAchievementUI();
    }

    void TryResolveAchievementUI()
    {
        if (achievementUI != null)
            return;
        achievementUI = Object.FindFirstObjectByType<AchievementUI>(FindObjectsInactive.Include);
    }

    static void EnsureInstance()
    {
        if (Instance != null)
            return;
        Instance = Object.FindFirstObjectByType<AchievementManager>(FindObjectsInactive.Include);
    }

    private void OnInventoryChangedRefreshCollection()
    {
        RefreshWeaponCollectionAchievement();
    }

    private void Start()
    {
        TryResolveAchievementUI();

        // ACH_01「初次下潛」等：對應 targetKey GameStart
        if (allAchievements != null && allAchievements.Count > 0)
            AddProgress("GameStart", 1);

        RefreshWeaponCollectionAchievement();
    }

    private void LoadProgress()
    {
        unlockedIDs.Clear();
        if (allAchievements == null) return;

        foreach (var ach in allAchievements)
        {
            if (ach == null) continue;
            // 讀取解鎖狀態 (1=Unlocked, 0=Locked)
            if (PlayerPrefs.GetInt($"ACH_UNLOCKED_{ach.id}", 0) == 1)
            {
                unlockedIDs.Add(ach.id);
            }
        }
    }

    public static void ClearRuntimeAchievementProgressKeyCache()
    {
        RuntimeProgressKeys.Clear();
    }

    static void TrackProgressKey(string targetKey)
    {
        if (!string.IsNullOrEmpty(targetKey))
            RuntimeProgressKeys.Add(targetKey);
    }

    /// <summary>寫入存檔：所有成就解鎖鍵、SO 上 targetKey 進度、額外鍵與執行期出現過的進度鍵。</summary>
    public List<AchievementPrefEntry> BuildAchievementSaveList()
    {
        var map = new Dictionary<string, int>();

        if (allAchievements != null)
        {
            foreach (var ach in allAchievements)
            {
                if (ach == null) continue;
                string uk = $"ACH_UNLOCKED_{ach.id}";
                map[uk] = PlayerPrefs.GetInt(uk, 0);
                if (!string.IsNullOrEmpty(ach.targetKey))
                {
                    string pk = $"ACH_PROG_{ach.targetKey}";
                    map[pk] = PlayerPrefs.GetInt(pk, 0);
                }
            }
        }

        foreach (var k in AdditionalAchievementProgressKeys)
        {
            string pk = $"ACH_PROG_{k}";
            map[pk] = PlayerPrefs.GetInt(pk, 0);
        }

        foreach (var tk in RuntimeProgressKeys)
        {
            if (string.IsNullOrEmpty(tk)) continue;
            string pk = $"ACH_PROG_{tk}";
            map[pk] = PlayerPrefs.GetInt(pk, 0);
        }

        var list = new List<AchievementPrefEntry>(map.Count);
        foreach (var kv in map)
            list.Add(new AchievementPrefEntry { key = kv.Key, value = kv.Value });
        return list;
    }

    /// <summary>讀檔後還原成就 PlayerPrefs 並刷新快取。</summary>
    public void ApplyAchievementSnapshot(List<AchievementPrefEntry> entries)
    {
        if (entries == null) return;
        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrEmpty(e.key)) continue;
            PlayerPrefs.SetInt(e.key, e.value);
            if (e.key.StartsWith("ACH_PROG_"))
            {
                string tk = e.key.Substring("ACH_PROG_".Length);
                if (!string.IsNullOrEmpty(tk))
                    RuntimeProgressKeys.Add(tk);
            }
        }
        PlayerPrefs.Save();
        LoadProgress();
    }

    /// <summary>
    /// 依背包檢查是否集滿武器／工具（ACH_12）。
    /// </summary>
    public void RefreshWeaponCollectionAchievement()
    {
        if (!unlockAllArmedByWeaponUpgradeMax || allAchievements == null)
            return;

        bool needsCollectionKey = allAchievements.Any(a => a != null && a.targetKey == "Collection_AllWeapons");
        if (!needsCollectionKey) return;

        if (PlayerDataManager.WeaponUpgradeLevel >= PlayerDataManager.WeaponUpgradeMaxLevel)
            SetProgress("Collection_AllWeapons", 1);
    }

    /// <summary>
    /// 更新進度 (累積型)
    /// </summary>
    /// <param name="targetKey">追蹤鍵值 (如 Kill_HouliCrab)</param>
    /// <param name="amount">增加的數量</param>
    public void AddProgress(string targetKey, int amount)
    {
        if (string.IsNullOrEmpty(targetKey) || amount == 0) return;
        EnsureInstance();
        if (Instance == null)
        {
            Debug.LogWarning($"[Achievement] 無 AchievementManager，略過進度 '{targetKey}'。");
            return;
        }
        if (this != Instance)
        {
            Instance.AddProgress(targetKey, amount);
            return;
        }
        TrackProgressKey(targetKey);

        string prefKey = $"ACH_PROG_{targetKey}";
        int current = PlayerPrefs.GetInt(prefKey, 0);
        int newValue = current + amount;
        PlayerPrefs.SetInt(prefKey, newValue);
        PlayerPrefs.Save();
        
        Debug.Log($"[Achievement] Progress '{targetKey}': {current} -> {newValue}");

        CheckAchievements(targetKey, newValue);
    }

    /// <summary>
    /// 更新進度 (設定型，如等級)
    /// </summary>
    /// <param name="targetKey">追蹤鍵值 (如 Level_Oxygen)</param>
    /// <param name="value">當前數值</param>
    public void SetProgress(string targetKey, int value)
    {
        if (string.IsNullOrEmpty(targetKey)) return;
        TrackProgressKey(targetKey);

        string prefKey = $"ACH_PROG_{targetKey}";
        PlayerPrefs.SetInt(prefKey, value);
        PlayerPrefs.Save();
        
        Debug.Log($"[Achievement] Set '{targetKey}': {value}");

        CheckAchievements(targetKey, value);
    }

    private void CheckAchievements(string key, int currentValue)
    {
        if (allAchievements == null) return;

        // 找出所有與此 Key 相關且尚未解鎖的成就
        var related = allAchievements.Where(a => a != null && a.targetKey == key && !unlockedIDs.Contains(a.id));

        foreach (var ach in related)
        {
            if (currentValue >= ach.targetValue)
            {
                UnlockAchievement(ach);
            }
        }
    }

    private void UnlockAchievement(AchievementData ach)
    {
        if (unlockedIDs.Contains(ach.id)) return;

        // 1. 標記解鎖
        unlockedIDs.Add(ach.id);
        PlayerPrefs.SetInt($"ACH_UNLOCKED_{ach.id}", 1);
        PlayerPrefs.Save();

        Debug.Log($"[Achievement] UNLOCKED: {ach.title}");

        // 2. 顯示通知
        TryResolveAchievementUI();
        if (achievementUI != null)
            achievementUI.ShowNotification(ach);
        else
            Debug.LogWarning("[AchievementManager] 找不到 AchievementUI，無法顯示成就通知。");
    }

    // 輔助方法：獲取當前進度值
    public int GetCurrentProgress(string targetKey)
    {
        return PlayerPrefs.GetInt($"ACH_PROG_{targetKey}", 0);
    }
    
    // 輔助方法：檢查是否解鎖
    public bool IsUnlocked(string id)
    {
        return unlockedIDs.Contains(id);
    }
}
