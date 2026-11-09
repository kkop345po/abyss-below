using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string itemId;
    public Sprite icon;
    public ItemType itemType; // 與 InventoryItem.cs 共用的 enum

    private Image dragImage;
    private Vector3 startPosition;
    private Transform startParent;
    private bool isDraggable = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDraggable = itemType != ItemType.Material && itemType != ItemType.Quest;
        if (!isDraggable)
            return;
        if (DragItemVisual.Instance == null)
        {
            isDraggable = false;
            return;
        }

        startPosition = transform.position;
        startParent = transform.parent;
        dragImage = DragItemVisual.Instance.Show(icon);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable)
            return;
        // 拖曳時的位置更新由 DragItemVisual 處理
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggable)
            return;

        DragItemVisual.Instance?.Hide();

        // 檢查是否放在裝備欄上
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            var equipSlot = result.gameObject.GetComponent<EquipmentSlotUI>();
            if (equipSlot != null)
            {
                // 檢查物品類型是否符合欄位類型
                bool canEquip = (equipSlot.slotType == EquipmentSlotUI.SlotType.Weapon1 || 
                               equipSlot.slotType == EquipmentSlotUI.SlotType.Weapon2) && 
                               itemType == ItemType.Weapon;

                canEquip |= equipSlot.slotType == EquipmentSlotUI.SlotType.Item && 
                           itemType == ItemType.Consumable;

                if (canEquip)
                {
                    // 觸發裝備欄的 OnDrop 事件
                    equipSlot.OnDrop(eventData);
                    return;
                }
            }
        }

        // 如果沒有裝備，回到原位
        transform.position = startPosition;
        transform.SetParent(startParent);
    }
}
