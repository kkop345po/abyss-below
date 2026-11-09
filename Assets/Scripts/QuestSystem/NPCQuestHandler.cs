using UnityEngine;

public class NPCQuestHandler : MonoBehaviour
{
    [Header("NPC 設定")]
    [Tooltip("需與 QuestObjective 中的 TalkToNPC TargetKey 一致")]
    public string npcId = "NPC_BaseCamp";

    [Header("任務 ID（必須與各 QuestData 資產上的 ID 欄位完全一致）")]
    [Tooltip("若對話永遠卡在「一開始」，請檢查 Quest_01 的 ID 是否與此字串相同")]
    public string quest01Id = "Quest_01";
    public string quest02Id = "Quest_02";
    public string quest03Id = "Quest_03";

    [Header("對話資料設定")]
    public DialogueData dialogueQuest1Start;
    public DialogueData dialogueQuest1End_2Start;
    public DialogueData dialogueQuest2End_3Start;
    public DialogueData dialogueQuest3End;
    
    [Header("預設日常對話 (任務解完/尚未接取時)")]
    public DialogueData defaultDialogue;

    /// <summary>
    /// 當玩家與這個 NPC 互動時呼叫
    /// </summary>
    public void InteractNPC(System.Action onComplete = null)
    {
        var manager = QuestManager.Instance;
        // 需包含未啟用物件：對話面板常預設關閉，否則找不到 DialogueManager
        var dialogueManagers = Object.FindObjectsByType<DialogueManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var dialogueManager = dialogueManagers != null && dialogueManagers.Length > 0 ? dialogueManagers[0] : null;

        if (dialogueManager == null)
        {
            Debug.LogWarning("[NPCQuestHandler] 找不到 DialogueManager！");
            onComplete?.Invoke();
            return;
        }

        if (manager == null || manager.currentQuest == null)
        {
            // 如果目前沒有任務，播放預設對話
            if (defaultDialogue != null) dialogueManager.StartDialogue(defaultDialogue, onComplete);
            else onComplete?.Invoke();
            return;
        }

        // 對話前強制與背包對齊，避免進度字典落後
        manager.SyncCollectObjectivesFromInventory();

        string questId = manager.currentQuest.ID;
        if (string.IsNullOrEmpty(questId))
            Debug.LogWarning($"[NPCQuestHandler] 當前任務 {manager.currentQuest.name} 的 ID 為空，無法對應階段對話。請在 QuestData 填寫 ID（例如 {quest01Id}）。");

        // 是否已達成任務卡上所有「收集」條件（以背包為準，與 QuestObjective.TargetKey 一致）
        bool collectDone = manager.AreAllCollectObjectivesMetByInventory();

        // ======== 第一層任務 ========
        if (questId == quest01Id)
        {
            if (collectDone)
            {
                dialogueManager.StartDialogue(dialogueQuest1End_2Start, () => {
                    manager.UpdateObjectiveProgress(npcId, 1);
                    onComplete?.Invoke();
                });
            }
            else
            {
                if (dialogueQuest1Start != null) dialogueManager.StartDialogue(dialogueQuest1Start, onComplete);
                else onComplete?.Invoke();
            }
        }
        // ======== 第二層任務 ========
        else if (questId == quest02Id)
        {
            if (collectDone)
            {
                dialogueManager.StartDialogue(dialogueQuest2End_3Start, () => {
                    manager.UpdateObjectiveProgress(npcId, 1);
                    onComplete?.Invoke();
                });
            }
            else
            {
                if (defaultDialogue != null) dialogueManager.StartDialogue(defaultDialogue, onComplete);
                else onComplete?.Invoke();
            }
        }
        // ======== 第三層任務 ========
        else if (questId == quest03Id)
        {
            if (collectDone)
            {
                dialogueManager.StartDialogue(dialogueQuest3End, () => {
                    manager.UpdateObjectiveProgress(npcId, 1);
                    onComplete?.Invoke();
                });
            }
            else
            {
                if (defaultDialogue != null) dialogueManager.StartDialogue(defaultDialogue, onComplete);
                else onComplete?.Invoke();
            }
        }
        else
        {
            Debug.LogWarning($"[NPCQuestHandler] 當前任務 ID「{questId}」與此 NPC 設定的 quest01/02/03Id 皆不符，將播放預設對話。請在 NPC 上把 quest01Id 等改成與 QuestData.ID 相同。");
            if (defaultDialogue != null) dialogueManager.StartDialogue(defaultDialogue, onComplete);
            else onComplete?.Invoke();
        }
    }
}
