using UnityEngine;

public class NormalEnemy : MonoBehaviour
{
	[Header("HP")]
	public int maxHP = 5;
	private int currentHP;

	[Header("成就／任務 (可選，空白則用物件名稱)")]
	public string enemyID;

	[Header("掉落")]
	public int coinDropAmount = 5;
	public GameObject coinPrefab;
	public MaterialType dropMaterialType = MaterialType.None;
	public int materialDropCount = 0;
	public GameObject materialPrefab;

	[Header("死亡效果")]
	public AudioClip deathSoundClip;
	public GameObject deathEffect;

	[Header("音效")]
	[Tooltip("怪物受傷時播放音效（預設：怪物受傷）。")]
	public bool playHurtSfxOnDamage = true;
	public string hurtSfxName = "殺魚";
	[Tooltip("同一隻怪在極短時間內連續被打到時，最少間隔多久才播一次（避免爆音）")]
	public float hurtSfxMinIntervalSeconds = 0.12f;
	private float _lastHurtSfxTime = -999f;
	[Tooltip("怪物死亡時播放音效（預設：怪物死）。")]
	public bool playDeathSfxOnDie = true;
	public string deathSfxName = "殺魚";

	[Header("受傷閃爍")]
	public SpriteRenderer[] flashRenderers;
	public Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
	public float flashInterval = 0.08f;
	public float flashDuration = 0.4f;
	private bool isFlashing = false;
	private bool isDead;

	void Awake()
	{
		// 一般怪物：統一受傷/死亡音效為「殺魚」
		playHurtSfxOnDamage = true;
		hurtSfxName = "殺魚";
		playDeathSfxOnDie = true;
		deathSfxName = "殺魚";

		// 在 currentHP 初始化前先套用平衡表（若 Resources/EnemyHpBalance.asset 存在）
		var bal = Resources.Load<EnemyHpBalanceAsset>("EnemyHpBalance");
		if (bal != null)
		{
			string id = !string.IsNullOrWhiteSpace(enemyID)
				? enemyID
				: gameObject.name.Replace("(Clone)", "").Trim();
			var tier = bal.ResolveTier(id);
			float ttk = bal.GetTtkSeconds(tier);

			int lvl = Mathf.Clamp(PlayerDataManager.WeaponUpgradeLevel, 0, WeaponUpgradeBalance.MaxLevel);
			float dmgMult = WeaponUpgradeBalance.GetDamageMultiplier(lvl);
			int projCount = WeaponUpgradeBalance.GetProjectileCount(lvl);
			float phase = WeaponUpgradeBalance.GetPhaseTimeScale(lvl);
			float cycle = Mathf.Max(0.2f, bal.fallbackAttackCycleSeconds) * Mathf.Max(0.1f, phase);
			float dps = (bal.baseBulletDamage * dmgMult * projCount) / cycle;
			float hp = dps * ttk * Mathf.Max(0.05f, bal.globalHpMultiplier);
			maxHP = Mathf.Max(10, Mathf.RoundToInt(hp));
		}

		currentHP = Mathf.Max(1, maxHP);
		if (string.IsNullOrWhiteSpace(enemyID))
			enemyID = gameObject.name.Replace("(Clone)", "").Trim();
	}

	public void TakeDamage(int damage)
	{
		if (isDead) return;
		currentHP -= Mathf.Max(1, damage);

		if (playHurtSfxOnDamage && !string.IsNullOrWhiteSpace(hurtSfxName))
		{
			if (Time.time - _lastHurtSfxTime >= Mathf.Max(0f, hurtSfxMinIntervalSeconds))
			{
				AudioManager.GetInstance()?.PlaySFX(hurtSfxName.Trim());
				_lastHurtSfxTime = Time.time;
			}
		}

		StartDamageFlash();
		if (currentHP <= 0)
		{
			Die();
		}
	}

	private void Die()
	{
		if (isDead) return;
		isDead = true;

		if (playDeathSfxOnDie && !string.IsNullOrWhiteSpace(deathSfxName))
			AudioManager.GetInstance()?.PlaySFX(deathSfxName.Trim());

		if (deathSoundClip != null)
		{
			AudioSource.PlayClipAtPoint(deathSoundClip, transform.position);
		}
		if (deathEffect != null)
		{
			Instantiate(deathEffect, transform.position, Quaternion.identity);
		}

		// 觸發相機震動
		if (CameraShake.Instance != null)
		{
			CameraShake.Instance.ShakeOnEnemyDeath();
		}

		if (!string.IsNullOrEmpty(enemyID))
		{
			QuestManager.Instance?.UpdateObjectiveProgress(enemyID, 1);
			AchievementManager.Instance?.AddProgress($"Kill_{enemyID}", 1);
		}
		AchievementManager.Instance?.AddProgress("TotalKills", 1);

		DropCoins();
		DropMaterials();

		Destroy(gameObject);
	}

	private void StartDamageFlash()
	{
		if (isFlashing) return;
		if (flashRenderers == null || flashRenderers.Length == 0)
			flashRenderers = GetComponentsInChildren<SpriteRenderer>();
		if (flashRenderers == null || flashRenderers.Length == 0) return;
		StartCoroutine(DamageFlashCoroutine());
	}

	private System.Collections.IEnumerator DamageFlashCoroutine()
	{
		isFlashing = true;
		float elapsed = 0f;
		var originals = new Color[flashRenderers.Length];
		for (int i = 0; i < flashRenderers.Length; i++) if (flashRenderers[i]) originals[i] = flashRenderers[i].color;
		while (elapsed < flashDuration)
		{
			for (int i = 0; i < flashRenderers.Length; i++) if (flashRenderers[i]) flashRenderers[i].color = flashColor;
			yield return new WaitForSeconds(flashInterval);
			for (int i = 0; i < flashRenderers.Length; i++) if (flashRenderers[i]) flashRenderers[i].color = originals[i];
			yield return new WaitForSeconds(flashInterval);
			elapsed += flashInterval * 2f;
		}
		for (int i = 0; i < flashRenderers.Length; i++) if (flashRenderers[i]) flashRenderers[i].color = originals[i];
		isFlashing = false;
	}

	private void DropCoins()
	{
		if (coinPrefab == null || coinDropAmount <= 0) return;
		GameObject coin = Instantiate(coinPrefab, transform.position, Quaternion.identity);
		CoinPickup pickup = coin.GetComponent<CoinPickup>();
		if (pickup != null) pickup.SetAmount(coinDropAmount);
	}

	private void DropMaterials()
	{
		if (materialPrefab == null || dropMaterialType == MaterialType.None || materialDropCount <= 0) return;
		for (int i = 0; i < materialDropCount; i++)
		{
			GameObject mat = Instantiate(materialPrefab, transform.position, Quaternion.identity);
			MaterialPickup pickup = mat.GetComponent<MaterialPickup>();
			if (pickup != null)
			{
				pickup.SetMaterial(dropMaterialType, 1);
			}
		}
	}
}


