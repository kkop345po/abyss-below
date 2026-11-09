using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string firstGameSceneName = "Land";

    // 本次啟動僅在「第一次按開始」時清檔；避免遊玩中回到選單再按開始又被清空。
    private static bool s_clearedAllDataOnFirstStartThisSession = false;

    public void PlayGame()
    {
        Debug.Log("PlayGame triggered");

        if (!s_clearedAllDataOnFirstStartThisSession)
        {
            s_clearedAllDataOnFirstStartThisSession = true;
            PlayerDataManager.ResetData();
        }

        // 需求：新進遊戲（從選單開始玩）時，血量/氧氣/體力必定滿條。
        // 清掉持久化 vitals（CurrentHealth/Oxygen/Stamina），讓各 Manager 以 Max 初始化。
        PlayerDataManager.ClearPersistedVitalsForFreshSpawn();
        // 需求：輸出後開始新遊戲背包要清空
        InventoryManager.ResetInventoryForNewGame();
        // 新開始遊戲：強制從陸地預設出生點（LandDefault）生成
        SceneSpawnRouter.SetPendingSpawnId(SceneSpawnDefaults.LandSpawnId);
        var loader = FindObjectOfType<SceneLoader>();
        if (loader != null && !string.IsNullOrEmpty(firstGameSceneName))
            loader.LoadSceneWithLoading(firstGameSceneName);
        else if (SceneManager.sceneCountInBuildSettings > 1)
            SceneManager.LoadScene(1);
        else
            SceneManager.LoadScene(firstGameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
