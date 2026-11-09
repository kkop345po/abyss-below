using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro
using System.Collections;
using System.Collections.Generic;

public class AchievementUI : MonoBehaviour
{
    [Header("Notification Settings")]
    public GameObject notificationPanel; // 右下角通知面板
    public Image notificationIcon;
    public TextMeshProUGUI notificationTitle;
    public TextMeshProUGUI notificationDesc;
    public float displayDuration = 3f;
    public float animationSpeed = 5f;

    [Header("List Settings (Optional)")]
    public GameObject listPanel; // 成就列表視窗
    public Transform listContent; // Scroll View Content
    public GameObject achievementItemPrefab; // 列表中的單項 Prefab

    private Queue<AchievementData> notificationQueue = new Queue<AchievementData>();
    private bool isShowingNotification = false;
    private RectTransform notifRect;
    private Vector2 hiddenPos;
    private Vector2 shownPos;

    private void Awake()
    {
        if (notificationPanel != null)
        {
            notifRect = notificationPanel.GetComponent<RectTransform>();
            // 假設初始位置在螢幕外 (右下角更下面的位置或右邊)
            // 這裡假設是右下角，我們用 AnchoredPosition 來控制
            
            // 隱藏時的位置 (Y軸向下移出螢幕)
            hiddenPos = new Vector2(notifRect.anchoredPosition.x, -200f); 
            // 顯示時的位置 (Y軸歸位，假設原本在 Canvas 編輯器中擺好的位置是顯示位置)
            shownPos = notifRect.anchoredPosition; 
            
            // 初始設為隱藏
            notifRect.anchoredPosition = hiddenPos;
            notificationPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // ESC 關閉成就列表（右下角通知照舊自動顯示/隱藏）
        if (listPanel != null &&
            listPanel.activeSelf &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            CloseList();
        }
    }

    public void ShowNotification(AchievementData data)
    {
        notificationQueue.Enqueue(data);
        if (!isShowingNotification)
        {
            StartCoroutine(NotificationCoroutine());
        }
    }

    private IEnumerator NotificationCoroutine()
    {
        isShowingNotification = true;

        while (notificationQueue.Count > 0)
        {
            AchievementData data = notificationQueue.Dequeue();

            // Setup UI
            if (notificationIcon) notificationIcon.sprite = data.icon;
            if (notificationTitle) notificationTitle.text = data.title;
            if (notificationDesc) notificationDesc.text = data.description;
            
            notificationPanel.SetActive(true);

            // Animate In (Slide Up from bottom)
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * animationSpeed;
                notifRect.anchoredPosition = Vector2.Lerp(hiddenPos, shownPos, t);
                yield return null;
            }

            // Wait（暫停選單 timeScale=0 時仍會倒數）
            yield return new WaitForSecondsRealtime(displayDuration);

            // Animate Out (Slide Down)
            t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * animationSpeed;
                notifRect.anchoredPosition = Vector2.Lerp(shownPos, hiddenPos, t);
                yield return null;
            }

            notificationPanel.SetActive(false);
            yield return new WaitForSecondsRealtime(0.5f);
        }

        isShowingNotification = false;
    }

    // 更新列表顯示
    public void RefreshList()
    {
        if (listPanel == null || listContent == null || achievementItemPrefab == null) return;

        // 清除舊的
        foreach (Transform child in listContent) Destroy(child.gameObject);

        // 生成新的
        if (AchievementManager.Instance == null) return;

        foreach (var ach in AchievementManager.Instance.allAchievements)
        {
            GameObject item = Instantiate(achievementItemPrefab, listContent);
            // 這裡假設 Prefab 上有個腳本或是簡單的 UI 元件
            // 為了簡單起見，我們直接獲取子物件
            // 實際專案建議寫一個 AchievementItemController
            
            bool unlocked = AchievementManager.Instance.IsUnlocked(ach.id);
            
            // Find components (Title, Desc, Icon, Overlay)
            var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
            var images = item.GetComponentsInChildren<Image>();

            // 假設 texts[0] = Title, texts[1] = Desc
            if (texts.Length > 0) texts[0].text = unlocked || !ach.isHidden ? ach.title : "???";
            if (texts.Length > 1) texts[1].text = unlocked || !ach.isHidden ? ach.description : "繼續探索以解鎖";

            // 假設 images[0] = BG, images[1] = Icon
            // 簡單處理：如果未解鎖，把 Icon 變黑或半透明
            if (images.Length > 1) 
            {
                 images[1].sprite = ach.icon;
                 images[1].color = unlocked ? Color.white : Color.black;
            }
        }
    }

    // 給按鈕或程式呼叫：開啟成就列表
    public void OpenList()
    {
        if (listPanel == null) return;
        listPanel.SetActive(true);
        RefreshList();
    }

    // 給按鈕或程式呼叫：關閉成就列表
    public void CloseList()
    {
        if (listPanel == null) return;
        listPanel.SetActive(false);
    }
}
