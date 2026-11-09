using UnityEngine;

public class BackToPreviousSceneTrigger : MonoBehaviour
{
    public GameObject confirmPanel; // 指定UI面板

    [Header("確認 UI 行為")]
    [Tooltip("顯示確認面板時暫停遊戲時間（Time.timeScale=0），關閉面板後還原。")]
    public bool pauseTimeWhileConfirmOpen = true;
    [Tooltip("顯示確認面板時可用 ESC 關閉面板並恢復時間。")]
    public bool cancelConfirmWithEscape = true;

    private bool confirmOpen;
    private bool pausedByThis;
    private float savedTimeScale = 1f;

    void Update()
    {
        if (!cancelConfirmWithEscape || !confirmOpen) return;
        if (confirmPanel == null || !confirmPanel.activeInHierarchy) return;
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        CloseConfirm();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && confirmPanel != null)
        {
            confirmPanel.SetActive(true);
            confirmOpen = true;

            if (pauseTimeWhileConfirmOpen)
            {
                savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                pausedByThis = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && confirmPanel != null)
        {
            CloseConfirm();
        }
    }

    void OnDisable()
    {
        CloseConfirm();
    }

    void CloseConfirm()
    {
        if (confirmPanel != null && confirmPanel.activeSelf)
            confirmPanel.SetActive(false);

        if (pausedByThis)
        {
            Time.timeScale = Mathf.Approximately(savedTimeScale, 0f) ? 1f : savedTimeScale;
            pausedByThis = false;
        }

        confirmOpen = false;
    }

    /// <summary>供確認 UI（按鈕）呼叫：避免只靠 TriggerExit 才還原 timeScale。</summary>
    public void NotifyConfirmClosedFromUi()
    {
        CloseConfirm();
    }
} 