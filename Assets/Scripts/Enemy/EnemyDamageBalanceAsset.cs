using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵人對玩家的固定傷害表（放到 Resources/EnemyDamageBalance.asset 才會生效）。
/// Key 建議使用 BaseEnemy2D.enemyID；未填 enemyID 時會退回 Prefab/物件名稱（去掉 (Clone)）。
/// </summary>
[CreateAssetMenu(menuName = "DeepSea/Balance/Enemy Damage Balance", fileName = "EnemyDamageBalance")]
public class EnemyDamageBalanceAsset : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string id;
        public int damageToPlayer;
    }

    [Tooltip("找不到 id 時使用的預設值")]
    public int defaultDamageToPlayer = 5;

    public List<Entry> entries = new List<Entry>();

    private Dictionary<string, int> _map;

    public int ResolveDamage(string id, int fallback)
    {
        if (string.IsNullOrWhiteSpace(id))
            return fallback;

        EnsureMap();
        if (_map != null && _map.TryGetValue(id.Trim(), out int dmg))
            return Mathf.Max(0, dmg);

        return fallback;
    }

    public int ResolveDamageOrDefault(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Mathf.Max(0, defaultDamageToPlayer);

        EnsureMap();
        if (_map != null && _map.TryGetValue(id.Trim(), out int dmg))
            return Mathf.Max(0, dmg);

        return Mathf.Max(0, defaultDamageToPlayer);
    }

    private void EnsureMap()
    {
        if (_map != null) return;
        _map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrWhiteSpace(e.id)) continue;
            _map[e.id.Trim()] = Mathf.Max(0, e.damageToPlayer);
        }
    }
}

