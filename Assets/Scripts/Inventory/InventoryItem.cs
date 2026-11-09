using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ItemType { Material, Consumable, Weapon, Quest }

[System.Serializable]
public class InventoryItem
{
    public string itemId;
    public string itemName;
    public Sprite icon;
    public int quantity;
    public string description;
    public ItemType itemType;
    
    // 用於存檔冷卻時間 (System.DateTime.Now.Ticks)
    public long cooldownEndTicks;
}
