using UnityEngine;

/// <summary>
/// 自動存檔管理器
/// 在特定時機自動保存遊戲狀態（使用 Slot 0）
/// </summary>
public class AutoSaveManager : MonoBehaviour
{
    public static AutoSaveManager Instance;
    
    [Header("自動存檔設定")]
    [Tooltip("場景切換時自動存檔")]
    public bool autoSaveOnSceneChange = true;
    
    [Tooltip("固定時間間隔自動存檔（秒），0 表示關閉")]
    public float autoSaveInterval = 300f; // 5 分鐘
    
    private float lastAutoSaveTime = 0f;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 避免在「進場第一幀（玩家/管理器尚未完成初始化）」就存檔，
        // 否則可能把尚未同步完成的暫態值（例如金幣 UI 或玩家狀態）寫回 Slot0，
        // 導致下一次切場景後看似被「回滾」。
        if (autoSaveOnSceneChange)
            Invoke(nameof(AutoSave), 0.25f);
        
        lastAutoSaveTime = Time.time;
    }
    
    void Update()
    {
        // 固定間隔自動存檔
        if (autoSaveInterval > 0f)
        {
            if (Time.time - lastAutoSaveTime >= autoSaveInterval)
            {
                AutoSave();
                lastAutoSaveTime = Time.time;
            }
        }
    }
    
    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (autoSaveOnSceneChange && scene.name != "Menu" && scene.name != "LoadingScene")
        {
            // 延遲一幀確保場景完全載入
            Invoke(nameof(AutoSave), 0.1f);
        }
    }
    
    /// <summary>
    /// 執行自動存檔
    /// </summary>
    public void AutoSave()
    {
        var ph = Object.FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
        if (ph != null && ph.GetCurrentHealth() <= 0)
        {
            Debug.Log("[AutoSaveManager] 玩家血量為 0，略過自動存檔以免寫入 0HP。");
            return;
        }

        Texture2D thumbnail = SaveSlotSystem.CaptureThumbnail();
        SaveSlotSystem.SaveToSlot(0, thumbnail);
        Debug.Log("[AutoSaveManager] 自動存檔完成");
    }
    
    /// <summary>
    /// 手動觸發自動存檔（供其他系統調用）
    /// </summary>
    public static void TriggerAutoSave()
    {
        if (Instance != null)
            Instance.AutoSave();
    }
}

