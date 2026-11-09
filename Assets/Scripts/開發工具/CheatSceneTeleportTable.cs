using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>僅供編輯器作弊工具使用的場景存在檢查與說明。</summary>
public static class CheatSceneTeleportTable
{
#if UNITY_EDITOR
    public static bool IsInBuildSettings(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return false;
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }

    public static string BuildCheatHelpText()
    {
        var lines = new System.Text.StringBuilder();
        lines.AppendLine("【場景傳送已移除】");

        lines.AppendLine("  F12: 開關作弊面板（CheatManager）");
        lines.AppendLine("【功能鍵】僅限 Unity 編輯器");
        lines.AppendLine("  F2 無敵+無限氧+無限體力");
        lines.AppendLine("  F3 回滿  F4 +1000金+素材");
        lines.AppendLine("  F7 向前傳送  F8 滑鼠傳送");
        lines.AppendLine("  F9 重置Boss+寶箱  F10 清檔重開");
        return lines.ToString();
    }
#endif
}
