using UnityEngine;

public enum MaterialType
{
    None,
    海膽冷盤,
    鳳梨蝦球,
    蟹黃膏,
    三杯中卷,
    旗魚壽司,
    砂鍋魚頭,
    烏魚子,
    鯊魚翅
}

public class MaterialPickup : MonoBehaviour
{
    public MaterialType materialType;
    public int amount;
    public Sprite[] materialSprites; // Inspector 設定，順序需與 MaterialType 枚舉一致
    
    private ItemFloatEffect floatEffect;
    private bool picked;

    private void Start()
    {
        // 自動添加漂浮效果組件（如果還沒有的話）
        floatEffect = GetComponent<ItemFloatEffect>();
        if (floatEffect == null)
        {
            floatEffect = gameObject.AddComponent<ItemFloatEffect>();
        }
    }

    public void SetMaterial(MaterialType type, int amt)
    {
        materialType = type;
        amount = amt;
        // 根據 type 設定外觀
        if (materialSprites != null && (int)type < materialSprites.Length)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = materialSprites[(int)type];
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        TryPickup();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        TryPickup();
    }

    private void TryPickup()
    {
        if (picked) return;
        picked = true;

        string materialId = "mat_" + (int)materialType;
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(materialId, amount);

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnItemCollected(materialId, amount);

        AudioManager.GetInstance()?.PlayPickupItemSfx();

        if (PickupFeedbackManager.Instance != null)
        {
            string displayName = materialType.ToString();
            PickupFeedbackManager.Instance.ShowPickup(displayName, amount);
        }

        Destroy(gameObject);
    }
} 