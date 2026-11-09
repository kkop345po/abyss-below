using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject questPanel; // 整個面板容器
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Transform objectivesContainer;
    
    [Header("Prefabs")]
    public GameObject objectivePrefab; // 必須掛有 QuestObjectiveUI

    private void Start()
    {
        // 訂閱事件
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted += HandleQuestAccepted;
            QuestManager.Instance.OnQuestUpdated += HandleQuestUpdated;
            QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
            
            // 如果已經有任務，初始化顯示
            if (QuestManager.Instance.currentQuest != null)
            {
                UpdateUI(QuestManager.Instance.currentQuest);
            }
            else
            {
                questPanel.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted -= HandleQuestAccepted;
            QuestManager.Instance.OnQuestUpdated -= HandleQuestUpdated;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }
    }

    private void HandleQuestAccepted(QuestData quest)
    {
        questPanel.SetActive(true);
        UpdateUI(quest);
    }

    private void HandleQuestUpdated()
    {
        if (QuestManager.Instance.currentQuest != null)
        {
            UpdateUI(QuestManager.Instance.currentQuest);
        }
    }

    private void HandleQuestCompleted(QuestData quest)
    {
        // 任務完成時可以顯示一個特效，這裡簡單隱藏面板或顯示完成
        // 如果有下一個任務，會自動接取並觸發 Accepted，所以面板會再次更新
        if (QuestManager.Instance.currentQuest == null)
        {
            questPanel.SetActive(false);
        }
    }

    private void UpdateUI(QuestData quest)
    {
        titleText.text = quest.Title;
        descriptionText.text = quest.Description;

        // 清空舊目標
        foreach (Transform child in objectivesContainer)
        {
            Destroy(child.gameObject);
        }

        // 生成新目標
        foreach (var obj in quest.Objectives)
        {
            GameObject go = Instantiate(objectivePrefab, objectivesContainer);
            QuestObjectiveUI ui = go.GetComponent<QuestObjectiveUI>();
            if (ui != null)
            {
                // 從 QuestManager 獲取當前進度
                int current = QuestManager.Instance.GetCurrentAmount(obj.TargetKey);
                ui.UpdateInfo(obj, current);
            }
        }
    }
}
