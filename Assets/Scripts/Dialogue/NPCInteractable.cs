using UnityEngine;
using UnityEngine.UI;

public enum NPCType
{
    None,
    QuestGiver,
    Shopkeeper,
    UpgradeMaster
}

public enum UpgradeMode
{
    None,
    Character,
    Weapon,
    Both
}

public class NPCInteractable : MonoBehaviour
{
    [Header("NPC 功能類型")]
    public NPCType npcType = NPCType.None;

    [Header("升級 NPC 模式")]
    public UpgradeMode upgradeMode = UpgradeMode.None;

    [Header("NPC 對應的 UI")]
    public GameObject questPanel;
    public GameObject shopPanel;
    public GameObject upgradeUIPanel;

    [Header("互動提示音效（可選）")]
    public AudioClip interactSound;
    private AudioSource audioSource;

    public GameObject itemShopPanel;
    public GameObject weaponShopPanel;

    [Header("互動提示UI")]
    public GameObject promptUI; // 拖曳提示UI物件
    private Animator promptAnimator; // 新增：提示動畫控制

    public GameObject dialoguePanel; // 對話UI

    public DialogueData dialogueData; // 對話資料

    public DialogueManager dialogueManager; // 拖曳有 DialogueManager 的空物件

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (promptUI != null)
        {
            promptUI.SetActive(false); // 預設隱藏提示
            promptAnimator = promptUI.GetComponent<Animator>(); // 自動抓取Animator
        }
    }

    public void Interact()
    {
        DialogueManager mgr = dialogueManager != null ? dialogueManager : FindDialogueManagerInScene();
        if (mgr != null && mgr.IsDialoguePlaying())
        {
            // 已經在對話中，攔截重複觸發，讓 DialogueManager 本身處理推進
            return;
        }

        if (audioSource != null && interactSound != null)
            audioSource.PlayOneShot(interactSound);
        else
            AudioManager.GetInstance()?.PlayDialogueClickSfx();

        QuestGiver questGiver = GetComponent<QuestGiver>();

        // 如果這個 NPC 身上有掛載進階的 NPCQuestHandler（處理任務階段對話）
        NPCQuestHandler questHandler = GetComponent<NPCQuestHandler>();
        if (questHandler != null)
        {
            // 有任務對話時：先播完對話再給任務（見 OnDialogueComplete），避免此處先 Accept 導致流程錯亂
            questHandler.InteractNPC(OnDialogueComplete);
            return;
        }

        // 無 NPCQuestHandler 時：維持「需要互動才給任務」的舊行為
        if (questGiver != null && questGiver.requireInteraction)
        {
            questGiver.Interact();
        }

        // 否則，使用基本的對話與系統
        DialogueManager dm = dialogueManager != null ? dialogueManager : FindDialogueManagerInScene();
        if (dm != null && dialogueData != null)
        {
            dm.StartDialogue(dialogueData, OnDialogueComplete);
        }
        else
        {
            // 沒有對話資料時，直接開啟功能UI
            OnDialogueComplete();
        }
    }

    /// <summary>
    /// 包含未啟用的物件（對話 UI 常預設關閉，否則 FindObjectOfType 會找不到 DialogueManager）。
    /// </summary>
    private static DialogueManager FindDialogueManagerInScene()
    {
        var list = Object.FindObjectsByType<DialogueManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return list != null && list.Length > 0 ? list[0] : null;
    }

    // 這個方法給 DialogueManager 在對話結束時呼叫
    public void OnDialogueComplete()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false); // 對話結束自動關閉對話UI

        switch (npcType)
        {
            case NPCType.QuestGiver:
                // 與 NPCQuestHandler 併用時：對話結束後若尚未接任務，由 QuestGiver 發放（專案中僅此處會呼叫 AcceptQuest）
                TryGiveQuestFromQuestGiverAfterDialogue();
                if (questPanel != null)
                    questPanel.SetActive(true);
                break;
            case NPCType.Shopkeeper:
                if (shopPanel != null)
                    shopPanel.SetActive(true);
                break;
            case NPCType.UpgradeMaster:
                if (upgradeUIPanel != null)
                {
                    upgradeUIPanel.SetActive(true);
                    HandleUpgradeMode();
                }
                break;
        }
    }

    private void TryGiveQuestFromQuestGiverAfterDialogue()
    {
        QuestGiver qg = GetComponent<QuestGiver>();
        if (qg == null || qg.questToGive == null) return;
        if (QuestManager.Instance == null) return;
        if (QuestManager.Instance.currentQuest != null) return;

        qg.Interact();
    }

    private void HandleUpgradeMode()
    {
        NPCUpgradeUIController controller = upgradeUIPanel.GetComponent<NPCUpgradeUIController>();

        if (controller == null) return;

        // 由於武器升級已被移除，所有模式都只開啟角色升級分頁
        controller.OpenCharacterTab();
        
        /* [Removed] 移除武器升級分頁
        switch (upgradeMode)
        {
            case UpgradeMode.Character:
                controller.OpenCharacterTab();
                break;
            case UpgradeMode.Weapon:
                controller.OpenWeaponTab();
                break;
            case UpgradeMode.Both:
                controller.OpenCharacterTab(); // 預設顯示角色升級分頁
                break;
        }
        */
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && promptUI != null)
        {
            promptUI.SetActive(true);
            if (promptAnimator != null)
            {
                promptAnimator.Play(0); // 播放動畫（從頭）
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && promptUI != null)
        {
            if (promptAnimator != null)
            {
                promptAnimator.Play(0, -1, 0f); // 重置動畫到起點
            }
            promptUI.SetActive(false);
        }
    }
}
