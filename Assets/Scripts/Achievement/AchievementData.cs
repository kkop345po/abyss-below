using UnityEngine;

[CreateAssetMenu(fileName = "New Achievement", menuName = "Achievement/Achievement Data")]
public class AchievementData : ScriptableObject
{
    [Header("基本資訊")]
    public string id; // 例如: ACH_01
    public string title; // 顯示名稱
    [TextArea] public string description; // 描述
    public Sprite icon; // 圖示

    [Header("目標設定")]
    public string targetKey; // 對應的追蹤鍵值 (如 "Kill_HouliCrab")
    public int targetValue; // 達成目標值 (如 10)
    
    [Header("顯示設定")]
    public bool isHidden; // 是否為隱藏成就 (達成前顯示為 ???)
}
