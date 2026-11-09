using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SaveSlotUIController : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("存檔槽數量")] public int slotCount = 4;
    [Header("存檔槽 UI 元件")]
    public TextMeshProUGUI[] slotPreviews; // 存檔 slot 1~3
    public Image[] slotThumbnails; // 縮圖（可選）
    public Button[] slotButtons; // 槽位按鈕
    
    [Header("確認對話框")]
    public GameObject confirmDialog;
    public TextMeshProUGUI confirmDialogTitle;
    public TextMeshProUGUI confirmDialogMessage;
    public Button confirmButton;
    public Button cancelButton;
    
    [Header("存檔選項對話框（覆蓋/載入）")]
    public GameObject slotOptionsDialog;
    public TextMeshProUGUI optionsDialogTitle;
    public TextMeshProUGUI optionsDialogMessage;
    public Button loadButton;
    public Button overwriteButton;
    public Button optionsCancelButton;
    
    [Header("面板設定")]
    public GameObject panelToClose;
    
    private int currentSlot = 0; // 當前操作的槽位
    private Action pendingAction; // 待執行的操作

    void Start()
    {
        FindUIReferences();
        SetupDialogs();
        RefreshAllPreviews();
    }

    void Update()
    {
        // ESC 關閉存檔面板與對話框
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool anyOpen = false;

            if (slotOptionsDialog != null && slotOptionsDialog.activeSelf)
            {
                slotOptionsDialog.SetActive(false);
                anyOpen = true;
            }

            if (confirmDialog != null && confirmDialog.activeSelf)
            {
                confirmDialog.SetActive(false);
                pendingAction = null;
                anyOpen = true;
            }

            if (!anyOpen && panelToClose != null && panelToClose.activeSelf)
            {
                ClosePanel();
            }
        }
    }

    void OnEnable()
    {
        // 面板開啟時重新綁定按鈕，確保事件正確
        SetupSlotButtons();
        RefreshAllPreviews();
    }

    private void FindUIReferences()
    {
        // 尋找存檔預覽文字
        if (slotPreviews == null || slotPreviews.Length != slotCount)
        {
            slotPreviews = new TextMeshProUGUI[slotCount];
            for (int i = 1; i <= slotCount; i++)
            {
                var slotText = transform.Find($"Slot{i}/PreviewText")?.GetComponent<TextMeshProUGUI>();
                if (slotText == null)
                    slotText = GameObject.Find($"PreviewText{i}")?.GetComponent<TextMeshProUGUI>();
                if (slotText != null)
                    slotPreviews[i - 1] = slotText;
            }
        }
        
        // 尋找縮圖（可選）
        if (slotThumbnails == null || slotThumbnails.Length != slotCount)
        {
            slotThumbnails = new Image[slotCount];
            for (int i = 1; i <= slotCount; i++)
            {
                var thumb = transform.Find($"Slot{i}/Thumbnail")?.GetComponent<Image>();
                if (thumb != null)
                    slotThumbnails[i - 1] = thumb;
            }
        }
        
        // 尋找槽位按鈕
        if (slotButtons == null || slotButtons.Length != slotCount)
        {
            slotButtons = new Button[slotCount];
            for (int i = 1; i <= slotCount; i++)
            {
                var btn = transform.Find($"Slot{i}")?.GetComponent<Button>();
                if (btn != null)
                {
                    slotButtons[i - 1] = btn;
                }
            }
        }
        
        // 綁定按鈕事件（無論陣列是自動尋找還是手動設置的）
        SetupSlotButtons();

        // 尋找確認對話框
        if (confirmDialog == null)
            confirmDialog = transform.Find("ConfirmDialog")?.gameObject;
        
        if (confirmDialog != null)
        {
            if (confirmDialogTitle == null)
                confirmDialogTitle = confirmDialog.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (confirmDialogMessage == null)
                confirmDialogMessage = confirmDialog.transform.Find("Message")?.GetComponent<TextMeshProUGUI>();
            if (confirmButton == null)
                confirmButton = confirmDialog.transform.Find("ConfirmButton")?.GetComponent<Button>();
            if (cancelButton == null)
                cancelButton = confirmDialog.transform.Find("CancelButton")?.GetComponent<Button>();
        }
        
        // 尋找存檔選項對話框
        if (slotOptionsDialog == null)
            slotOptionsDialog = transform.Find("SlotOptionsDialog")?.gameObject;
        
        if (slotOptionsDialog != null)
        {
            if (optionsDialogTitle == null)
                optionsDialogTitle = slotOptionsDialog.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (optionsDialogMessage == null)
                optionsDialogMessage = slotOptionsDialog.transform.Find("Message")?.GetComponent<TextMeshProUGUI>();
            if (loadButton == null)
                loadButton = slotOptionsDialog.transform.Find("LoadButton")?.GetComponent<Button>();
            if (overwriteButton == null)
                overwriteButton = slotOptionsDialog.transform.Find("OverwriteButton")?.GetComponent<Button>();
            if (optionsCancelButton == null)
                optionsCancelButton = slotOptionsDialog.transform.Find("CancelButton")?.GetComponent<Button>();
        }

        // 尋找面板
        if (panelToClose == null)
        {
            panelToClose = transform.parent?.gameObject;
            if (panelToClose == null)
                panelToClose = gameObject;
        }
    }
    
    /// <summary>
    /// 綁定存檔槽按鈕事件
    /// </summary>
    private void SetupSlotButtons()
    {
        if (slotButtons == null) return;
        
        for (int i = 0; i < slotButtons.Length && i < slotCount; i++)
        {
            if (slotButtons[i] != null)
            {
                int slotNum = i + 1; // 槽位從 1 開始
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotNum));
                Debug.Log($"[SaveSlotUIController] 綁定按鈕 Slot{slotNum}");
            }
        }
    }
    
    private void SetupDialogs()
    {
        if (confirmDialog != null)
            confirmDialog.SetActive(false);
        
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmAction);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelAction);
        }
        
        if (slotOptionsDialog != null)
            slotOptionsDialog.SetActive(false);
        
        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(OnLoadButtonClicked);
        }
        
        if (overwriteButton != null)
        {
            overwriteButton.onClick.RemoveAllListeners();
            overwriteButton.onClick.AddListener(OnOverwriteButtonClicked);
        }
        
        if (optionsCancelButton != null)
        {
            optionsCancelButton.onClick.RemoveAllListeners();
            optionsCancelButton.onClick.AddListener(OnOptionsCancel);
        }
    }
    
    /// <summary>
    /// 處理槽位點擊
    /// </summary>
    private void OnSlotClicked(int slot)
    {
        Debug.Log($"[SaveSlotUIController] 點擊存檔槽 {slot}");
        currentSlot = slot;
        bool isEmpty = !SaveSlotSystem.HasSaveSlot(slot);
        
        if (isEmpty)
        {
            // 空槽：直接保存
            Debug.Log($"[SaveSlotUIController] 槽位 {slot} 為空，開始存檔");
            SaveToSlot(slot);
        }
        else
        {
            // 有存檔：顯示選項對話框
            Debug.Log($"[SaveSlotUIController] 槽位 {slot} 已有存檔，顯示選項對話框");
            ShowSlotOptionsDialog(slot);
        }
    }

    public void SaveToSlot(int slot)
    {
        // 異步截圖並保存
        StartCoroutine(SaveWithThumbnail(slot));
    }
    
    private System.Collections.IEnumerator SaveWithThumbnail(int slot)
    {
        // 等待一幀確保渲染完成
        yield return null;
        
        Texture2D thumbnail = SaveSlotSystem.CaptureThumbnail();
        SaveSlotSystem.SaveToSlot(slot, thumbnail);
        RefreshPreview(slot);
    }

    public void LoadFromSlot(int slot)
    {
        SaveSlotSystem.LoadFromSlot(slot);
        // 載入後恢復時間與暫停堆疊
        PanelPauseManager.ForceResumeAll();
        if (Time.timeScale != 1f) Time.timeScale = 1f;
        if (panelToClose != null)
            panelToClose.SetActive(false);
    }
    
    public void DeleteSlot(int slot)
    {
        ShowConfirmDialog(
            "確認刪除",
            $"確定要刪除存檔槽 {slot} 嗎？\n此操作無法復原。",
            () =>
            {
                SaveSlotSystem.DeleteSlot(slot);
                RefreshPreview(slot);
            },
            null
        );
    }

    public void RefreshAllPreviews()
    {
        for (int i = 1; i <= slotCount; i++)
        {
            RefreshPreview(i);
        }
    }

    public void RefreshPreview(int slot)
    {
        if (slot < 1 || slot > slotCount) return;

        var data = SaveSlotSystem.GetSlotPreview(slot);
        
        // 更新文字預覽
        if (slotPreviews != null && slotPreviews.Length >= slot && slotPreviews[slot - 1] != null)
        {
            if (data == null)
            {
                slotPreviews[slot - 1].text = "空存檔";
            }
            else
            {
                // 只顯示時間，不要前綴
                slotPreviews[slot - 1].text = data.GetFormattedTime();
            }
        }
        
        // 更新縮圖
        if (slotThumbnails != null && slotThumbnails.Length >= slot && slotThumbnails[slot - 1] != null)
        {
            var thumbnail = SaveSlotSystem.LoadThumbnail(slot);
            if (thumbnail != null)
            {
                try
                {
                    // 如果已有 Sprite，先銷毀避免記憶體洩漏
                    if (slotThumbnails[slot - 1].sprite != null)
                    {
                        DestroyImmediate(slotThumbnails[slot - 1].sprite);
                    }
                    
                    Sprite sprite = Sprite.Create(
                        thumbnail,
                        new Rect(0, 0, thumbnail.width, thumbnail.height),
                        new Vector2(0.5f, 0.5f),
                        100f // pixelsPerUnit
                    );
                    slotThumbnails[slot - 1].sprite = sprite;
                    slotThumbnails[slot - 1].gameObject.SetActive(true);
                    Debug.Log($"[SaveSlotUIController] 縮圖已顯示到槽位 {slot}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SaveSlotUIController] 創建縮圖 Sprite 失敗 (槽位 {slot}): {e.Message}");
                    slotThumbnails[slot - 1].gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.Log($"[SaveSlotUIController] 槽位 {slot} 沒有縮圖檔案");
                slotThumbnails[slot - 1].gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 顯示存檔選項對話框（覆蓋/載入）
    /// </summary>
    private void ShowSlotOptionsDialog(int slot)
    {
        var saveData = SaveSlotSystem.GetSlotPreview(slot);
        if (saveData == null) return;
        
        if (slotOptionsDialog == null)
        {
            // 如果沒有選項對話框，直接顯示確認對話框
            ShowConfirmDialog(
                "選擇操作",
                $"存檔槽 {slot}\n存檔時間：{saveData.GetFormattedTime()}\n場景：{saveData.sceneName}\n\n請選擇要執行的操作。",
                () => LoadFromSlot(slot),
                null
            );
            return;
        }
        
        if (optionsDialogTitle != null)
            optionsDialogTitle.text = "存檔槽 " + slot;
        
        if (optionsDialogMessage != null)
            optionsDialogMessage.text = $"存檔時間：{saveData.GetFormattedTime()}\n場景：{saveData.sceneName}\n金幣：{saveData.coins}";
        
        slotOptionsDialog.SetActive(true);
    }
    
    private void OnLoadButtonClicked()
    {
        slotOptionsDialog.SetActive(false);
        LoadFromSlot(currentSlot);
    }
    
    private void OnOverwriteButtonClicked()
    {
        slotOptionsDialog.SetActive(false);
        ShowConfirmDialog(
            "確認覆蓋",
            $"確定要覆蓋存檔槽 {currentSlot} 嗎？\n原有存檔將被永久刪除。",
            () => SaveToSlot(currentSlot),
            null
        );
    }
    
    private void OnOptionsCancel()
    {
        slotOptionsDialog.SetActive(false);
    }
    
    /// <summary>
    /// 顯示確認對話框
    /// </summary>
    private void ShowConfirmDialog(string title, string message, Action onConfirm, Action onCancel)
    {
        if (confirmDialog == null)
        {
            onConfirm?.Invoke();
            return;
        }
        
        if (confirmDialogTitle != null)
            confirmDialogTitle.text = title;
        
        if (confirmDialogMessage != null)
            confirmDialogMessage.text = message;
        
        pendingAction = () => onConfirm?.Invoke();
        confirmDialog.SetActive(true);
    }
    
    private void OnConfirmAction()
    {
        pendingAction?.Invoke();
        pendingAction = null;
        if (confirmDialog != null)
            confirmDialog.SetActive(false);
    }
    
    private void OnCancelAction()
    {
        pendingAction = null;
        if (confirmDialog != null)
            confirmDialog.SetActive(false);
    }

    public void ClosePanel()
    {
        if (panelToClose != null)
            panelToClose.SetActive(false);
    }
}