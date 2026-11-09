using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MaterialRow : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;
    public TextMeshProUGUI rateText;
    public TMP_InputField inputField;
    public Button exchangeButton;
    
    [Header("字體設定")]
    public TMP_FontAsset customFont; // 自定義字體

    private MaterialType materialType;
    private MaterialExchangeUI exchangeUI;

    public void Setup(MaterialType type, Sprite sprite, int count, int rate, MaterialExchangeUI ui)
    {
        materialType = type;
        icon.sprite = sprite;
        nameText.text = type.ToString();
        countText.text = $"持有：{count}";
        rateText.text = $"1個={rate}金幣";
        inputField.text = "1";
        exchangeUI = ui;

        // 套用自定義字體
        ApplyCustomFont();

        exchangeButton.onClick.RemoveAllListeners();
        exchangeButton.onClick.AddListener(OnExchangeClicked);
    }

    void OnExchangeClicked()
    {
        int amount = 1;
        int.TryParse(inputField.text, out amount);
        if (amount < 1) amount = 1;
        exchangeUI.ExchangeMaterial(materialType, amount);
    }

    public void UpdateCount(int count)
    {
        countText.text = $"持有：{count}";
    }

    private void ApplyCustomFont()
    {
        if (customFont != null)
        {
            // 套用到所有文字元件
            if (nameText != null) nameText.font = customFont;
            if (countText != null) countText.font = customFont;
            if (rateText != null) rateText.font = customFont;
            
            // 套用到輸入欄位的文字
            if (inputField != null && inputField.textComponent != null)
                inputField.textComponent.font = customFont;
        }
    }

    // 公共方法，允許外部設定字體
    public void SetFont(TMP_FontAsset font)
    {
        customFont = font;
        ApplyCustomFont();
    }
} 