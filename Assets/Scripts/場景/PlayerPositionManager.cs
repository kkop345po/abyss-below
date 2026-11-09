using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPositionManager : MonoBehaviour
{
    private static PlayerPositionManager instance;
    public static PlayerPositionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PlayerPositionManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("PlayerPositionManager");
                    instance = go.AddComponent<PlayerPositionManager>();
                    DontDestroyOnLoad(go);
                    Debug.Log("[PlayerPositionManager] 自動創建實例");
                }
            }
            return instance;
        }
    }

    // 儲存每個場景的玩家位置
    private Dictionary<string, Vector3> scenePositions = new Dictionary<string, Vector3>();
    
    // 儲存每個場景的玩家是否在水中的狀態
    private Dictionary<string, bool> sceneUnderwaterStates = new Dictionary<string, bool>();
    
    // 記錄傳送門位置：key = "來源場景->目標場景"，value = 傳送門在來源場景的位置
    private Dictionary<string, Vector3> portalPositions = new Dictionary<string, Vector3>();
    
    // 記錄上一場景名稱（用於查找返回傳送門）
    private string previousSceneName = "";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PlayerPositionManager] 位置管理器初始化");
        }
        else if (instance != this)
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
        string sceneName = scene.name;
        
        // 確保 previousSceneName 已同步
        SyncPreviousSceneName();
        
        Debug.Log($"[PlayerPositionManager] 場景載入：{sceneName}，上一場景：{previousSceneName}");
        
        // 延遲一幀執行，處理其他狀態恢復（如水下狀態）
        // 注意：位置現在由 ScenePlayerManager 在生成時直接設定，這裡主要作為保險和狀態恢復
        StartCoroutine(RestorePlayerState(sceneName));
    }

    private void SyncPreviousSceneName()
    {
        // 嘗試從 SceneHistoryManager 獲取上一場景（如果存在且我們沒有記錄）
        if (string.IsNullOrEmpty(previousSceneName) && SceneHistoryManager.Instance != null)
        {
            // 使用反射獲取 SceneHistoryManager 的 previousScene
            var previousSceneField = typeof(SceneHistoryManager).GetField("previousScene",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (previousSceneField != null)
            {
                string historyPreviousScene = (string)previousSceneField.GetValue(SceneHistoryManager.Instance);
                if (!string.IsNullOrEmpty(historyPreviousScene))
                {
                    previousSceneName = historyPreviousScene;
                    Debug.Log($"[PlayerPositionManager] 從 SceneHistoryManager 獲取上一場景：{previousSceneName}");
                }
            }
        }
    }

    private static Scene ResolveSpawnScene(string sceneName)
    {
        Scene s = SceneManager.GetSceneByName(sceneName);
        if (s.IsValid() && s.isLoaded)
            return s;
        return SceneManager.GetActiveScene();
    }

    private static bool SpawnPointBelongsToScene(SpawnPoint p, Scene scene)
    {
        return p != null && scene.IsValid() && p.gameObject.scene == scene;
    }

    /// <summary>
    /// 計算並獲取玩家在當前場景的生成位置
    /// </summary>
    public Vector3 GetSpawnPosition(string sceneName)
    {
        Scene spawnScene = ResolveSpawnScene(sceneName);

        // 1) 若有指定入口 SpawnId，優先用「該場景內」的 SpawnPoint（避免 DDOL / 他場景殘留物件先被掃到）
        string pendingSpawnId = SceneSpawnRouter.ConsumePendingSpawnId();
        if (!string.IsNullOrEmpty(pendingSpawnId))
        {
            // 舊版 "Default" 依場景類型對應到 LandDefault／SeaDefault
            if (string.Equals(pendingSpawnId, SceneSpawnDefaults.LegacyDefaultSpawnId, System.StringComparison.OrdinalIgnoreCase))
                pendingSpawnId = SceneSpawnDefaults.GetDefaultSpawnIdForScene(sceneName);

            var points = Object.FindObjectsOfType<SpawnPoint>(true);
            foreach (var p in points)
            {
                if (!SpawnPointBelongsToScene(p, spawnScene))
                    continue;
                if (string.Equals(p.spawnId, pendingSpawnId, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[PlayerPositionManager] GetSpawnPosition - 使用 SpawnPoint：{pendingSpawnId} @ {p.transform.position}（場景：{spawnScene.name}）");
                    return p.transform.position;
                }
            }
            Debug.LogWarning($"[PlayerPositionManager] 在場景「{spawnScene.name}」找不到 SpawnId={pendingSpawnId} 的 SpawnPoint，改用其他策略。");
        }

        // 2) 若沒有指定入口，使用陸地 LandDefault／海洋 SeaDefault（含舊版 Default 相容）
        {
            var defaultCandidates = SceneSpawnDefaults.GetDefaultSpawnIdCandidates(sceneName);
            var points = Object.FindObjectsOfType<SpawnPoint>(true);
            foreach (var candidateId in defaultCandidates)
            {
                foreach (var p in points)
                {
                    if (!SpawnPointBelongsToScene(p, spawnScene))
                        continue;
                    if (string.Equals(p.spawnId, candidateId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"[PlayerPositionManager] GetSpawnPosition - 使用預設 SpawnPoint「{candidateId}」@ {p.transform.position}（場景：{spawnScene.name}）");
                        return p.transform.position;
                    }
                }
            }

            foreach (var p in points)
            {
                if (!SpawnPointBelongsToScene(p, spawnScene))
                    continue;
                bool legacyUnnamedDefault = string.IsNullOrWhiteSpace(p.spawnId) &&
                    p.gameObject.name.IndexOf("default", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (legacyUnnamedDefault)
                {
                    Debug.Log($"[PlayerPositionManager] GetSpawnPosition - 使用未命名 Default SpawnPoint @ {p.transform.position}（場景：{spawnScene.name}）");
                    return p.transform.position;
                }
            }
        }

        SyncPreviousSceneName();

        Vector3 targetPosition = Vector3.zero;
        bool usePortalPosition = false;
        
        // 優先檢查是否有從上一場景傳送來的記錄（傳送門位置）
        if (!string.IsNullOrEmpty(previousSceneName))
        {
            // RecordPortalPosition 的 key = "來源場景->目標場景"
            // 進入 sceneName 時，若上一場景為 previousSceneName，應查 "previous->current"
            string forwardKey = $"{previousSceneName}->{sceneName}";

            if (portalPositions.ContainsKey(forwardKey))
            {
                targetPosition = portalPositions[forwardKey];
                usePortalPosition = true;
                Debug.Log($"[PlayerPositionManager] GetSpawnPosition - 使用傳送門進入位置：{targetPosition} (傳送關係：{forwardKey})");
            }
            else
            {
                Debug.Log($"[PlayerPositionManager] GetSpawnPosition - 找不到傳送關係：{forwardKey}，改用其他策略。");
            }
        }
        
        // 如果沒有傳送門位置，則使用保存的一般位置
        if (!usePortalPosition && scenePositions.ContainsKey(sceneName))
        {
            targetPosition = scenePositions[sceneName];
            Debug.Log($"[PlayerPositionManager] GetSpawnPosition - 使用保存的位置：{targetPosition}");
        }
        else if (!usePortalPosition)
        {
            Debug.Log($"[PlayerPositionManager] GetSpawnPosition - 使用預設位置 (0,0,0)");
        }

        return targetPosition;
    }

    private System.Collections.IEnumerator RestorePlayerState(string sceneName)
    {
        yield return null; // 等待一幀
        
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning($"[PlayerPositionManager] 未找到玩家物件 (場景：{sceneName})");
            yield break;
        }
        
        // 恢復水下狀態（如果適用）
        if (sceneUnderwaterStates.ContainsKey(sceneName))
        {
            bool wasUnderwater = sceneUnderwaterStates[sceneName];
            // 這裡可以添加恢復水下狀態的邏輯
            Debug.Log($"[PlayerPositionManager] 恢復水下狀態：{wasUnderwater} (場景：{sceneName})");
        }
        
        // 清除上一場景記錄
        if (!string.IsNullOrEmpty(previousSceneName))
        {
            Debug.Log($"[PlayerPositionManager] 清除上一場景記錄：{previousSceneName}");
            previousSceneName = "";
        }
    }

    // 保存當前場景的玩家位置
    public void SaveCurrentPosition()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        GameObject player = GameObject.FindWithTag("Player");
        
        if (player != null)
        {
            Vector3 position = player.transform.position;
            scenePositions[currentScene] = position;
            
            // 保存水下狀態（如果玩家有氧氣管理器）
            bool isUnderwater = false;
            var oxygenManager = player.GetComponent<OxygenManager>();
            if (oxygenManager != null)
            {
                // 使用反射來獲取私有變數 isUnderwater
                var underwaterField = typeof(OxygenManager).GetField("isUnderwater", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (underwaterField != null)
                {
                    isUnderwater = (bool)underwaterField.GetValue(oxygenManager);
                }
            }
            sceneUnderwaterStates[currentScene] = isUnderwater;
            
            Debug.Log($"[PlayerPositionManager] 保存位置：{position} (場景：{currentScene})");
        }
    }

    // 清除指定場景的位置記錄
    public void ClearScenePosition(string sceneName)
    {
        if (scenePositions.ContainsKey(sceneName))
        {
            scenePositions.Remove(sceneName);
            Debug.Log($"[PlayerPositionManager] 清除場景位置記錄：{sceneName}");
        }
        
        if (sceneUnderwaterStates.ContainsKey(sceneName))
        {
            sceneUnderwaterStates.Remove(sceneName);
        }
    }

    // 清除所有位置記錄
    public void ClearAllPositions()
    {
        scenePositions.Clear();
        sceneUnderwaterStates.Clear();
        Debug.Log("[PlayerPositionManager] 清除所有位置記錄");
    }

    // 獲取指定場景的保存位置
    public Vector3 GetScenePosition(string sceneName)
    {
        return scenePositions.ContainsKey(sceneName) ? scenePositions[sceneName] : Vector3.zero;
    }

    // 檢查是否有指定場景的位置記錄
    public bool HasScenePosition(string sceneName)
    {
        return scenePositions.ContainsKey(sceneName);
    }
    
    // 記錄傳送門位置（從來源場景傳送到目標場景時使用）
    // fromScene: 來源場景名稱
    // toScene: 目標場景名稱
    // portalPosition: 傳送門在來源場景的位置
    public void RecordPortalPosition(string fromScene, string toScene, Vector3 portalPosition)
    {
        string key = $"{fromScene}->{toScene}";
        portalPositions[key] = portalPosition;
        previousSceneName = fromScene; // 記錄上一場景
        Debug.Log($"[PlayerPositionManager] 記錄傳送門位置：{portalPosition} (傳送關係：{key})");
    }
    
    // 設置上一場景名稱（用於手動設置）
    public void SetPreviousScene(string sceneName)
    {
        previousSceneName = sceneName;
        Debug.Log($"[PlayerPositionManager] 設置上一場景：{sceneName}");
    }
    
    // 清除傳送門記錄（可選，用於重置）
    public void ClearPortalPositions()
    {
        portalPositions.Clear();
        previousSceneName = "";
        Debug.Log("[PlayerPositionManager] 清除所有傳送門位置記錄");
    }
}
