using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quest System/Quest Data")]
public class QuestData : ScriptableObject
{
    public string ID;
    public string Title;
    [TextArea] public string Description;

    public List<QuestObjective> Objectives;

    public List<ItemData> RewardItems;
    public int RewardGold;
    public QuestData NextQuest;
    
    // Editor Validation Reference
    [HideInInspector] public ItemDatabaseSO validationDatabase;
}
