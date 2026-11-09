using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string nameText;
    private string description;
    private InventoryUIController ui;
    private Sprite icon;

    public void Setup(string name, string desc, Sprite sprite, InventoryUIController controller)
    {
        nameText = name;
        description = desc;
        icon = sprite;
        ui = controller;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ui.ShowTooltip(nameText, description, icon);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ui.HideTooltip();
    }
}
