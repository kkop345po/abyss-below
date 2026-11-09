using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePlayerManager : MonoBehaviour
{
    public GameObject landPlayerPrefab;
    public GameObject waterPlayerPrefab;
    private static bool isFirstLoad = true;

    private void Start()
    {
        if (isFirstLoad)
        {
            // 只在第一次載入時重置資料
            SaveSystem.LoadPlayerData();
            
            // 檢查是否需要初始化基本資料
            if (PlayerPrefs.GetInt("HasInitialized", 0) == 0)
            {
                Debug.Log("初始化基本資料...");
                InitializeBaseData();
            }
            
            isFirstLoad = false;
        }

        string sceneName = SceneManager.GetActiveScene().name;

        // 查找並銷毀所有現有的玩家（包括DontDestroyOnLoad的）
        // 使用 FindGameObjectsWithTag 找到所有玩家，避免遺漏
        GameObject[] existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        if (existingPlayers != null && existingPlayers.Length > 0)
        {
            Debug.Log($"[ScenePlayerManager] 發現 {existingPlayers.Length} 個現有玩家，正在銷毀...");
            foreach (GameObject player in existingPlayers)
            {
                if (player == null) continue; // 避免父物件被銷毀後子物件也跟著銷毀導致的空指標錯誤
                Debug.Log($"[ScenePlayerManager] 銷毀玩家：{player.name}");
                DestroyImmediate(player); // 立即銷毀，避免延遲導致重複創建
            }
        }

        // 再次確認沒有玩家存在
        GameObject[] remainingPlayers = GameObject.FindGameObjectsWithTag("Player");
        if (remainingPlayers != null && remainingPlayers.Length > 0)
        {
            Debug.LogWarning($"[ScenePlayerManager] 警告：仍有 {remainingPlayers.Length} 個玩家存在，強制銷毀");
            foreach (GameObject player in remainingPlayers)
            {
                if (player == null) continue;
                DestroyImmediate(player);
            }
        }

        GameObject spawnedPlayer = null;
        
        // 計算生成位置（避免閃現）
        Vector3 spawnPos = Vector3.zero;
        if (PlayerPositionManager.Instance != null)
        {
            spawnPos = PlayerPositionManager.Instance.GetSpawnPosition(sceneName);
            Debug.Log($"[ScenePlayerManager] 取得生成位置：{spawnPos}");
        }

        if (sceneName.ToLower() == "land" || sceneName.ToLower() == "harbor" || sceneName.ToLower() == "mainmenu" || sceneName.ToLower() == "loading_scene")
        {
            if (landPlayerPrefab != null)
                spawnedPlayer = Instantiate(landPlayerPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // 所有其餘的深海/洞穴場景，若無特別指定一律使用水下角色
            if (waterPlayerPrefab != null)
                spawnedPlayer = Instantiate(waterPlayerPrefab, spawnPos, Quaternion.identity);
            else
                Debug.LogError("[ScenePlayerManager] 嚴重錯誤：WaterPlayerPrefab 未指派！");
        }

        if (spawnedPlayer != null)
        {
            CameraFollowClamped camFollow = Camera.main.GetComponent<CameraFollowClamped>();
            if (camFollow != null)
            {
                camFollow.player = spawnedPlayer.transform;
                // 立即更新相機位置，避免相機滑動
                camFollow.transform.position = new Vector3(spawnPos.x, spawnPos.y, camFollow.transform.position.z);
            }
            
            Debug.Log($"[ScenePlayerManager] 玩家已生成於 {spawnPos} (場景：{sceneName})");
            PlayerStateManager.Instance?.RequestResyncPlayerVitals();
#if UNITY_EDITOR
            SimpleCheatManager.Instance?.RefreshPlayerCache();
#endif
        }
    }

    public void InitializeBaseData()
    {
        // 新遊戲初始化：明確回到 Lv.0，並清掉持久化血/氧/體力，讓玩家以「滿狀態」重生。
        // 金幣不另發放：新玩家維持 0（PlayerDataManager.Coins 預設）
        PlayerDataManager.SpeedLevel = 0;
        PlayerDataManager.OxygenLevel = 0;
        PlayerDataManager.StaminaLevel = 0;
        PlayerDataManager.WeaponUpgradeLevel = 0;
        PlayerDataManager.ClearPersistedVitalsForFreshSpawn();

        PlayerPrefs.SetInt("HasInitialized", 1);
        SaveSystem.SavePlayerData();
        Debug.Log("基本資料初始化完成");
    }
}
