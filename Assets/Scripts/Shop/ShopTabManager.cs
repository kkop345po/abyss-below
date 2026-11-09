using UnityEngine;

public class ShopTabManager : MonoBehaviour
{
    // public GameObject weaponPanel; // [Removed] 移除武器販賣
    public GameObject itemPanel;
    public GameObject shopPanel;
    public GameObject exchangePanel;

    // public void OpenWeaponTab() { ... } // [Removed]

    public void OpenItemTab()
    {
        // weaponPanel.SetActive(false);
        itemPanel.SetActive(true);
        if (exchangePanel != null) exchangePanel.SetActive(false);
    }

    public void OpenExchangeTab()
    {
        // weaponPanel.SetActive(false);
        itemPanel.SetActive(false);
        if (exchangePanel != null) exchangePanel.SetActive(true);
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }
}
