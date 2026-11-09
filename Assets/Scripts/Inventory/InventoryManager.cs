using System;
using System.Collections.Generic;
using Game.Inventory.UI;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public int maxSlots = 16;
    public List<InventoryItem> items = new List<InventoryItem>();
    public InventoryItem GetItemById(string id)
    {
        return items.Find(i => i.itemId == id);
    }
    
    // UI 更新事件
    public static event Action OnInventoryChanged;

    /// <summary>供存檔載入等「類別外部」觸發背包 UI 刷新（event 不可在外部 Invoke）。</summary>
    public static void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 在 <see cref="PlayerPrefs.DeleteAll"/> 之後呼叫：依已清空的偏好還原背包記憶體、寫回空的 InventoryData，並刷新快捷欄。
    /// </summary>
    public static void ApplyGlobalPrefsResetToInventory()
    {
        if (Instance == null) return;
        Instance.LoadInventory();
        Instance.SaveInventory();
        NotifyInventoryChanged();
        InventoryQuickbarBinder.Instance?.Refresh();
    }

    /// <summary>
    /// 新遊戲（從 Menu 開始玩）用：清空背包資料並刷新 UI/快捷欄。
    /// 不會動到金幣/升級等其他 PlayerPrefs key。
    /// </summary>
    public static void ResetInventoryForNewGame()
    {
        PlayerPrefs.DeleteKey("InventoryData");
        PlayerPrefs.Save();

        if (Instance != null)
        {
            Instance.LoadInventory();  // 讀到空
            Instance.SaveInventory();  // 寫回空
            NotifyInventoryChanged();
            InventoryQuickbarBinder.Instance?.Refresh();
        }
    }

    /// <summary>
    /// 玩家死亡懲罰：
    /// - 消耗品：全部刪除
    /// - 素材：扣除一定比例（預設 30%）
    /// - 任務道具：保留
    /// - 武器/裝備等：保留
    /// </summary>
    public static void ApplyDeathPenalty(float lossPercent = 0.3f)
    {
        if (Instance == null) return;
        Instance.ApplyDeathPenaltyInternal(lossPercent);
    }

    private void ApplyDeathPenaltyInternal(float lossPercent)
    {
        lossPercent = Mathf.Clamp01(lossPercent);
        float keepPercent = 1f - lossPercent;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            var it = items[i];
            if (it == null) continue;

            if (it.itemType == ItemType.Consumable)
            {
                items.RemoveAt(i);
                continue;
            }

            if (it.itemType == ItemType.Material)
            {
                int kept = Mathf.FloorToInt(Mathf.Max(0, it.quantity) * keepPercent);
                if (kept <= 0)
                    items.RemoveAt(i);
                else
                    it.quantity = kept;
            }
        }

        SaveInventory();
        NotifyInventoryChanged();
        InventoryQuickbarBinder.Instance?.Refresh();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }

        static T LoadFirstFromResources<T>(string preferredName = null) where T : UnityEngine.Object
        {
            if (!string.IsNullOrEmpty(preferredName))
            {
                var preferred = Resources.Load<T>(preferredName);
                if (preferred != null) return preferred;
            }
            // 若未命名或命名找不到，退回抓 Resources 內第一個同型別資產
            var all = Resources.LoadAll<T>("");
            return (all != null && all.Length > 0) ? all[0] : null;
        }

        // 從 Menu 進入遊戲時，常駐的 InventoryManager 可能是「Menu 場景那份 Prefab」或被動建立，
        // Inspector 未指派資料庫會導致 ResolveItemData 失敗（背包顯示成 itemId、icon 變 null）。
        // 這裡提供 Resources 後備：Assets/Resources/ItemDatabase.asset
        if (itemDatabase == null)
            itemDatabase = LoadFirstFromResources<ItemDatabaseSO>("ItemDatabase");
        // QuestItemDatabase 也給後備（若你之後把任務道具庫移到 Resources/QuestItemDatabase.asset 就會自動抓到）
        if (questItemDatabase == null)
            questItemDatabase = LoadFirstFromResources<QuestItemDatabaseSO>("QuestItemDatabase");

        LoadInventory();
    }

    /// <summary>
    /// 先查 ItemDatabase，再查任務道具庫（與 QuestItemDatabase 一致時可顯示圖示／名稱）。
    /// </summary>
    public ItemData ResolveItemData(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        if (itemDatabase != null)
        {
            var d = itemDatabase.GetItem(itemId);
            if (d != null) return d;
            // 允許用「Item Name」傳入（例如賭博系統想用中文名），也能對到資料庫
            var byName = itemDatabase.allItems != null ? itemDatabase.allItems.Find(x => x != null && x.itemName == itemId) : null;
            if (byName != null) return byName;
        }
        if (questItemDatabase != null)
        {
            var q = questItemDatabase.GetQuestItem(itemId);
            if (q != null) return q;
            // 也允許用名稱找任務道具（保底）
            if (questItemDatabase.questItems != null)
                return questItemDatabase.questItems.Find(x => x != null && x.itemName == itemId);
        }
        return null;
    }

    /// <summary>
    /// 任務完成時：移除該任務「收集類」目標對應道具（不交給下一任務帶入）。
    /// </summary>
    public void RemoveItemsForCompletedQuestCollectObjectives(QuestData completedQuest)
    {
        if (completedQuest == null || completedQuest.Objectives == null) return;

        foreach (var obj in completedQuest.Objectives)
        {
            if (obj.Type != QuestObjectiveType.CollectItem || string.IsNullOrEmpty(obj.TargetKey))
                continue;
            var stack = GetItemById(obj.TargetKey);
            if (stack != null && stack.quantity > 0)
                RemoveItem(obj.TargetKey, stack.quantity);
        }
    }

    public void AddItem(string itemId, int amount)
    {
        var existing = items.Find(i => i.itemId == itemId);
        if (existing != null)
        {
            existing.quantity += amount;
        }
        else if (items.Count < maxSlots)
        {
            ItemData data = ResolveItemData(itemId);

            InventoryItem newItem = new InventoryItem
            {
                itemId = itemId,
                itemName = data != null ? data.itemName : itemId,
                icon = data != null ? data.icon : null,
                description = data != null ? data.description : "",
                itemType = data != null ? data.itemType : ItemType.Material,
                quantity = amount
            };
            items.Add(newItem);
        }
        else
        {
            Debug.Log("背包已滿，無法新增物品！");
            return;
        }

        SaveInventory();
        OnInventoryChanged?.Invoke();
    }

    public void AddItem(ShopItemData data, int amount)
    {
        var existing = items.Find(i => i.itemId == data.itemId);
        if (existing != null)
        {
            existing.quantity += amount;
        }
        else if (items.Count < maxSlots)
        {
            InventoryItem newItem = new InventoryItem
            {
                itemId = data.itemId,
                itemName = data.itemName,
                icon = data.icon,
                description = data.description,
                itemType = data.type == ShopItemType.Weapon ? ItemType.Weapon : ItemType.Consumable,
                quantity = amount
            };
            items.Add(newItem);
        }
        else
        {
            Debug.Log("背包已滿，無法新增物品！");
            return;
        }

        SaveInventory();
        OnInventoryChanged?.Invoke();
    }

    public void RemoveItem(string itemId, int amount)
    {
        var existing = items.Find(i => i.itemId == itemId);
        if (existing != null)
        {
            existing.quantity -= amount;
            if (existing.quantity <= 0)
                items.Remove(existing);
            SaveInventory();
            OnInventoryChanged?.Invoke(); // 通知 UI 更新
        }
    }

    //  存檔
    public void SaveInventory()
    {
        string json = JsonUtility.ToJson(new InventoryWrapper(items), true);
        PlayerPrefs.SetString("InventoryData", json);
        PlayerPrefs.Save();
    }

    public ItemDatabaseSO itemDatabase;
    [Tooltip("與 QuestItemDatabase 相同資產即可；當 ItemDatabase 查無此 id 時作為備援（背包顯示與存檔同步）")]
    public QuestItemDatabaseSO questItemDatabase;

    //  讀檔
    public void LoadInventory()
    {
        items.Clear();
        string json = PlayerPrefs.GetString("InventoryData", "");
        if (!string.IsNullOrEmpty(json))
        {
            InventoryWrapper wrapper = JsonUtility.FromJson<InventoryWrapper>(json);
            if (wrapper != null && wrapper.items != null)
            {
                items = wrapper.items;
                
                foreach (var item in items)
                {
                    var data = ResolveItemData(item.itemId);
                    if (data != null)
                    {
                        item.icon = data.icon;
                        item.itemName = data.itemName;
                        item.description = data.description;
                        item.itemType = data.itemType;
                    }
                    else
                    {
                        Debug.LogWarning($"[InventoryManager] ItemDatabase 與任務道具庫都找不到: {item.itemId}");
                    }
                }
            }
        }
    }

    // 素材相關的專門方法
    public int GetMaterialCount(MaterialType materialType)
    {
        string materialId = "mat_" + (int)materialType;
        var item = GetItemById(materialId);
        return item != null ? item.quantity : 0;
    }

    public bool RemoveMaterial(MaterialType materialType, int amount)
    {
        string materialId = "mat_" + (int)materialType;
        var item = GetItemById(materialId);
        
        if (item != null && item.quantity >= amount)
        {
            RemoveItem(materialId, amount);
            return true;
        }
        return false;
    }

    public void AddMaterial(MaterialType materialType, int amount)
    {
        string materialId = "mat_" + (int)materialType;
        AddItem(materialId, amount);
    }

    /// <summary> 由裝備欄卸回背包等路徑呼叫；邏輯與 AddItem(itemId, amount) 一致。 </summary>
    internal void AddItem(InventoryItem inventoryItem)
    {
        if (inventoryItem == null || string.IsNullOrEmpty(inventoryItem.itemId) || inventoryItem.quantity <= 0)
            return;
        AddItem(inventoryItem.itemId, inventoryItem.quantity);
    }

    // --- 冷卻系統 ---

    public void SetCooldown(string itemId, float duration)
    {
        var item = GetItemById(itemId);
        if (item != null)
        {
            // 計算結束時間點 (當前時間 + 持續時間)
            long endTicks = DateTime.Now.AddSeconds(duration).Ticks;
            item.cooldownEndTicks = endTicks;
            SaveInventory(); // 立即存檔
        }
    }

    public float GetCooldownRemaining(string itemId)
    {
        var item = GetItemById(itemId);
        if (item != null && item.cooldownEndTicks > 0)
        {
            long now = DateTime.Now.Ticks;
            if (item.cooldownEndTicks > now)
            {
                // 換算回秒數
                TimeSpan remaining = new TimeSpan(item.cooldownEndTicks - now);
                return (float)remaining.TotalSeconds;
            }
        }
        return 0f;
    }

    public void ClearCooldown(string itemId)
    {
        var item = GetItemById(itemId);
        if (item == null)
            return;

        if (item.cooldownEndTicks == 0)
            return;

        item.cooldownEndTicks = 0;
        SaveInventory();
    }

    [System.Serializable]
    private class InventoryWrapper
    {
        public List<InventoryItem> items;
        public InventoryWrapper(List<InventoryItem> items) => this.items = items;
    }
}
