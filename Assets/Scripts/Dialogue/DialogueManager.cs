using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    /// <summary>
    /// 對話剛結束時的 Time.frameCount，供 PlayerInteract 等同幀避免 E 重開互動。
    /// </summary>
    public static int LastDialogueEndedFrame { get; private set; } = -1;

    public GameObject dialogueBackground;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI dialogueText;
    public Image npcPortrait;
    public Image playerPortrait;

    private DialogueLine[] currentDialogueLines;
    private int currentLineIndex = 0;
    private bool isDialoguePlaying = false;
    private bool isTyping = false;

    private Sprite currentNPCPortrait = null;

    // ✅ 新增：對話結束時執行的回調
    private System.Action onDialogueFinishedCallback;

    private Coroutine typingCoroutine; // 新增：保存打字機協程
    private string currentLineText = ""; // 新增：目前行內容

    /// <summary>
    /// 開始對話（支援結束時執行的回調）
    /// </summary>
    public void StartDialogue(DialogueData dialogueData, System.Action onFinished = null)
    {
        // 對話面板開啟即暫停
        if (dialogueBackground != null && dialogueBackground.GetComponent<PauseOnEnable>() == null)
            dialogueBackground.AddComponent<PauseOnEnable>();

        // 鎖定玩家操作
        var player = GameObject.FindWithTag("Player");
        var controller = player != null ? player.GetComponent<PlayerController>() : null;
        controller?.LockInput();
        dialogueBackground.SetActive(true);
        npcNameText.gameObject.SetActive(true);
        playerNameText.gameObject.SetActive(true);
        dialogueText.gameObject.SetActive(true);
        npcPortrait.gameObject.SetActive(true);
        playerPortrait.gameObject.SetActive(true);

        currentDialogueLines = dialogueData.lines;
        currentLineIndex = 0;
        isDialoguePlaying = true;
        isTyping = false;

        onDialogueFinishedCallback = onFinished; // ✅ 存下回調

        DisplayCurrentLine();
    }

    private void DisplayCurrentLine()
    {
        if (currentDialogueLines != null && currentLineIndex < currentDialogueLines.Length)
        {
            DialogueLine line = currentDialogueLines[currentLineIndex];
            StopAllCoroutines();
            typingCoroutine = StartCoroutine(TypeLine(line));
        }
    }

    private IEnumerator TypeLine(DialogueLine line)
    {
        isTyping = true;
        dialogueText.text = "";
        currentLineText = line.text;

        if (line.isPlayerSpeaking)
        {
            playerNameText.text = line.speakerName;
            npcNameText.text = "";
            playerPortrait.sprite = line.speakerPortrait;
            playerPortrait.color = Color.white;
            npcPortrait.color = new Color(1, 1, 1, 0.3f);
        }
        else
        {
            npcNameText.text = line.speakerName;
            playerNameText.text = "";
            if (line.speakerPortrait != null)
                currentNPCPortrait = line.speakerPortrait;
            npcPortrait.sprite = currentNPCPortrait;
            npcPortrait.color = Color.white;
            playerPortrait.color = new Color(1, 1, 1, 0.3f);
        }

        foreach (char letter in line.text)
        {
            dialogueText.text += letter;
            // 使用 Unscaled，暫停時仍可打字
            yield return new WaitForSecondsRealtime(0.05f);
        }

        isTyping = false;
    }

    void Update()
    {
        if (isDialoguePlaying)
        {
            // 滑鼠左鍵：前進或跳過打字
            if (isTyping && Input.GetMouseButtonDown(0))
            {
                // 跳過打字機效果，直接顯示完整句子
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                    dialogueText.text = currentLineText;
                    isTyping = false;
                }
                AudioManager.GetInstance()?.PlayDialogueClickSfx();
            }
            else if (!isTyping && Input.GetMouseButtonDown(0))
            {
                AudioManager.GetInstance()?.PlayDialogueClickSfx();
                NextLine();
            }

            // ESC：直接結束對話（視為「取消當前 UI」）
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EndDialogue();
            }
        }
    }

    public void NextLine()
    {
        if (!isDialoguePlaying) return;

        currentLineIndex++;

        if (currentLineIndex < currentDialogueLines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        isDialoguePlaying = false;
        LastDialogueEndedFrame = Time.frameCount;

        dialogueBackground.SetActive(false);
        npcNameText.gameObject.SetActive(false);
        playerNameText.gameObject.SetActive(false);
        dialogueText.gameObject.SetActive(false);
        npcPortrait.gameObject.SetActive(false);
        playerPortrait.gameObject.SetActive(false);

        // ✅ 執行結束後的回呼（例如開啟升級 UI）
        onDialogueFinishedCallback?.Invoke();

        // 解除玩家鎖定
        var player = GameObject.FindWithTag("Player");
        var controller = player != null ? player.GetComponent<PlayerController>() : null;
        controller?.UnlockInput();
    }

    public bool IsDialoguePlaying()
    {
        return isDialoguePlaying;
    }

    /// <summary>
    /// 由 PlayerInteract 在按 E 時先呼叫；若正在對話則消耗輸入（與滑鼠左鍵相同：跳打字或下一句）。
    /// </summary>
    public bool TryConsumeInteractKey()
    {
        if (!isDialoguePlaying) return false;

        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            dialogueText.text = currentLineText;
            isTyping = false;
            AudioManager.GetInstance()?.PlayDialogueClickSfx();
        }
        else
        {
            AudioManager.GetInstance()?.PlayDialogueClickSfx();
            NextLine();
        }

        return true;
    }
}
