using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabaseSO : ScriptableObject
{
    public List<ItemData> allItems;

    public ItemData GetItem(string id)
    {
        return allItems.Find(i => i.itemId == id);
    }
}
