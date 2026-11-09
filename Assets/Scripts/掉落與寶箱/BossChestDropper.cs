using UnityEngine;

public class BossChestDropper : MonoBehaviour
{
	[Header("掉落寶箱設定")]
	public GameObject chestPrefab; // 必須掛有 Chest
	public Vector2 dropOffset = new Vector2(0f, 0.2f);

	[Header("預設掉落內容（可選）")]
	public Chest.LootItem[] defaultLoot;

	public void DropChest()
	{
		if (chestPrefab == null)
		{
			Debug.LogWarning("[BossChestDropper] 未指定 chestPrefab");
			return;
		}
		Vector3 pos = transform.position + (Vector3)dropOffset;
		GameObject chestObj = Instantiate(chestPrefab, pos, Quaternion.identity);
		Chest chest = chestObj.GetComponent<Chest>();
		if (chest != null && defaultLoot != null && defaultLoot.Length > 0)
		{
			chest.lootItems.Clear();
			foreach (var li in defaultLoot)
			{
				if (li == null || string.IsNullOrEmpty(li.itemId)) continue;
				chest.lootItems.Add(new Chest.LootItem { itemId = li.itemId, amount = Mathf.Max(1, li.amount) });
			}
			chest.chestType = ChestType.BossDrop;
		}
	}
}


