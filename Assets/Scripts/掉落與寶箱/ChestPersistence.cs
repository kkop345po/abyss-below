using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>寶箱是否已開過的跨場景紀錄（PlayerPrefs）。作弊「重置寶箱」會清掉此處登錄的鍵。</summary>
public static class ChestPersistence
{
    const string RegistryKey = "Chest_OpenedRegistry_v1";

    public static string BuildDefaultId(MonoBehaviour chest)
    {
        if (chest == null) return "unknown";
        var sc = chest.gameObject.scene.name;
        return $"{sc}_{chest.name}_{chest.transform.position.x:F1}_{chest.transform.position.y:F1}";
    }

    static string PrefsKey(string chestId) => "ChestOpened_v1_" + chestId;

    public static bool IsOpened(string chestId)
    {
        if (string.IsNullOrEmpty(chestId)) return false;
        return PlayerPrefs.GetInt(PrefsKey(chestId), 0) == 1;
    }

    public static void MarkOpened(string chestId)
    {
        if (string.IsNullOrEmpty(chestId)) return;
        PlayerPrefs.SetInt(PrefsKey(chestId), 1);
        var reg = PlayerPrefs.GetString(RegistryKey, "");
        string token = chestId + "|";
        if (reg.IndexOf(token, StringComparison.Ordinal) < 0)
        {
            PlayerPrefs.SetString(RegistryKey, reg + token);
        }
        PlayerPrefs.Save();
    }

    /// <summary>作弊用：清掉本專案登錄過的寶箱開啟狀態（需重新載入場景或重生寶箱物件才看得到）。</summary>
    public static void ClearAllOpenedForCheats()
    {
        string reg = PlayerPrefs.GetString(RegistryKey, "");
        if (!string.IsNullOrEmpty(reg))
        {
            var parts = reg.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var seen = new HashSet<string>();
            foreach (var id in parts)
            {
                if (string.IsNullOrEmpty(id) || seen.Contains(id)) continue;
                seen.Add(id);
                PlayerPrefs.DeleteKey(PrefsKey(id));
            }
        }
        PlayerPrefs.DeleteKey(RegistryKey);
        PlayerPrefs.Save();
    }
}
