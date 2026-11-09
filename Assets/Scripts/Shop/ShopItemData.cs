using UnityEngine;

public enum ShopItemType { Weapon, Item }

[CreateAssetMenu(menuName = "Shop/ShopItemData")]
public class ShopItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    public Sprite icon;
    public ShopItemType type;
    public int price;
    public string description;
}