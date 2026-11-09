using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestItemDatabase", menuName = "Quest System/Quest Item Database")]
public class QuestItemDatabaseSO : ScriptableObject
{
    [Header("任務專用道具庫")]
    [Tooltip("這裡只存放任務相關的關鍵道具，例如：C4, 雷達等")]
    public List<ItemData> questItems = new List<ItemData>();

    /// <summary>
    /// 透過 itemId 取得任務道具資料
    /// </summary>
    public ItemData GetQuestItem(string id)
    {
        return questItems.Find(i => i.itemId == id);
    }

    /// <summary>
    /// 檢查是否包含特定任務道具
    /// </summary>
    public bool ContainsItem(string id)
    {
        return questItems.Exists(i => i.itemId == id);
    }
}
