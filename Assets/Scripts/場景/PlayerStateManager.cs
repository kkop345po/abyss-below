using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStateManager : MonoBehaviour
{
    public static PlayerStateManager Instance;

    /// <summary>
    /// 某些流程（例如死亡回城）會先清空持久化 vitals，讓下一場景滿狀態重生；
    /// 這時若再執行 OnSceneTransition 保存（此刻玩家可能是 0HP），會把 0 覆蓋回去。
    /// </summary>
    public static bool SuppressNextSceneTransitionSave { get; set; }
    
    private PlayerHealth playerHealth;
    private OxygenManager oxygenManager;
    private PlayerStamina playerStamina;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PlayerStateManager] 初始化");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[PlayerStateManager] 場景載入: {scene.name}");
        
        // 延遲一幀後尋找玩家組件，確保玩家已經生成
        // 使用更長的延遲，確保 PlayerHealth.Start() 已經執行完 SetMaxHP()
        Invoke(nameof(FindPlayerComponents), 0.25f);
    }

    /// <summary>存檔載入等同場景時呼叫，讓血氧體力與 PlayerPrefs 一致。</summary>
    public void RequestResyncPlayerVitals()
    {
        Invoke(nameof(FindPlayerComponents), 0.05f);
    }

    private void FindPlayerComponents()
    {
        // 尋找玩家組件
        playerHealth = FindObjectOfType<PlayerHealth>();
        oxygenManager = FindObjectOfType<OxygenManager>();
        playerStamina = FindObjectOfType<PlayerStamina>();

        if (playerHealth != null)
        {
            Debug.Log("[PlayerStateManager] 找到 PlayerHealth 組件");
            // 確保血量已從存檔同步（因為 SetMaxHP 可能已經在 Start 中調用，但需要再次確認）
            SyncPlayerHealth();
        }
        if (oxygenManager != null)
        {
            Debug.Log("[PlayerStateManager] 找到 OxygenManager 組件");
            // 同步氧氣
            int savedOxygen = PlayerDataManager.CurrentOxygen >= 0 ? (int)PlayerDataManager.CurrentOxygen : -1;
            if (savedOxygen >= 0)
            {
                oxygenManager.SetOxygen(savedOxygen);
                Debug.Log($"[PlayerStateManager] 同步氧氣: {savedOxygen}");
            }
        }
        if (playerStamina != null)
        {
            Debug.Log("[PlayerStateManager] 找到 PlayerStamina 組件");
            // 同步體力
            float savedStamina = PlayerDataManager.CurrentStamina >= 0 ? PlayerDataManager.CurrentStamina : -1;
            if (savedStamina >= 0)
            {
                playerStamina.SetStamina(savedStamina);
                Debug.Log($"[PlayerStateManager] 同步體力: {savedStamina}");
            }
        }
    }

    /// <summary>
    /// 同步玩家血量（確保新場景的玩家使用保存的血量）
    /// </summary>
    private void SyncPlayerHealth()
    {
        if (playerHealth == null) return;

        playerHealth.RecalculateFromUpgrades();
        Debug.Log($"[PlayerStateManager] 同步血量: {playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}");
    }

    // 保存當前玩家狀態
    public void SavePlayerState()
    {
        if (playerHealth != null)
        {
            playerHealth.SaveCurrentHealth();
            Debug.Log($"[PlayerStateManager] 保存血量: {playerHealth.GetCurrentHealth()}");
        }

        if (oxygenManager != null)
        {
            oxygenManager.SaveCurrentOxygen();
            Debug.Log($"[PlayerStateManager] 保存氧氣: {oxygenManager.GetCurrentOxygen()}");
        }

        if (playerStamina != null)
        {
            playerStamina.SaveCurrentStamina();
            Debug.Log($"[PlayerStateManager] 保存體力: {playerStamina.GetCurrentStamina()}");
        }

        Debug.Log("[PlayerStateManager] 玩家狀態已保存");
    }

    // 在場景切換前調用此方法
    public void OnSceneTransition()
    {
        if (SuppressNextSceneTransitionSave)
        {
            SuppressNextSceneTransitionSave = false;
            return;
        }
        SavePlayerState();
    }
}