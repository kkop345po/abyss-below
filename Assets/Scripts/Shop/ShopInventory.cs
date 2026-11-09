using UnityEngine;

[CreateAssetMenu(menuName = "Shop/ShopInventory")]
public class ShopInventory : ScriptableObject
{
    public ShopItemData[] weapons;
    public ShopItemData[] items;
}