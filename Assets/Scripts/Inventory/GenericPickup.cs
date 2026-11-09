using UnityEngine;

public class GenericPickup : MonoBehaviour
{
	[Header("物品設定")]
	public string itemId;
	public int amount = 1;

	[Header("進階（可選）")]
	public ShopItemData shopItemData;
	[Tooltip("任務道具請選 Quest；與 ItemDatabase／任務道具庫的 itemType 一致時背包會出現在「素材」分頁（含任務類）")]
	public ItemType itemTypeOverride = ItemType.Material;

	private ItemFloatEffect floatEffect;
	private bool picked;

	private void Start()
	{
		Rigidbody2D rb = GetComponent<Rigidbody2D>();
		if (rb == null)
		{
			rb = gameObject.AddComponent<Rigidbody2D>();
			rb.gravityScale = 0.1f;
			rb.drag = 3f;
			rb.freezeRotation = true;
		}

		// 不再強制 isTrigger = false：與玩家 Trigger 碰撞時需 OnTriggerEnter2D；是否用 Trigger 由 Prefab／場景決定

		floatEffect = GetComponent<ItemFloatEffect>();
		if (floatEffect == null)
			floatEffect = gameObject.AddComponent<ItemFloatEffect>();
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

		if (InventoryManager.Instance != null)
		{
			string pickupName = itemId;
			bool addedSomething = false;

			if (shopItemData != null)
			{
				InventoryManager.Instance.AddItem(shopItemData, amount);
				pickupName = shopItemData.itemName;
				addedSomething = true;
			}
			else if (!string.IsNullOrEmpty(itemId))
			{
				InventoryManager.Instance.AddItem(itemId, amount);
				if (itemTypeOverride != ItemType.Material)
				{
					var added = InventoryManager.Instance.GetItemById(itemId);
					if (added != null) added.itemType = itemTypeOverride;
					InventoryManager.Instance.SaveInventory();
				}
				pickupName = itemId;
				addedSomething = true;
			}
			else
			{
				Debug.LogWarning("[GenericPickup] 未指定 itemId 或 shopItemData");
			}

			if (QuestManager.Instance != null)
			{
				string questItemKey = shopItemData != null ? shopItemData.itemId : itemId;
				if (!string.IsNullOrEmpty(questItemKey))
					QuestManager.Instance.OnItemCollected(questItemKey, amount);
			}

			if (PickupFeedbackManager.Instance != null)
			{
				string displayName = shopItemData != null
					? shopItemData.itemName
					: GetDisplayName(pickupName);
				PickupFeedbackManager.Instance.ShowPickup(displayName, amount);
			}

			if (addedSomething)
				AudioManager.GetInstance()?.PlayPickupItemSfx();
		}
		else
		{
			Debug.LogWarning($"[GenericPickup] 找不到 InventoryManager，無法新增 {(shopItemData != null ? shopItemData.itemId : itemId)}");
		}

		Destroy(gameObject);
	}

	private string GetDisplayName(string id)
	{
		if (id == "Coin" || id == "Gold") return "金幣";
		if (id == "Material") return "素材";
		if (id == "Meat") return "蟹肉";
		return id;
	}
}
