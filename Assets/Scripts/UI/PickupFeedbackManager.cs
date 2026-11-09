using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PickupFeedbackManager : MonoBehaviour
{
    public static PickupFeedbackManager Instance;

    [Header("UI Settings")]
    public GameObject pickupLinePrefab; // 必須掛有 PickupLine 組件
    public Transform container; // 掛有 VerticalLayoutGroup 的容器 (通常在 Canvas 上)

    private Dictionary<string, PickupLine> activeLines = new Dictionary<string, PickupLine>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowPickup(string itemName, int amount)
    {
        if (pickupLinePrefab == null || container == null) return;

        // 檢查是否已有該物品的行
        if (activeLines.ContainsKey(itemName) && activeLines[itemName] != null)
        {
            // 更新現有行
            PickupLine line = activeLines[itemName];
            
            // 這裡我們需要知道之前的總數，但為了簡化，我們假設調用者會處理累積
            // 或者我們可以在這裡維護一個計數器
            // 為了實現 "金幣 * 50" -> "金幣 * 60" 的效果，我們需要儲存當前顯示的數量
            
            // 簡單做法：我們在 Dictionary 裡存的不只是 Line，還有當前數量
            // 但因為 PickupLine 可能被銷毀，我們需要小心
            
            // 修正策略：
            // 我們不存數量，而是讓調用者傳入 "增加量"，然後我們解析字串? 不太好
            // 更好做法：PickupLine 自己記住數量
        }
        
        // 重新實作：使用內部類別或修改 PickupLine 來支援數量累加
        // 為了保持簡單，我們直接在 ShowPickup 處理邏輯
        
        PickupLine targetLine = null;
        
        if (activeLines.TryGetValue(itemName, out targetLine))
        {
            if (targetLine == null) // 可能已被銷毀
            {
                activeLines.Remove(itemName);
            }
        }

        if (targetLine != null)
        {
            // 累加
            // 我們需要從 PickupLine 獲取當前數量，這需要修改 PickupLine
            // 這裡先用 SendMessage 或 GetComponent 獲取自定義數據
            // 為了方便，我們假設 PickupLine 有個 CurrentAmount 屬性
            // 由於 PickupLine 是新寫的，我們可以去修改它，或者用一個簡單的 Dictionary 輔助
            
            // 這裡用 Dictionary 輔助計數
            if (!itemCounts.ContainsKey(itemName)) itemCounts[itemName] = 0;
            itemCounts[itemName] += amount;
            
            targetLine.UpdateText($"{itemName} * {itemCounts[itemName]}");
        }
        else
        {
            // 新增
            GameObject obj = Instantiate(pickupLinePrefab, container);
            targetLine = obj.GetComponent<PickupLine>();
            
            if (targetLine == null)
            {
                Debug.LogError($"[PickupFeedbackManager] Prefab '{pickupLinePrefab.name}' 缺少 PickupLine 組件！請在 Inspector 中掛上 PickupLine 腳本。");
                Destroy(obj);
                return;
            }

            itemCounts[itemName] = amount;
            targetLine.Initialize($"{itemName} * {amount}");
            
            // 監聽銷毀以便移除 Dictionary 紀錄 (這比較麻煩，不如直接在 Update 檢查 null)
            activeLines[itemName] = targetLine;
        }
    }

    private Dictionary<string, int> itemCounts = new Dictionary<string, int>();

    private void Update()
    {
        // 清理已銷毀的 Line
        List<string> keysToRemove = new List<string>();
        foreach (var kvp in activeLines)
        {
            if (kvp.Value == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            activeLines.Remove(key);
            itemCounts.Remove(key); // 重置計數
        }
    }
}
