using UnityEngine;
using TMPro;

public class QuestObjectiveUI : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public Color completedColor = Color.green;
    public Color inProgressColor = Color.white;

    public void UpdateInfo(QuestObjective obj, int current)
    {
        if (statusText == null) return;

        bool isComplete = current >= obj.RequiredAmount;
        statusText.color = isComplete ? completedColor : inProgressColor;

        // 例如: "收集海膽 (1/3)"
        // 或者使用 obj.Description 如果有的話
        string baseText = string.IsNullOrEmpty(obj.Description) ? obj.TargetKey : obj.Description;
        
        // 限制 current 不超過 required
        int displayCurrent = Mathf.Min(current, obj.RequiredAmount);
        
        statusText.text = $"{baseText} ({displayCurrent}/{obj.RequiredAmount})";
        
        if (isComplete)
        {
            statusText.fontStyle = FontStyles.Strikethrough;
        }
        else
        {
            statusText.fontStyle = FontStyles.Normal;
        }
    }
}
