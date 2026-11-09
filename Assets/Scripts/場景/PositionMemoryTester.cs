using UnityEngine;

public class PositionMemoryTester : MonoBehaviour
{
#if UNITY_EDITOR

    [Header("測試功能")]
    public bool enableDebugLogs = true;
    
    [Header("手動測試")]
    public KeyCode savePositionKey = KeyCode.F1;
    public KeyCode clearAllPositionsKey = KeyCode.F2;
    public KeyCode showAllPositionsKey = KeyCode.F3;

    void Update()
    {
        if (!enableDebugLogs) return;

        // 手動保存當前位置
        if (Input.GetKeyDown(savePositionKey))
        {
            if (PlayerPositionManager.Instance != null)
            {
                PlayerPositionManager.Instance.SaveCurrentPosition();
                Debug.Log("[測試] 手動保存當前位置");
            }
        }

        // 清除所有位置記錄
        if (Input.GetKeyDown(clearAllPositionsKey))
        {
            if (PlayerPositionManager.Instance != null)
            {
                PlayerPositionManager.Instance.ClearAllPositions();
                Debug.Log("[測試] 清除所有位置記錄");
            }
        }

        // 顯示所有保存的位置
        if (Input.GetKeyDown(showAllPositionsKey))
        {
            ShowAllSavedPositions();
        }
    }

    private void ShowAllSavedPositions()
    {
        if (PlayerPositionManager.Instance == null) return;

        Debug.Log("=== 所有保存的位置 ===");
        string[] scenes = { "Land", "Shallow", "Mid", "Deep", "Sunken ship", "Cave", "1Fhole", "2Fhole", "3Fhole" };
        
        foreach (string scene in scenes)
        {
            if (PlayerPositionManager.Instance.HasScenePosition(scene))
            {
                Vector3 position = PlayerPositionManager.Instance.GetScenePosition(scene);
                Debug.Log($"場景 {scene}: {position}");
            }
            else
            {
                Debug.Log($"場景 {scene}: 無保存位置");
            }
        }
        Debug.Log("=====================");
    }

    void OnGUI()
    {
        if (!enableDebugLogs) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("位置記憶測試工具");
        GUILayout.Label($"F1: 保存當前位置");
        GUILayout.Label($"F2: 清除所有位置");
        GUILayout.Label($"F3: 顯示所有位置");
        
        if (PlayerPositionManager.Instance != null)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (PlayerPositionManager.Instance.HasScenePosition(currentScene))
            {
                Vector3 savedPos = PlayerPositionManager.Instance.GetScenePosition(currentScene);
                GUILayout.Label($"當前場景 {currentScene} 有保存位置: {savedPos}");
            }
            else
            {
                GUILayout.Label($"當前場景 {currentScene} 無保存位置");
            }
        }
        GUILayout.EndArea();
    }

#else
    // 輸出版本僅保留空殼，讓既有場景引用有效；不編譯任何測試輸入或作弊邏輯。
    private void Awake() { enabled = false; }
#endif
}
