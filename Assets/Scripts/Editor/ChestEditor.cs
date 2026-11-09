#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Chest))]
public class ChestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        var chest = (Chest)target;

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("從「任務道具庫」填入全部戰利品（7 項）"))
            {
                string[] guids = AssetDatabase.FindAssets("t:QuestItemDatabaseSO");
                if (guids == null || guids.Length == 0)
                {
                    EditorUtility.DisplayDialog("Chest", "專案中找不到 QuestItemDatabaseSO（任務道具庫）。", "OK");
                    return;
                }

                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var db = AssetDatabase.LoadAssetAtPath<QuestItemDatabaseSO>(path);
                if (db == null || db.questItems == null || db.questItems.Count == 0)
                {
                    EditorUtility.DisplayDialog("Chest", "任務道具庫是空的或無法讀取。", "OK");
                    return;
                }

                Undo.RecordObject(chest, "Fill Chest Loot From Quest Item Database");
                chest.lootItems.Clear();
                foreach (var q in db.questItems)
                {
                    if (q == null || string.IsNullOrWhiteSpace(q.itemId)) continue;
                    chest.lootItems.Add(new Chest.LootItem
                    {
                        itemId = q.itemId.Trim(),
                        amount = 1,
                        shopItemData = null,
                        itemTypeOverride = q.itemType
                    });
                }

                EditorUtility.SetDirty(chest);
                Debug.Log($"[ChestEditor] 已從 {path} 填入 {chest.lootItems.Count} 筆戰利品。");
            }
        }

        if (Application.isPlaying)
            EditorGUILayout.HelpBox("請在編輯模式下使用「填入戰利品」按鈕。", MessageType.Warning);

        EditorGUILayout.HelpBox(
            "按鈕會依《任務道具庫》(QuestItemDatabase) 的順序填入 itemId。\n已將 7 項寫入 Resources/ItemDatabase；Generic Pickup 預製仍須在寶箱上指定（可用同一個帶 GenericPickup 的預製）。",
            MessageType.Info);
    }
}
#endif
