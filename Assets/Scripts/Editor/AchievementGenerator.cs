using UnityEngine;
using UnityEditor;
using System.IO;

public class AchievementGenerator : EditorWindow
{
    [MenuItem("Achievement/Generate Default Achievements")]
    public static void GenerateDefaults()
    {
        string path = "Assets/Data/Achievements";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        CreateAchievement(path, "ACH_01", "初次下潛", "開始你的深海冒險", "GameStart", 1);
        CreateAchievement(path, "ACH_02", "深海清道夫", "擊敗 10 隻厚力蟹 (HouliCrab)", "Kill_HouliCrab", 10);
        CreateAchievement(path, "ACH_03", "生魚片大師", "擊敗 10 隻劍魚 (Swordfish)", "Kill_Swordfish", 10);
        CreateAchievement(path, "ACH_04", "海洋霸主", "擊敗 50 隻任意敵人", "TotalKills", 50);
        CreateAchievement(path, "ACH_05", "深淵觸手", "擊敗莫尼亞拉普", "Kill_BigOctopus", 1);
        CreateAchievement(path, "ACH_06", "深淵巨獸", "擊敗恩利克", "Kill_SmallBoss", 1);
        CreateAchievement(path, "ACH_07", "寶藏獵人", "累積獲得 500 金幣", "TotalGold", 500);
        CreateAchievement(path, "ACH_08", "氧氣充足", "將氧氣筒升至滿級 (Lv.5)", "Level_Oxygen", 5);
        CreateAchievement(path, "ACH_09", "馬拉松選手", "將體力升至滿級 (Lv.5)", "Level_Stamina", 5);
        CreateAchievement(path, "ACH_10", "迅捷如風", "將速度升至滿級 (Lv.5)", "Level_Speed", 5);
        CreateAchievement(path, "ACH_11", "九死一生", "在血量低於 10% 時存活並恢復", "Survivor", 1);
        CreateAchievement(path, "ACH_12", "全副武裝", "獲得所有種類的武器/工具", "Collection_AllWeapons", 1);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Default Achievements Generated in " + path);
    }

    private static void CreateAchievement(string path, string id, string title, string desc, string key, int value)
    {
        string assetPath = $"{path}/{id}.asset";
        
        // Check if exists
        AchievementData asset = AssetDatabase.LoadAssetAtPath<AchievementData>(assetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<AchievementData>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        asset.id = id;
        asset.title = title;
        asset.description = desc;
        asset.targetKey = key;
        asset.targetValue = value;
        
        EditorUtility.SetDirty(asset);
    }
}
