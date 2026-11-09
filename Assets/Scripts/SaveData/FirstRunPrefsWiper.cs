using UnityEngine;

/// <summary>
/// 輸出（Build）後第一次啟動：清除所有 PlayerPrefs/存檔資料（只清一次）。
/// - 不在 Editor 執行，避免開發時資料一直被清。
/// - 以 Application.version 做「每個版本第一次啟動」的判定鍵。
/// </summary>
public static class FirstRunPrefsWiper
{
    private const string WipedVersionKey = "FirstRunWipe_DoneForVersion";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void WipeOnFirstRunOfBuild()
    {
#if UNITY_EDITOR
        return;
#else
        string currentVersion = string.IsNullOrWhiteSpace(Application.version) ? "0" : Application.version;
        string wipedFor = PlayerPrefs.GetString(WipedVersionKey, "");

        if (wipedFor == currentVersion)
            return;

        // 清除所有資料（含存檔檔案、buff、成就快取、背包等）
        PlayerDataManager.ResetData();

        // ResetData 會 DeleteAll，因此必須在之後再寫入旗標
        PlayerPrefs.SetString(WipedVersionKey, currentVersion);
        PlayerPrefs.Save();
#endif
    }
}

