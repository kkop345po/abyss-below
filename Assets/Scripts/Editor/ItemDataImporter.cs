using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[InitializeOnLoad]
public class ItemDataImporter : EditorWindow
{
    static ItemDataImporter()
    {
        // 延遲執行，確保 AssetDatabase 已準備好
        EditorApplication.delayCall += AutoImportOnLoad;
    }

    private static void AutoImportOnLoad()
    {
        string key = "ItemDataImporter_HasRun_v1";
        if (!EditorPrefs.GetBool(key, false))
        {
            DoAutoImport();
            EditorPrefs.SetBool(key, true);
        }
    }

    [MenuItem("Tools/Import Item Data")]
    public static void ShowWindow()
    {
        GetWindow<ItemDataImporter>("Item Importer");
    }

    private ItemDatabaseSO targetDatabase;

    private void OnGUI()
    {
        GUILayout.Label("Import Existing Data to ItemDatabase", EditorStyles.boldLabel);

        targetDatabase = (ItemDatabaseSO)EditorGUILayout.ObjectField("Target Database", targetDatabase, typeof(ItemDatabaseSO), false);

        if (GUILayout.Button("Import All Data"))
        {
            if (targetDatabase == null)
            {
                Debug.LogError("Please assign a Target Database!");
                return;
            }
            ImportData(targetDatabase);
        }
    }

    private static void DoAutoImport()
    {
        // 1. 尋找或建立 ItemDatabase
        string path = "Assets/Resources/ItemDatabase.asset";
        ItemDatabaseSO db = AssetDatabase.LoadAssetAtPath<ItemDatabaseSO>(path);
        
        if (db == null)
        {
            if (!Directory.Exists("Assets/Resources"))
                Directory.CreateDirectory("Assets/Resources");
                
            db = ScriptableObject.CreateInstance<ItemDatabaseSO>();
            AssetDatabase.CreateAsset(db, path);
            Debug.Log("[ItemDataImporter] 自動建立資料庫於: " + path);
        }

        // 2. 執行匯入
        ImportData(db);
        
        // 3. 自動選取該檔案，讓使用者知道發生了什麼
        Selection.activeObject = db;
        EditorGUIUtility.PingObject(db);
    }

    private static void ImportData(ItemDatabaseSO db)
    {
        int count = 0;
        if (db.allItems == null) db.allItems = new List<ItemData>();
        
        // 1. Import ShopItemData
        string[] shopGuids = AssetDatabase.FindAssets("t:ShopItemData");
        foreach (string guid in shopGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ShopItemData shopItem = AssetDatabase.LoadAssetAtPath<ShopItemData>(assetPath);
            if (shopItem != null)
            {
                if (AddToDatabase(db, shopItem)) count++;
            }
        }

        // 2. Import MaterialData
        string[] matGuids = AssetDatabase.FindAssets("t:MaterialData");
        foreach (string guid in matGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            MaterialData matItem = AssetDatabase.LoadAssetAtPath<MaterialData>(assetPath);
            if (matItem != null)
            {
                if (AddToDatabase(db, matItem)) count++;
            }
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"[ItemDataImporter] 自動匯入完成！共更新/新增 {count} 筆資料。");
    }

    private static bool AddToDatabase(ItemDatabaseSO db, ShopItemData source)
    {
        if (string.IsNullOrEmpty(source.itemId)) return false;

        ItemData existing = db.GetItem(source.itemId);
        if (existing == null)
        {
            existing = new ItemData();
            db.allItems.Add(existing);
        }

        existing.itemId = source.itemId;
        existing.itemName = source.itemName;
        existing.icon = source.icon;
        existing.description = source.description;
        
        // Map Type
        if (source.type == ShopItemType.Weapon) existing.itemType = ItemType.Weapon;
        else if (source.type == ShopItemType.Item) existing.itemType = ItemType.Consumable;
        else existing.itemType = ItemType.Material;

        return true;
    }

    private static bool AddToDatabase(ItemDatabaseSO db, MaterialData source)
    {
        if (string.IsNullOrEmpty(source.materialId)) return false;

        ItemData existing = db.GetItem(source.materialId);
        if (existing == null)
        {
            existing = new ItemData();
            db.allItems.Add(existing);
        }

        existing.itemId = source.materialId;
        existing.itemName = source.materialName;
        existing.icon = source.icon;
        existing.description = source.description;
        existing.itemType = ItemType.Material;

        return true;
    }
}
