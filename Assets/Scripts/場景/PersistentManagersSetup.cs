using UnityEngine;

/// <summary>
/// 確保 EventSystem 在場景切換時不被銷毀（如果 UI 是全局的）
/// 將此腳本掛在 EventSystem 物件上
/// </summary>
public class PersistentManagersSetup : MonoBehaviour
{
    private void Awake()
    {
        // 確保 EventSystem 持久化（如果 UI 是全局的）
        if (gameObject.name == "EventSystem" || GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PersistentManagersSetup] EventSystem 設置為持久化");
        }
    }
}

