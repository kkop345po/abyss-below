using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChestType
{
	Placed,
	BossDrop
}

[RequireComponent(typeof(Animator))]
public class Chest : MonoBehaviour
{
	[Header("類型與動畫")]
	public ChestType chestType = ChestType.Placed;
	public Animator animator;
	public string openTriggerName = "Open";
	public float spawnDelayAfterOpen = 0.2f;

	[Header("互動設定")]
	public bool requireInteractKey = true;
	public KeyCode interactKey = KeyCode.E;
	public GameObject interactPromptUI;

	[Header("內容物（以 itemId 表示）")]
	public List<LootItem> lootItems = new List<LootItem>();

	[Header("跨場景持久化")]
	[Tooltip("留空則用「場景名＋物件名＋座標」自動產生。建議手動填唯一 ID，避免搬動物件後重複拿寶箱。")]
	public string uniqueChestId = "";

	[Header("生成設定")]
	public GameObject genericPickupPrefab; // 帶有 GenericPickup 的預製
	public Transform spawnPoint; // 物品生成點，未指定則用自身
	[Tooltip("多個戰利品時沿圓周散開，避免完全重疊")]
	public float lootSpawnSpreadRadius = 0.4f;
	
	[Header("浮動效果設定")]
	public bool useFloatingEffect = true; // 是否使用浮動效果
	public float floatingDuration = 1.5f; // 浮動時間
	public float floatingHeight = 2f; // 浮動高度
	public AnimationCurve floatingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 浮動曲線

	[Header("視覺")]
	[Tooltip("開啟寶箱時要隱藏的背光等物件（例如子物件 glow）。留空則自動在子階層尋找名為 glow 的物件。")]
	public GameObject backlightToHideOnOpen;

	private bool isOpened = false;
	private bool isPlayerInside = false;
	private string ResolvedId => string.IsNullOrEmpty(uniqueChestId) ? ChestPersistence.BuildDefaultId(this) : uniqueChestId;

	[System.Serializable]
	public class LootItem
	{
		public string itemId;
		public int amount = 1;
		public ShopItemData shopItemData;   // 可直接指定商店/道具資料
		public ItemType itemTypeOverride = ItemType.Material; // 任務道具請選 Quest
	}

	void Reset()
	{
		animator = GetComponent<Animator>();
	}

	void Start()
	{
		if (ChestPersistence.IsOpened(ResolvedId))
		{
			isOpened = true;
			gameObject.SetActive(false);
		}
	}

	public void Open()
	{
		if (isOpened) return;
		isOpened = true;
		ChestPersistence.MarkOpened(ResolvedId);
		HideBacklight();
		if (animator == null) animator = GetComponent<Animator>();
		if (animator != null && !string.IsNullOrEmpty(openTriggerName))
		{
			animator.SetTrigger(openTriggerName);
		}
        
        // 播放開啟寶箱音效
        if (AudioManager.GetInstance() != null) AudioManager.GetInstance().PlaySFX("發現寶物");
        
		StartCoroutine(SpawnLootAfterAnimation());
	}

	static Transform FindDescendantByNameIgnoreCase(Transform root, string targetName)
	{
		foreach (Transform child in root)
		{
			if (string.Equals(child.name, targetName, System.StringComparison.OrdinalIgnoreCase))
				return child;
			var nested = FindDescendantByNameIgnoreCase(child, targetName);
			if (nested != null)
				return nested;
		}
		return null;
	}

	void HideBacklight()
	{
		GameObject target = backlightToHideOnOpen;
		if (target == null)
		{
			var t = FindDescendantByNameIgnoreCase(transform, "glow");
			if (t != null)
				target = t.gameObject;
		}
		if (target != null && target.activeSelf)
			target.SetActive(false);
	}

	private IEnumerator SpawnLootAfterAnimation()
	{
		yield return new WaitForSeconds(spawnDelayAfterOpen);
		SpawnLoot();
	}

	private void SpawnLoot()
	{
		if (genericPickupPrefab == null)
		{
			Debug.LogWarning("[Chest] 未指定 genericPickupPrefab，無法生成內容物");
			return;
		}

		Transform point = spawnPoint != null ? spawnPoint : transform;
		int n = 0;
		int total = 0;
		foreach (var x in lootItems)
		{
			if (!string.IsNullOrEmpty(x.itemId)) total++;
		}

		foreach (var loot in lootItems)
		{
			if (string.IsNullOrEmpty(loot.itemId)) continue;

			Vector3 spawnPos = point.position;
			if (lootSpawnSpreadRadius > 0f && total > 1)
			{
				float angle = (n * (360f / total)) * Mathf.Deg2Rad;
				spawnPos += new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * lootSpawnSpreadRadius;
			}
			n++;

			GameObject obj = Instantiate(genericPickupPrefab, spawnPos, Quaternion.identity);
			GenericPickup pickup = obj.GetComponent<GenericPickup>();
			if (pickup != null)
			{
				pickup.itemId = loot.itemId;
				pickup.amount = Mathf.Max(1, loot.amount);
				pickup.shopItemData = loot.shopItemData;
				pickup.itemTypeOverride = loot.itemTypeOverride;
			}
			
			// 添加物理噴發效果
			Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
			if (rb != null)
			{
				// 隨機噴發方向 (類似爆炸)
				Vector2 randomDir = Random.insideUnitCircle.normalized;
				randomDir.y = Mathf.Abs(randomDir.y); // 偏向向上噴發
				float force = Random.Range(3f, 6f);
				rb.AddForce(randomDir * force, ForceMode2D.Impulse);
			}
		}
	}
	
	// [Removed] 移除舊的 FloatingEffect Coroutine，改用物理系統
	// private IEnumerator FloatingEffect(...) { ... }

	// 互動入口
	void OnTriggerEnter2D(Collider2D other)
	{
		Debug.Log($"[Chest] OnTriggerEnter2D: {other.tag}, isPlayer: {other.CompareTag("Player")}");
		if (!other.CompareTag("Player")) return;
		isPlayerInside = true;
		Debug.Log("[Chest] 玩家進入範圍，isPlayerInside = true");
		if (interactPromptUI != null)
		{
			interactPromptUI.SetActive(requireInteractKey);
			Debug.Log($"[Chest] 顯示提示按鈕: {requireInteractKey}");
			
			// 測試：強制設定為可見
			interactPromptUI.SetActive(true);
			Debug.Log("[Chest] 強制設定 UI 為可見");
		}
		else
		{
			Debug.LogWarning("[Chest] interactPromptUI 未指定，無法顯示提示");
		}
	}

	void OnTriggerExit2D(Collider2D other)
	{
		Debug.Log($"[Chest] OnTriggerExit2D: {other.tag}");
		if (!other.CompareTag("Player")) return;
		isPlayerInside = false;
		Debug.Log("[Chest] 玩家離開範圍，isPlayerInside = false");
		if (interactPromptUI != null)
			interactPromptUI.SetActive(false);
	}

	void Update()
	{
		if (requireInteractKey && isPlayerInside && !isOpened && Input.GetKeyDown(interactKey))
		{
			AudioManager.GetInstance()?.PlayButtonSfx();
			Open();
			if (interactPromptUI != null) interactPromptUI.SetActive(false);
		}
	}
}


