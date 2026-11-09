using UnityEngine;

/// <summary>
/// 跨場景的一次性生成點路由。
/// 由傳送門/回程門在載入前設定，ScenePlayerManager 生成玩家時讀取並清除。
/// </summary>
public static class SceneSpawnRouter
{
    private const string PendingSpawnIdKey = "PendingSpawnId";

    /// <summary>設定下一個場景要使用的 SpawnId（一次性）。</summary>
    public static void SetPendingSpawnId(string spawnId)
    {
        if (string.IsNullOrWhiteSpace(spawnId))
        {
            PlayerPrefs.DeleteKey(PendingSpawnIdKey);
            return;
        }

        PlayerPrefs.SetString(PendingSpawnIdKey, spawnId.Trim());
        PlayerPrefs.Save();
    }

    /// <summary>讀取並清除 PendingSpawnId。</summary>
    public static string ConsumePendingSpawnId()
    {
        string id = PlayerPrefs.GetString(PendingSpawnIdKey, "");
        PlayerPrefs.DeleteKey(PendingSpawnIdKey);
        PlayerPrefs.Save();
        return id;
    }
}

