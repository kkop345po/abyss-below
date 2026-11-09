using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class MaterialExchangeUI : MonoBehaviour
{
    public MaterialRow materialRowPrefab;
    public Transform contentParent; // 指向 ScrollView/Viewport/Content
    public Sprite[] materialSprites; // 8種素材圖
    public int[] materialToGoldRates; // Inspector設置每種素材兌換比率
    public TextMeshProUGUI messageText; // UI提示文字
    [Header("資料來源（可選）")]
    public MaterialDatabase materialDatabase; // 若提供，將使用資料庫的名稱與圖示
    
    [Header("字體設定")]
    public TMP_FontAsset rowFont; // 素材行的字體

    [Header("金幣顯示／兌換入帳演出（可選，與道具商店相同元件）")]
    [Tooltip("未掛 CoinSpendFeedbackView 時，可單獨指定 TMP 顯示餘額並隨 CoinsChanged 更新")]
    [SerializeField] private TextMeshProUGUI coinBalanceText;
    [SerializeField] private CoinSpendFeedbackView coinSpendFeedback;

    private List<MaterialRow> rows = new List<MaterialRow>();

    void OnEnable()
    {
        if (coinSpendFeedback == null)
            coinSpendFeedback = GetComponentInChildren<CoinSpendFeedbackView>(true);
        if (coinSpendFeedback == null && coinBalanceText != null)
            PlayerDataManager.CoinsChanged += OnMaterialShopCoinsChanged;
        if (coinSpendFeedback != null && coinBalanceText != null)
            coinSpendFeedback.SetExternalBalanceMirror(coinBalanceText);
        coinSpendFeedback?.RefreshBalanceText();
        if (coinSpendFeedback == null && coinBalanceText != null)
            coinBalanceText.text = PlayerDataManager.Coins.ToString();
        RefreshUI();
        if (messageText != null) messageText.text = "";
    }

    void OnDisable()
    {
        coinSpendFeedback?.SetExternalBalanceMirror(null);
        PlayerDataManager.CoinsChanged -= OnMaterialShopCoinsChanged;
    }

    void OnMaterialShopCoinsChanged(int c)
    {
        if (coinBalanceText != null)
            coinBalanceText.text = c.ToString();
    }

    public void RefreshUI()
    {
        foreach (var row in rows) Destroy(row.gameObject);
        rows.Clear();

        for (int i = 1; i < materialToGoldRates.Length; i++) // 從1開始，0是None
        {
            var type = (MaterialType)i;
            int count = GetMaterialCount(type);
            // 背包沒有該素材就不顯示
            if (count <= 0) continue;

            // 取得圖示與顯示名稱（優先用資料庫）
            Sprite icon = (materialSprites != null && i < materialSprites.Length) ? materialSprites[i] : null;
            string displayName = type.ToString();
            if (materialDatabase != null)
            {
                var data = materialDatabase.GetMaterial($"mat_{i}");
                if (data != null)
                {
                    if (data.icon != null) icon = data.icon;
                    if (!string.IsNullOrEmpty(data.materialName)) displayName = data.materialName;
                }
            }

            var row = Instantiate(materialRowPrefab, contentParent);
            row.Setup(type, icon, count, materialToGoldRates[i], this);
            if (!string.IsNullOrEmpty(displayName))
            {
                row.nameText.text = displayName;
            }
            
            // 套用字體設定
            if (rowFont != null)
                row.SetFont(rowFont);
            
            rows.Add(row);
        }
    }

    public void ExchangeMaterial(MaterialType type, int amount)
    {
        if (RemoveMaterial(type, amount))
        {
            int gold = materialToGoldRates[(int)type] * amount;
            if (coinSpendFeedback != null)
                coinSpendFeedback.SetAnimLock(true);
            int coinsBefore = PlayerDataManager.Coins;
            if (CoinManager.Instance != null)
                CoinManager.Instance.AddCoin(gold);
            else
            {
                PlayerDataManager.Coins += gold;
                SaveSystem.SavePlayerData();
            }
            if (coinSpendFeedback != null)
                coinSpendFeedback.RunGainPresentation(coinsBefore, gold, this);
            ShowMessage($"兌換 {type} x{amount}，獲得金幣：{gold}");
            RefreshUI();
        }
        else
        {
            ShowMessage("素材不足！");
        }
    }

    private void ShowMessage(string msg)
    {
        if (messageText != null)
        {
            messageText.text = msg;
            CancelInvoke(nameof(ClearMessage));
            Invoke(nameof(ClearMessage), 2f);
        }
    }

    private void ClearMessage()
    {
        if (messageText != null) messageText.text = "";
    }

    // 素材管理方法
    private int GetMaterialCount(MaterialType type)
    {
        return InventoryManager.Instance != null ? InventoryManager.Instance.GetMaterialCount(type) : 0;
    }

    private bool RemoveMaterial(MaterialType type, int amount)
    {
        return InventoryManager.Instance != null && InventoryManager.Instance.RemoveMaterial(type, amount);
    }
} 