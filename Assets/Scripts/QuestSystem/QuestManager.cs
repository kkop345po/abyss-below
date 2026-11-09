using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[Serializable]
class QuestProgressBlob
{
    public QuestKv[] entries;
}

[Serializable]
class QuestKv
{
    public string k;
    public int v;
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    /// <summary> 與 SaveSlotData 共用鍵名，避免任務只存在 PlayerPrefs 而與存檔槽不同步。 </summary>
    public const string QuestPlayerPrefsIdKey = "Quest_CurrentQuestId";
    public const string QuestPlayerPrefsProgressKey = "Quest_ObjectiveProgressJson";

    [Header("存檔")]
    [Tooltip("勾選時將目前任務與目標進度寫入 PlayerPrefs（與背包等一併持久化）。")]
    [SerializeField] private bool persistQuestProgress = true;
    [Tooltip("依 QuestData 的 ID 還原任務資產；請將專案內所有任務拖入（含鏈狀 NextQuest）。")]
    [SerializeField] private QuestData[] questRegistry;

    [Header("Runtime Data")]
    public QuestData currentQuest;

    // Events for UI
    public event Action<QuestData> OnQuestAccepted;
    public event Action OnQuestUpdated;
    public event Action<QuestData> OnQuestCompleted;
    
    // Key: TargetKey (String), Value: Current Amount
    private Dictionary<string, int> objectiveProgress = new Dictionary<string, int>();

    public int GetCurrentAmount(string key)
    {
        if (objectiveProgress.ContainsKey(key))
            return objectiveProgress[key];
        return 0;
    }

    /// <summary>
    /// 用於 UI／NPC：同時參考內存進度與背包，避免 ID 未寫進字典或不同步時誤判。
    /// </summary>
    public int GetEffectiveAmount(string key)
    {
        int p = GetCurrentAmount(key);
        if (InventoryManager.Instance == null) return p;
        var stack = InventoryManager.Instance.GetItemById(key);
        int inv = stack != null ? stack.quantity : 0;
        return Mathf.Max(p, inv);
    }

    /// <summary>
    /// 任務內所有「收集」目標是否皆以背包數量達成（與 QuestObjective 的 TargetKey / RequiredAmount 一致）。
    /// NPC 交任務對話應以此為準，勿硬編碼道具 ID。
    /// </summary>
    public bool AreAllCollectObjectivesMetByInventory()
    {
        if (currentQuest?.Objectives == null) return false;
        var inv = InventoryManager.Instance;
        if (inv == null) return false;

        bool anyCollect = false;
        foreach (var obj in currentQuest.Objectives)
        {
            if (obj.Type != QuestObjectiveType.CollectItem || string.IsNullOrEmpty(obj.TargetKey))
                continue;
            anyCollect = true;
            var stack = inv.GetItemById(obj.TargetKey);
            int q = stack != null ? stack.quantity : 0;
            if (q < obj.RequiredAmount)
                return false;
        }

        return anyCollect;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 防止過圖時任務管理器被銷毀
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        InventoryManager.OnInventoryChanged += OnInventoryChangedForQuest;
    }

    private void OnDisable()
    {
        InventoryManager.OnInventoryChanged -= OnInventoryChangedForQuest;
    }

    private void OnInventoryChangedForQuest()
    {
        SyncCollectObjectivesFromInventory();
    }

    private void Start()
    {
        EnsureQuestRegistrySeeded();
        if (TryLoadPersistedQuest())
            return;

        // 如果在 Inspector 中預先拉好了 Quest，但還沒初始化 Dictionary，則自動執行 Accept
        if (currentQuest != null && (objectiveProgress == null || objectiveProgress.Count == 0))
        {
            Debug.Log($"[QuestManager] 檢測到預設任務 {currentQuest.Title}，正在自動初始化...");
            AcceptQuest(currentQuest);
        }
        else if (currentQuest != null)
        {
            // 過場／讀檔後背包已變更，再對一次進度
            SyncCollectObjectivesFromInventory();
        }
        else
        {
            // 防呆：場景沒配置預設任務時仍保持空狀態
            OnQuestUpdated?.Invoke();
        }
    }

    void EnsureQuestRegistrySeeded()
    {
        if (questRegistry != null && questRegistry.Length > 0) return;
        if (currentQuest == null) return;

        // 無 Inspector 設定時，以 currentQuest -> NextQuest 鏈做最小可用註冊，
        // 避免跨場景/讀檔時找不到 QuestData 而被迫回退到場景預設任務。
        var list = new List<QuestData>();
        var seen = new HashSet<QuestData>();
        QuestData q = currentQuest;
        int guard = 0;
        while (q != null && guard++ < 32)
        {
            if (!seen.Add(q)) break;
            list.Add(q);
            q = q.NextQuest;
        }

        if (list.Count > 0)
            questRegistry = list.ToArray();
    }

