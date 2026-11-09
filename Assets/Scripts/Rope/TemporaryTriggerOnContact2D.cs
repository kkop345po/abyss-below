using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TemporaryTriggerOnContact2D : MonoBehaviour
{
    [Header("Timing")]
    [Min(0f)] public float triggerDuration = 1f; // 變成 Trigger 的持續時間

    [Header("Scope")]
    public bool includeChildren = true;          // 是否包含子物件的 Collider2D 一起切換

    [Header("Filter")] 
    public LayerMask reactLayers = ~0;           // 只對這些 Layer 的接觸做出反應（預設全部）
    public bool reactOnCollision = true;         // 對 OnCollisionEnter2D 反應
    public bool reactOnTrigger = false;          // 對 OnTriggerEnter2D 反應（例如某些特殊判定）

    private List<Collider2D> _cachedColliders;
    private Coroutine _running;

    void Awake()
    {
        _cachedColliders = new List<Collider2D>();
        if (includeChildren)
        {
            GetComponentsInChildren(true, _cachedColliders);
        }
        else
        {
            var c = GetComponent<Collider2D>();
            if (c != null) _cachedColliders.Add(c);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!reactOnCollision) return;
        if (((1 << collision.gameObject.layer) & reactLayers) == 0) return;
        TriggerTemporarily();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!reactOnTrigger) return;
        if (((1 << other.gameObject.layer) & reactLayers) == 0) return;
        TriggerTemporarily();
    }

    public void TriggerTemporarily()
    {
        if (_running != null)
        {
            StopCoroutine(_running);
        }
        _running = StartCoroutine(SwitchToTriggerThenRestore());
    }

    private IEnumerator SwitchToTriggerThenRestore()
    {
        if (_cachedColliders == null || _cachedColliders.Count == 0)
        {
            yield break;
        }

        var originals = new bool[_cachedColliders.Count];
        for (int i = 0; i < _cachedColliders.Count; i++)
        {
            var col = _cachedColliders[i];
            if (col == null) continue;
            originals[i] = col.isTrigger;
            col.isTrigger = true;
        }

        if (triggerDuration > 0f)
        {
            yield return new WaitForSeconds(triggerDuration);
        }

        for (int i = 0; i < _cachedColliders.Count; i++)
        {
            var col = _cachedColliders[i];
            if (col == null) continue;
            col.isTrigger = originals[i];
        }

        _running = null;
    }
}


