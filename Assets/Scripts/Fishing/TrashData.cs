using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>釣魚垃圾池：QTE 失敗時不會用到；realBite 為 false 時從此隨機抽取。</summary>
[CreateAssetMenu(fileName = "TrashData", menuName = "Fishing/Trash Data")]
public class TrashData : ScriptableObject
{
    [Serializable]
    public class TrashEntry
    {
        [Tooltip("顯示名稱，例如：核廢料桶")]
        public string displayName;

        public Sprite icon;

        [Tooltip("勾選後 QTE 時間縮為 0.6 秒，相機震動強度加倍")]
        public bool isNuclearWasteBarrel;

        [Tooltip("加入背包用的 itemId；留空則使用 displayName")]
        public string inventoryItemId;

        [Min(0f)]
        public float weight = 1f;
    }

    [SerializeField] private List<TrashEntry> entries = new List<TrashEntry>();

    public IReadOnlyList<TrashEntry> Entries => entries;

    public TrashEntry RollRandom()
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
}
