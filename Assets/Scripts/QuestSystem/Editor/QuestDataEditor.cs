using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(QuestData))]
public class QuestDataEditor : Editor
{
    private ItemDatabaseSO itemDatabase;
    private QuestItemDatabaseSO questItemDatabase;

    private void OnEnable()
    {
        // 1. Try to find the ItemDatabase automatically
        string[] guids = AssetDatabase.FindAssets("t:ItemDatabaseSO");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabaseSO>(path);
        }

        // 2. Try to find the QuestItemDatabase automatically
        string[] questGuids = AssetDatabase.FindAssets("t:QuestItemDatabaseSO");
        if (questGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(questGuids[0]);
            questItemDatabase = AssetDatabase.LoadAssetAtPath<QuestItemDatabaseSO>(path);
        }
    }

    public override void OnInspectorGUI()
    {
        // Draw default inspector first
        base.OnInspectorGUI();

        QuestData quest = (QuestData)target;

        if (itemDatabase == null && questItemDatabase == null)
        {
            EditorGUILayout.HelpBox("Cannot find any ItemDatabaseSO or QuestItemDatabaseSO! Please check your project.", MessageType.Warning);
            return;
        }

        if (quest.Objectives != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Validation Status", EditorStyles.boldLabel);

            foreach (var obj in quest.Objectives)
            {
                if (obj.Type == QuestObjectiveType.CollectItem)
                {
                    if (string.IsNullOrEmpty(obj.TargetKey))
                    {
                        EditorGUILayout.HelpBox($"Objective '{obj.Description}': Missing Target Key!", MessageType.Warning);
                    }
                    else
                    {
                        // 3. Check if TargetKey exists in ItemDatabase
                        bool exists = itemDatabase != null && itemDatabase.allItems.Any(x => x.itemId == obj.TargetKey);
                        
                        // Check QuestItemDatabase if not found yet
                        if (!exists && questItemDatabase != null)
                        {
                            exists = questItemDatabase.questItems.Any(x => x.itemId == obj.TargetKey);
                        }

                        // 4. Show Error if not found
                        if (!exists)
                        {
                            Color defaultColor = GUI.color;
                            GUI.color = Color.red; 
                            EditorGUILayout.HelpBox($"Item ID '{obj.TargetKey}' not found in Database!", MessageType.Error);
                            GUI.color = defaultColor;
                        }
                    }
                }
            }
        }
    }
}
