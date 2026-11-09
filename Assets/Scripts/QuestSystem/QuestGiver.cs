using UnityEngine;

public class QuestGiver : MonoBehaviour
{
    [Header("Quest Settings")]
    public QuestData questToGive;
    public bool oneTimeOnly = true;

    [Header("Activation Mode")]
    public bool autoAcceptOnEnter = true; // 進入範圍自動接取
    public bool requireInteraction = false; // 需要按互動鍵 (尚未實作完整互動系統，暫時用 Trigger 模擬)

    private bool isGiven = false;

    private void Start()
    {
        // 如果這個任務已經完成或正在進行，就不再給予 (簡單防呆)
        if (QuestManager.Instance != null && questToGive != null)
        {
            // 這裡可以擴充檢查：是否已經接過這個任務？
            // 目前 QuestManager 還沒提供查詢「已完成任務」的 API，暫時省略
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isGiven && oneTimeOnly) return;
        if (!other.CompareTag("Player")) return;

        if (QuestManager.Instance == null) return;

        // 模式 1: 自動接取 (走出新手村、到達特定地點)
        if (autoAcceptOnEnter && !requireInteraction)
        {
            GiveQuest();
        }
    }

    // 如果您有按鍵互動系統 (例如 PlayerInteract)，可以在那邊呼叫這個方法
    public void Interact()
    {
        if (isGiven && oneTimeOnly) return;
        GiveQuest();
    }

    private void GiveQuest()
    {
        if (questToGive == null)
        {
            Debug.LogWarning($"[QuestGiver] {name} 沒有設定 Quest Data！");
            return;
        }

        Debug.Log($"[QuestGiver] 給予任務: {questToGive.Title}");
        QuestManager.Instance.AcceptQuest(questToGive);
        
        isGiven = true;
        
        // 視覺回饋 (可選)
        // e.g. 隱藏頭上的驚嘆號
        // GetComponent<SpriteRenderer>().enabled = false; 
        
        // 仍須與 NPC 對話推進任務時，不可關閉碰撞（Collider 常在子物件，此處也只會關到同物件上的 Collider）
        if (oneTimeOnly && GetComponent<NPCQuestHandler>() == null)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col) col.enabled = false;
        }
    }
}
