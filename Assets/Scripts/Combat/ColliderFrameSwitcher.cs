using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 以「多顆 Collider 切換」方式做逐幀碰撞（最適合 Hitbox/Hurtbox）。
/// 用法：在 Boss 底下建立子物件 Hurtboxes，底下放 HB_0、HB_1... 各自掛 PolygonCollider2D (isTrigger)。
/// 本腳本掛在 Hurtboxes 上，會自動收集並依 HB_索引排序，供 Animation Event 呼叫 ApplyFrame(frameIndex)。
/// </summary>
public class ColliderFrameSwitcher : MonoBehaviour
{
    [Header("Auto Collect")]
    [Tooltip("若勾選，Awake/OnValidate 會自動抓取子物件上的 PolygonCollider2D，並依 HB_0..n 排序。")]
    [SerializeField] private bool autoCollectFromChildren = true;

    [Tooltip("只收集名稱符合此格式的子物件 Collider。預設：HB_0、HB_1...")]
    [SerializeField] private string childNamePrefix = "HB_";

    [Header("Default")]
    [SerializeField] private int defaultFrame = 0;
    [Tooltip("啟動時是否套用 defaultFrame。")]
    [SerializeField] private bool applyDefaultOnAwake = true;

    [Header("Debug")]
    [SerializeField] private bool logMissingFrames;

    [NonSerialized] public PolygonCollider2D[] frames;
    private int _current = -1;

    static int ExtractIndex(string name, string prefix)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(prefix)) return int.MaxValue;
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return int.MaxValue;
        string tail = name.Substring(prefix.Length);
        if (int.TryParse(tail, out int idx)) return idx;
        // 支援像 "HB_0 (xxx)" 這類情況
        var m = Regex.Match(tail, @"(\d+)");
        if (m.Success && int.TryParse(m.Groups[1].Value, out idx)) return idx;
        return int.MaxValue;
    }

    void Awake()
    {
        if (autoCollectFromChildren)
            CollectFrames();
        if (applyDefaultOnAwake)
            ApplyFrame(defaultFrame);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!autoCollectFromChildren) return;
        CollectFrames();
    }
#endif

    [ContextMenu("Collect Frames")]
    public void CollectFrames()
    {
        var cols = GetComponentsInChildren<PolygonCollider2D>(true);
        var list = new List<PolygonCollider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (c == null) continue;
            if (c.transform == transform) continue; // 忽略自己身上的
            if (!string.IsNullOrEmpty(childNamePrefix) &&
                !c.gameObject.name.StartsWith(childNamePrefix, StringComparison.OrdinalIgnoreCase))
                continue;
            list.Add(c);
        }

        list.Sort((a, b) =>
            ExtractIndex(a.gameObject.name, childNamePrefix).CompareTo(ExtractIndex(b.gameObject.name, childNamePrefix)));

        frames = list.ToArray();
    }

    /// <summary>Animation Event 呼叫：切換到指定幀。</summary>
    public void ApplyFrame(int frameIndex)
    {
        if (frames == null || frames.Length == 0)
        {
            if (logMissingFrames) Debug.LogWarning("[ColliderFrameSwitcher] frames 為空，請先 Collect 或確認 HB_* 子物件。", this);
            return;
        }

        frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
        if (_current == frameIndex) return;

        for (int i = 0; i < frames.Length; i++)
        {
            var c = frames[i];
            if (c == null) continue;
            c.enabled = (i == frameIndex);
        }

        _current = frameIndex;
    }

    /// <summary>Animation Event 呼叫：全部關閉（無敵幀／不可受傷）。</summary>
    public void DisableAll()
    {
        if (frames == null) return;
        for (int i = 0; i < frames.Length; i++)
        {
            var c = frames[i];
            if (c == null) continue;
            c.enabled = false;
        }
        _current = -1;
    }
}

