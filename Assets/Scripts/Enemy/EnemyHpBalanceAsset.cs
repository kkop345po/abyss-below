using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 怪物血量平衡表：以「玩家輸出」推導出合理 HP（讓擊殺時間穩定）。
/// 放在 Resources 並命名為 EnemyHpBalance（Resources/EnemyHpBalance.asset）即可自動生效。
/// </summary>
[CreateAssetMenu(menuName = "DeepSea/Enemy/HP Balance Asset", fileName = "EnemyHpBalance")]
public class EnemyHpBalanceAsset : ScriptableObject
{
    public enum EnemyTier
    {
        Small,
        Medium,
        Large
    }

    [System.Serializable]
    public class NameTierRule
    {
        [Tooltip("只要 enemyID 或 GameObject 名稱包含此關鍵字，就歸類到 tier。")]
        public string contains;
        public EnemyTier tier = EnemyTier.Small;
    }

    [Header("玩家輸出估算（以此推導怪物 HP）")]
    [Tooltip("玩家子彈預製體的基礎傷害（你目前 Bullet 預設是 1；若之後改了，這裡也改）。")]
    public int baseBulletDamage = 1;

    [Tooltip("玩家一次攻擊週期的秒數估算（秒）。會用 PlayerController：max(attackCooldown, attackDuration+reloadDuration) * phaseScale。")]
    public float fallbackAttackCycleSeconds = 1.2f;

    [Header("擊殺時間（秒）")]
    [Tooltip("以武器 Lv0 的體感：小怪被點掉的時間。")]
    public float ttkSmallSeconds = 2.0f;
    public float ttkMediumSeconds = 4.5f;
    public float ttkLargeSeconds = 8.0f;

    [Header("安全係數")]
    [Tooltip("全體怪物 HP 乘上此值（用來微調整體難度）。")]
    public float globalHpMultiplier = 1.0f;

    [Header("名稱 → 分級規則（可自行增改）")]
    public List<NameTierRule> nameTierRules = new List<NameTierRule>
    {
        new NameTierRule { contains = "孩膽", tier = EnemyTier.Small },
        new NameTierRule { contains = "蝦子", tier = EnemyTier.Small },
        new NameTierRule { contains = "后里蟹", tier = EnemyTier.Small },
        new NameTierRule { contains = "旗魚", tier = EnemyTier.Medium },
        new NameTierRule { contains = "中卷", tier = EnemyTier.Medium },
        new NameTierRule { contains = "砂鍋魚頭", tier = EnemyTier.Medium },
        new NameTierRule { contains = "巫魚", tier = EnemyTier.Large },
        new NameTierRule { contains = "素瘋魚", tier = EnemyTier.Large },
    };

    public EnemyTier ResolveTier(string enemyIdOrName)
    {
        if (string.IsNullOrWhiteSpace(enemyIdOrName)) return EnemyTier.Small;
        foreach (var r in nameTierRules)
        {
            if (r == null) continue;
            if (string.IsNullOrWhiteSpace(r.contains)) continue;
            if (enemyIdOrName.Contains(r.contains))
                return r.tier;
        }
        return EnemyTier.Small;
    }

    public float GetTtkSeconds(EnemyTier tier)
    {
        return tier switch
        {
            EnemyTier.Small => Mathf.Max(0.2f, ttkSmallSeconds),
            EnemyTier.Medium => Mathf.Max(0.2f, ttkMediumSeconds),
            EnemyTier.Large => Mathf.Max(0.2f, ttkLargeSeconds),
            _ => Mathf.Max(0.2f, ttkSmallSeconds),
        };
    }
}

