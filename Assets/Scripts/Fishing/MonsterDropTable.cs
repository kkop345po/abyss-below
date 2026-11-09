using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>釣魚成功且 realBite 為 true 時的掉落表（對應海中怪物素材掉落邏輯）。</summary>
[CreateAssetMenu(fileName = "MonsterDropTable", menuName = "Fishing/Monster Drop Table")]
public class MonsterDropTable : ScriptableObject
{
    [Serializable]
    public class DropEntry
    {
        [Tooltip("直接指定 itemId（優先）")]
        public string itemId;

        [Tooltip("若 itemId 為空，使用 mat_ ＋ MaterialType 整數")]
        public MaterialType materialType = MaterialType.None;

        [Min(1)]
        public int amount = 1;

        [Min(0f)]
        public float weight = 1f;
    }

    [SerializeField] private List<DropEntry> entries = new List<DropEntry>();

    public DropEntry RollRandom()
    {
        if (entries == null || entries.Count == 0)
            return null;

        float total = 0f;
        foreach (var e in entries)
        {
            if (e == null) continue;
            total += Mathf.Max(0f, e.weight);
        }

        if (total <= 0f)
            return entries[UnityEngine.Random.Range(0, entries.Count)];

        float roll = UnityEngine.Random.Range(0f, total);
        float acc = 0f;
        foreach (var e in entries)
        {
            if (e == null) continue;
            acc += Mathf.Max(0f, e.weight);
            if (roll <= acc)
                return e;
        }

        return entries[entries.Count - 1];
    }

    /// <summary>解析實際要加入背包的 itemId。</summary>
    public static string ResolveItemId(DropEntry entry)
    {
        if (entry == null) return null;
        if (!string.IsNullOrWhiteSpace(entry.itemId))
            return entry.itemId.Trim();
        if (entry.materialType != MaterialType.None)
            return "mat_" + (int)entry.materialType;
        return null;
    }
}
