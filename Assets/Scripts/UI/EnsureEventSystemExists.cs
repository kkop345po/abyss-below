using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Game.UI
{
    /// <summary>
    /// 保證場景中永遠存在可用的 EventSystem，避免某些海域/場景 UI 點擊失效。
    /// </summary>
    public class EnsureEventSystemExists : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            // 任何一個 EventSystem 都算（避免重複建立）
            if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
                return;

            var go = new GameObject("EventSystem (Auto)");
            go.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            // 新 Input System 專用 UI module
            go.AddComponent<InputSystemUIInputModule>();
#else
            // 舊 Input Manager UI module
            go.AddComponent<StandaloneInputModule>();
#endif

            Object.DontDestroyOnLoad(go);
            Debug.Log("[EnsureEventSystemExists] 已自動建立 EventSystem（避免 UI 點擊失效）");
        }
    }
}

