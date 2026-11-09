using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    private ShopItemData itemData;
    private ShopUIController shop;

    public void Setup(ShopItemData data, ShopUIController controller)
    {
        itemData = data;
        shop = controller;

        icon.sprite = data.icon;
        nameText.text = data.itemName;
        priceText.text = data.price + " 金幣";

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void OnBuyClicked()
    {
        shop.AttemptBuy(itemData);
    }
}
