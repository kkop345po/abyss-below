using UnityEngine;

public enum QuestObjectiveType
{
    CollectItem,
    TalkToNPC,
    KillEnemy,
    ReachLocation
}

[System.Serializable]
public class QuestObjective
{
    public QuestObjectiveType Type;
    
    [Tooltip("對應 ItemID, NPCID, 或 EnemyID")]
    public string TargetKey;
    
    public int RequiredAmount = 1;
    
    public string Description;
}
