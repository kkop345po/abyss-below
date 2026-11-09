using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[SaveSystem] 初始化並設置為持久化");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadPlayerData();
    }

    public static void SavePlayerData()
    {
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] 保存玩家数据");
    }

    public static void LoadPlayerData()
    {
        // 持久數值由 PlayerPrefs／各管理器在存取時讀取；花枝丸 Buff 僅記憶體內、可跨場景，
        // 不可從存檔還原；僅在「從存檔槽載入」或 Reset 時清空，見 SaveSlotData.ApplyToGame／ResetAllData。
        Debug.Log("[SaveSystem] 读取玩家数据");
    }

    public static void ResetAllData()
    {
        BuffManager.ClearAllBuffsStatic();
        SaveSlotSystem.DeleteAllSaveFiles();
        AchievementManager.ClearRuntimeAchievementProgressKeyCache();
        PlayerPrefs.DeleteAll();
        InventoryManager.ApplyGlobalPrefsResetToInventory();
        // 重新初始化玩家数据
        var sceneManager = GameObject.FindObjectOfType<ScenePlayerManager>();
        if (sceneManager != null)
        {
            // 将 InitializeBaseData 改成 public 才能调用
            sceneManager.InitializeBaseData();
            Debug.Log("[SaveSystem] 玩家数据已初始化");
        }
        else
        {
            Debug.LogWarning("[SaveSystem] 找不到 ScenePlayerManager，无法初始化数据！");
        }
    }
}