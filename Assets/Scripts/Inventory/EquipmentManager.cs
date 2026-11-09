using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    public string equippedWeapon1 = "";
    public string equippedWeapon2 = "";
    public string equippedItem = "";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void EquipWeapon(int slotIndex, string weaponId)
    {
        if (slotIndex == 1) equippedWeapon1 = weaponId;
        else if (slotIndex == 2) equippedWeapon2 = weaponId;

        var pdc = FindObjectOfType<PlayerDisplayController>(true);
        pdc?.UpdateWeaponDisplay(weaponId);
    }

    public void EquipItem(string itemId)
    {
        equippedItem = itemId;
    }
}