    QuestData FindQuestById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (questRegistry != null)
        {
            foreach (var q in questRegistry)
            {
                if (q != null && q.ID == id)
                    return q;
            }
        }
        if (currentQuest != null && currentQuest.ID == id)
            return currentQuest;
        return null;
    }

    bool TryLoadPersistedQuest()
    {
        if (!persistQuestProgress) return false;

        string id = PlayerPrefs.GetString(QuestPlayerPrefsIdKey, "");
        if (string.IsNullOrEmpty(id)) return false;

        QuestData quest = FindQuestById(id);
        if (quest == null)
        {
            Debug.LogWarning($"[QuestManager] 存檔任務 ID「{id}」在 questRegistry 找不到對應 QuestData，略過讀檔。");
            return false;
        }

        string json = PlayerPrefs.GetString(QuestPlayerPrefsProgressKey, "");
        QuestProgressBlob blob = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<QuestProgressBlob>(json);

        AcceptQuestInternal(quest, blob);
        return true;
    }

    void PersistQuestProgress()
    {
        if (!persistQuestProgress) return;

        if (currentQuest == null || string.IsNullOrEmpty(currentQuest.ID))
        {
            PlayerPrefs.SetString(QuestPlayerPrefsIdKey, "");
            PlayerPrefs.SetString(QuestPlayerPrefsProgressKey, "");
            PlayerPrefs.Save();
            return;
        }

        var list = new List<QuestKv>(objectiveProgress.Count);
        foreach (var kvp in objectiveProgress)
            list.Add(new QuestKv { k = kvp.Key, v = kvp.Value });

        var blob = new QuestProgressBlob { entries = list.ToArray() };
        PlayerPrefs.SetString(QuestPlayerPrefsIdKey, currentQuest.ID);
        PlayerPrefs.SetString(QuestPlayerPrefsProgressKey, JsonUtility.ToJson(blob));
        PlayerPrefs.Save();
    }

    public void AcceptQuest(QuestData quest)
    {
        AcceptQuestInternal(quest, null);
    }

    void AcceptQuestInternal(QuestData quest, QuestProgressBlob fromSave)
    {
        if (quest == null) return;
        if (quest.Objectives == null)
        {
            Debug.LogWarning($"[QuestManager] 任務「{quest.Title}」的 Objectives 為空，已略過接任務。");
            return;
        }

        currentQuest = quest;
        objectiveProgress.Clear();

        foreach (var obj in currentQuest.Objectives)
        {
            if (!string.IsNullOrEmpty(obj.TargetKey))
            {
                if (!objectiveProgress.ContainsKey(obj.TargetKey))
                    objectiveProgress.Add(obj.TargetKey, 0);
            }
        }

        if (fromSave != null && fromSave.entries != null)
        {
            foreach (var e in fromSave.entries)
            {
                if (string.IsNullOrEmpty(e.k) || !objectiveProgress.ContainsKey(e.k)) continue;
                var obj = currentQuest.Objectives.FirstOrDefault(o => o.TargetKey == e.k);
                if (obj != null && obj.Type != QuestObjectiveType.CollectItem)
                    objectiveProgress[e.k] = e.v;
            }
        }

        Debug.Log($"[QuestManager] Accepted Quest: {quest.Title}");

        QuestData questRef = currentQuest;
        SyncCollectObjectivesFromInventory();
        if (currentQuest != questRef)
            return;

        OnQuestAccepted?.Invoke(currentQuest);
        OnQuestUpdated?.Invoke();
        PersistQuestProgress();
    }

    /// <summary>
    /// 以背包實際數量為準更新「收集類」目標（接任務前已持有的道具也會算入）。
    /// </summary>
    public void SyncCollectObjectivesFromInventory()
    {
        if (currentQuest == null || currentQuest.Objectives == null) return;
        var inv = InventoryManager.Instance;
        if (inv == null) return;

        bool changed = false;
        foreach (var obj in currentQuest.Objectives)
        {
            if (obj.Type != QuestObjectiveType.CollectItem || string.IsNullOrEmpty(obj.TargetKey))
                continue;

            if (!objectiveProgress.ContainsKey(obj.TargetKey))
                objectiveProgress[obj.TargetKey] = 0;

            var stack = inv.GetItemById(obj.TargetKey);
            int count = stack != null ? stack.quantity : 0;
            if (objectiveProgress[obj.TargetKey] != count)
            {
                objectiveProgress[obj.TargetKey] = count;
                changed = true;
                Debug.Log($"[QuestManager] 收集進度（來自背包） {obj.TargetKey}: {count} / {obj.RequiredAmount}");
            }
        }

        if (changed)
        {
            OnQuestUpdated?.Invoke();
            PersistQuestProgress();
        }

        if (AreAllObjectivesComplete())
            CompleteQuest();
    }

    // 撿起道具時改為依背包重算（與 Sync 一致，且修正只走 += 時與背包脫節）
    public void OnItemCollected(string itemID, int amountCollected)
    {
        if (currentQuest == null || currentQuest.Objectives == null || string.IsNullOrEmpty(itemID)) return;
        bool isCollectTarget = currentQuest.Objectives.Any(o =>
            o.Type == QuestObjectiveType.CollectItem && o.TargetKey == itemID);
        if (!isCollectTarget) return;

        SyncCollectObjectivesFromInventory();
    }

    // Generic update for other types (Kill, Talk, etc)
    public void UpdateObjectiveProgress(string targetKey, int amount)
    {
        if (currentQuest == null)
        {
            Debug.LogWarning($"[QuestManager] 嘗試更新進度 '{targetKey}' 但目前沒有進行中的任務！");
            return;
        }
        if (currentQuest.Objectives == null)
            return;

        if (!objectiveProgress.ContainsKey(targetKey))
        {
            Debug.LogWarning($"[QuestManager] 任務「{currentQuest.Title}」的目標清單沒有 TargetKey「{targetKey}」。請確認 QuestData 裡 TalkToNPC 的 TargetKey 與 NPCQuestHandler 的 npcId 完全一致（含大小寫）。");
            return;
        }

        objectiveProgress[targetKey] += amount;
        Debug.Log($"[QuestManager] {targetKey} 進度更新: {objectiveProgress[targetKey]} / {GetRequiredAmount(targetKey)}");
        PersistQuestProgress();
        OnQuestUpdated?.Invoke();

        if (AreAllObjectivesComplete())
        {
            CompleteQuest();
        }
    }

    private int GetRequiredAmount(string key)
    {
        if (currentQuest?.Objectives == null) return 999;
        var obj = currentQuest.Objectives.FirstOrDefault(x => x.TargetKey == key);
        return obj != null ? obj.RequiredAmount : 999;
    }

    // [Test] 方便在編輯器手動接任務測試
    public QuestData debugQuestToAccept;
    [ContextMenu("Debug Accept Quest")]
    public void DebugAcceptQuest()
    {
        if (debugQuestToAccept != null) AcceptQuest(debugQuestToAccept);
    }

    private bool AreAllObjectivesComplete()
    {
        if (currentQuest == null || currentQuest.Objectives == null) return false;

        foreach (var obj in currentQuest.Objectives)
        {
            int current = objectiveProgress.ContainsKey(obj.TargetKey) ? objectiveProgress[obj.TargetKey] : 0;
            if (current < obj.RequiredAmount)
            {
                return false;
            }
        }
        return true;
    }

    private void CompleteQuest()
    {
        QuestData completed = currentQuest;
        Debug.Log($"[QuestManager] Quest Completed: {completed.Title}!");

        OnQuestCompleted?.Invoke(completed);

        // 先交出收集類道具，再發獎勵（若獎勵與任務道具同 itemId，才拿得到獎勵）
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.RemoveItemsForCompletedQuestCollectObjectives(completed);

        if (completed.RewardItems != null)
        {
            foreach (var item in completed.RewardItems)
            {
                if (InventoryManager.Instance != null)
                    InventoryManager.Instance.AddItem(item.itemId, 1);
                Debug.Log($"[Reward] Got Item: {item.itemName}");
            }
        }
        
        if (completed.RewardGold > 0)
        {
             if (CoinManager.Instance != null)
                 CoinManager.Instance.AddCoins(completed.RewardGold);
             Debug.Log($"[Reward] Got Gold: {completed.RewardGold}");
        }

        QuestData next = completed.NextQuest;
        currentQuest = null;
        objectiveProgress.Clear();

        if (next == null)
            PersistQuestProgress();
        else
            AcceptQuest(next);
    }

    /// <summary>
    /// 由存檔槽 <see cref="SaveSlotData.ApplyToGame"/> 在還原背包之後呼叫，使任務與該槽快照一致。
    /// </summary>
    public void ApplyPersistedQuestFromPrefsAfterSaveSlotLoad()
    {
        if (!persistQuestProgress) return;

        string id = PlayerPrefs.GetString(QuestPlayerPrefsIdKey, "");
        if (string.IsNullOrEmpty(id))
        {
            currentQuest = null;
            objectiveProgress.Clear();
            OnQuestUpdated?.Invoke();
            return;
        }

        QuestData quest = FindQuestById(id);
        if (quest == null)
        {
            Debug.LogWarning($"[QuestManager] 存檔槽任務 ID「{id}」在 questRegistry 找不到，已清空任務狀態。");
            currentQuest = null;
            objectiveProgress.Clear();
            PlayerPrefs.SetString(QuestPlayerPrefsIdKey, "");
            PlayerPrefs.SetString(QuestPlayerPrefsProgressKey, "");
            PlayerPrefs.Save();
            OnQuestUpdated?.Invoke();
            return;
        }

        string json = PlayerPrefs.GetString(QuestPlayerPrefsProgressKey, "");
        QuestProgressBlob blob = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<QuestProgressBlob>(json);
        AcceptQuestInternal(quest, blob);
    }
}
