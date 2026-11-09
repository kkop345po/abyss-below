using UnityEngine;

[System.Serializable]
public class ItemData
{
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemType itemType;
    public int maxStack = 99;
}
