using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHistoryManager : MonoBehaviour
{
    public static SceneHistoryManager Instance;

    /// <summary>
    /// 若場景內未放置管理器，於執行期建立 DontDestroyOnLoad 實例（避免 GoBackPortal 等因 Instance 為 null 無法返回）。
    /// </summary>
    public static SceneHistoryManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        SceneHistoryManager found = Object.FindObjectOfType<SceneHistoryManager>(true);
        if (found != null)
        {
            if (!found.gameObject.activeInHierarchy)
                found.gameObject.SetActive(true);
            if (Instance == null)
            {
                Instance = found;
                if (found.gameObject.scene.IsValid() && found.gameObject.scene.buildIndex >= 0)
                    DontDestroyOnLoad(found.gameObject);
            }
            return Instance;
        }

        GameObject go = new GameObject("SceneHistoryManager");
        return go.AddComponent<SceneHistoryManager>();
    }

    private string previousScene = "";
    private bool isReturning = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[SceneHistory] 管理器初始化");
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
        Debug.Log($"[SceneHistory] 載入場景：{scene.name}，上一個場景：{previousScene}，是否正在返回：{isReturning}");
    }

    public void LoadScene(string sceneName)
    {
        if (sceneName != "LoadingScene")
        {
            previousScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[SceneHistory] LoadScene - 設置上一個場景為：{previousScene}，準備載入：{sceneName}");

            // 保存玩家狀態（血量、氧氣、體力）
            if (PlayerStateManager.Instance != null)
            {
                PlayerStateManager.Instance.OnSceneTransition();
                Debug.Log("[SceneHistory] 保存玩家狀態");
            }
        }
        SceneManager.LoadScene(sceneName);
    }

    public void GoBack()
    {
        Debug.Log($"[SceneHistory] 嘗試返回 - 上一個場景：{previousScene}，是否正在返回：{isReturning}");

        if (!string.IsNullOrEmpty(previousScene) && !isReturning)
        {
            isReturning = true;
            string sceneToLoad = previousScene;
            previousScene = "";

            Debug.Log($"[SceneHistory] 開始返回到場景：{sceneToLoad}");

            // 保存玩家狀態（血量、氧氣、體力）
            if (PlayerStateManager.Instance != null)
            {
                PlayerStateManager.Instance.OnSceneTransition();
                Debug.Log("[SceneHistory] 保存玩家狀態");
            }

            var sceneLoader = FindObjectOfType<SceneLoader>();
            if (sceneLoader != null)
            {
                Debug.Log("[SceneHistory] 使用 SceneLoader 直接載入場景");
                sceneLoader.LoadSceneDirectly(sceneToLoad);
            }
            else
            {
                Debug.Log("[SceneHistory] 直接使用 SceneManager 載入場景");
                SceneManager.LoadScene(sceneToLoad);
            }

            isReturning = false;
        }
        else
        {
            Debug.LogWarning($"[SceneHistory] 無法返回 - 上一個場景是空的或正在返回中");
        }
    }

    /// <summary>讀取上一個場景名稱（不清空）。若無則回 false。</summary>
    public bool TryPeekPreviousScene(out string sceneName)
    {
        sceneName = previousScene;
        return !string.IsNullOrEmpty(previousScene) && !isReturning;
    }

    /// <summary>取出上一個場景名稱並清空記錄（等同 GoBack 的第一步）。若無則回 false。</summary>
    public bool TryConsumePreviousScene(out string sceneName)
    {
        sceneName = null;
        if (string.IsNullOrEmpty(previousScene) || isReturning)
            return false;

        sceneName = previousScene;
        previousScene = "";
        return true;
    }

    public void SetPreviousScene(string sceneName)
    {
        if (sceneName != "LoadingScene" && !isReturning)
        {
            Debug.Log($"[SceneHistory] 設置上一個場景：{sceneName}");
            previousScene = sceneName;
        }
        else
        {
            Debug.Log($"[SceneHistory] 忽略設置上一個場景：{sceneName}（LoadingScene 或正在返回中）");
        }
    }
}