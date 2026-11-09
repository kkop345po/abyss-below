using UnityEngine;

/// <summary>
/// 陸地／海洋場景的預設出生點 SpawnId（與場景內 SpawnPoint 元件對齊）。
/// </summary>
public static class SceneSpawnDefaults
{
    public const string LandSpawnId = "LandDefault";
    public const string SeaSpawnId = "SeaDefault";

    /// <summary>舊版相容：部分海洋場景仍使用 Default。</summary>
    public const string LegacyDefaultSpawnId = "Default";

    public static bool IsLandScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return true;
        string n = sceneName.Trim().ToLowerInvariant();
        return n == "land" || n == "harbor" || n == "mainmenu" || n == "loading_scene" || n == "menu";
    }

    public static string GetDefaultSpawnIdForScene(string sceneName)
    {
        return IsLandScene(sceneName) ? LandSpawnId : SeaSpawnId;
    }

    /// <summary>依場景類型回傳可接受的預設 SpawnId 候選（優先順序由高到低）。</summary>
    public static string[] GetDefaultSpawnIdCandidates(string sceneName)
    {
        if (IsLandScene(sceneName))
            return new[] { LandSpawnId, LegacyDefaultSpawnId };
        return new[] { SeaSpawnId, LegacyDefaultSpawnId };
    }
}
