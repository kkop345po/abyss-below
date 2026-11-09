using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public enum SlotType { Weapon1, Weapon2, Item }
    public SlotType slotType;

    public Image iconImage;
    private string equippedId;
    private string equippedName;

    public void OnDrop(PointerEventData eventData)
    {
        DraggableItem dragged = eventData.pointerDrag?.GetComponent<DraggableItem>();
        if (dragged == null) return;
        if (InventoryManager.Instance == null) return;

        InventoryItem item = InventoryManager.Instance.GetItemById(dragged.itemId);
        if (item == null) return;

        if ((slotType == SlotType.Weapon1 || slotType == SlotType.Weapon2) && item.itemType != ItemType.Weapon)
        {
            Debug.LogWarning("?o???O?Z???A?????b?o??I");
            return;
        }

        if (slotType == SlotType.Item && item.itemType != ItemType.Consumable)
        {
            Debug.LogWarning("?o???O?i??£dD??I");
            return;
        }

        iconImage.sprite = dragged.icon;
        iconImage.color = Color.white;
        equippedId = dragged.itemId;
        equippedName = item.itemName;

        // ?O?????
        switch (slotType)
        {
            case SlotType.Weapon1:
                PlayerDataManager.EquippedWeapon1 = dragged.itemId;
                break;
            case SlotType.Weapon2:
                PlayerDataManager.EquippedWeapon2 = dragged.itemId;
                break;
            case SlotType.Item:
                PlayerDataManager.EquippedItem = dragged.itemId;
                break;
        }
        PlayerDataManager.Save();

        // ??s????~?[
        FindObjectOfType<PlayerDisplayController>()?.UpdateWeaponDisplay(dragged.itemId);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && !string.IsNullOrEmpty(equippedId))
        {
            if (InventoryManager.Instance == null) return;
            var item = InventoryManager.Instance.GetItemById(equippedId);
            if (item != null)
            {
                InventoryManager.Instance.AddItem(new InventoryItem
                {
                    itemId = item.itemId,
                    itemName = item.itemName,
                    icon = item.icon,
                    description = item.description,
                    itemType = item.itemType,
                    quantity = 1
                });
            }

            iconImage.sprite = null;
            iconImage.color = new Color(1, 1, 1, 0);
            equippedId = "";

            // ?M???O??
            switch (slotType)
            {
                case SlotType.Weapon1: PlayerDataManager.EquippedWeapon1 = ""; break;
                case SlotType.Weapon2: PlayerDataManager.EquippedWeapon2 = ""; break;
                case SlotType.Item: PlayerDataManager.EquippedItem = ""; break;
            }
            PlayerDataManager.Save();
            FindObjectOfType<PlayerDisplayController>()?.UpdateWeaponDisplay("");
        }
    }
}