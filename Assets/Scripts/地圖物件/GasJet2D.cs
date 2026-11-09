using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GasJet2D : MonoBehaviour
{
    [Header("推力設定")]
    [Tooltip("推力大小（越大越容易把玩家推到牆上）")]
    public float force = 1500f;
    [Tooltip("噴氣持續時間（秒）")]
    public float duration = 1.2f;
    [Tooltip("施力模式：Force 為連續力，Impulse 為瞬間脈衝")]
    public ForceMode2D forceMode = ForceMode2D.Force;
    [Tooltip("力度隨時間變化的曲線（0~1 對應持續時間內的比例）")]
    public AnimationCurve forceOverTime = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("循環設定")]
    [Tooltip("噴發前的預備/充能時間（可在此播放預備動畫）")]
    public float prewind = 0.2f;
    [Tooltip("每次噴發之間的冷卻時間")]
    public float cooldown = 1.5f;
    [Tooltip("是否啟用自動循環")]
    public bool autoLoop = true;
    [Tooltip("啟用時首次延遲")]
    public float startDelay = 0f;

    [Header("過濾與方向")]
    [Tooltip("只影響這些 Layer（請把玩家層加入）")]
    public LayerMask affectedLayers;
    [Tooltip("忽略垂直分量，僅推水平")]
    public bool ignoreVertical = false;

    [Header("可視化對齊（動畫旋轉）")]
    [Tooltip("要對齊旋轉的可視化節點（通常是掛 Animator 的物件；留空則使用本物件）")]
    public Transform visualRoot;
    [Tooltip("是否將可視化朝向與噴射方向（transform.right）對齊")]
    public bool rotateVisualToJetDirection = true;
    [Tooltip("可視化額外角度偏移（度）")]
    public float visualAngleOffset = 0f;

    [Header("可視化顯示（待機隱藏）")]
    [Tooltip("待機時隱藏 visualRoot；噴發時自動顯示，結束後再隱藏")]
    public bool hideVisualWhenIdle = true;

    [Header("動畫（不含粒子）")]
    [Tooltip("動畫器，可在預備與噴發時觸發動畫參數")]
    public Animator animator;
    [Tooltip("預備觸發器名稱（留空則不觸發）")]
    public string prewindTrigger = "Prewind";
    [Tooltip("噴發觸發器名稱（留空則不觸發）")]
    public string fireTrigger = "Fire";

    // 內部狀態
    private readonly HashSet<Rigidbody2D> _targets = new HashSet<Rigidbody2D>();
    private readonly List<Rigidbody2D> _iterBuffer = new List<Rigidbody2D>(32);
    private Collider2D _triggerCol;
    private Coroutine _loopCo;
    // firing 狀態目前未被外部使用，移除避免 CS0414 警告

    void Awake()
    {
        _triggerCol = GetComponent<Collider2D>();
        _triggerCol.isTrigger = true;
        // 初始對齊一次
        if (rotateVisualToJetDirection)
        {
            AlignVisualToDirection();
        }
        // 初始隱藏可視化
        if (hideVisualWhenIdle)
        {
            SetVisualActive(false);
        }
    }

    void OnEnable()
    {
        if (autoLoop)
        {
            _loopCo = StartCoroutine(LoopRoutine());
        }
    }

    void OnDisable()
    {
        if (_loopCo != null) StopCoroutine(_loopCo);
        _loopCo = null;
        _targets.Clear();
        if (hideVisualWhenIdle)
        {
            SetVisualActive(false);
        }
    }

    IEnumerator LoopRoutine()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
        while (true)
        {
            yield return StartCoroutine(FireOnce());
            if (!autoLoop) yield break;
            if (cooldown > 0f) yield return new WaitForSeconds(cooldown);
        }
    }

    public IEnumerator FireOnce()
    {
        // 預備階段（可播預備動畫）
        if (prewind > 0f)
        {
            if (animator && !string.IsNullOrEmpty(prewindTrigger) && HasAnimatorParameter(animator, prewindTrigger))
                animator.SetTrigger(prewindTrigger);
            yield return new WaitForSeconds(prewind);
        }

        // 每次噴發前，將可視化朝向對齊噴射方向
        if (rotateVisualToJetDirection)
        {
            AlignVisualToDirection();
        }

        // 顯示可視化
        if (hideVisualWhenIdle)
        {
            SetVisualActive(true);
        }

        // 噴發動畫（保護：Animator 參數須存在）
        if (animator && !string.IsNullOrEmpty(fireTrigger) && HasAnimatorParameter(animator, fireTrigger))
        {
            animator.SetTrigger(fireTrigger);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;

            // 計算推力方向
            Vector2 dir = transform.right; // 以物件右方為噴發方向
            if (ignoreVertical) dir.y = 0f;
            if (dir.sqrMagnitude > 1e-4f) dir.Normalize();

            // 力度權重
            float t01 = duration > 1e-4f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float k = forceOverTime != null ? forceOverTime.Evaluate(t01) : 1f;

            // 以快照避免迭代期間集合變動
            _iterBuffer.Clear();
            _iterBuffer.AddRange(_targets);

            for (int i = 0; i < _iterBuffer.Count; i++)
            {
                var rb = _iterBuffer[i];
                if (!rb) continue;
                int layerMask = 1 << rb.gameObject.layer;
                if ((affectedLayers.value & layerMask) == 0) continue;

                // Force 模式為持續力；Impulse 模式為瞬發，但這裡仍可每幀加小脈衝視覺上較連續
                rb.AddForce(dir * (force * k), forceMode);
            }

            yield return new WaitForFixedUpdate();
        }

        // 結束
        // 噴發結束後隱藏
        if (hideVisualWhenIdle)
        {
            SetVisualActive(false);
        }
    }

    private static bool HasAnimatorParameter(Animator anim, string paramName)
    {
        if (anim == null || string.IsNullOrEmpty(paramName)) return false;
        var ps = anim.parameters;
        if (ps == null) return false;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i] != null && ps[i].name == paramName) return true;
        }
        return false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (!rb) return;
        int layerMask = 1 << other.gameObject.layer;
        if ((affectedLayers.value & layerMask) == 0) return;
        _targets.Add(rb);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (!rb) return;
        _targets.Remove(rb);
    }

    // 依 transform.right 設定可視化旋轉
    private void AlignVisualToDirection()
    {
        Transform target = visualRoot ? visualRoot : transform;
        Vector2 dir = transform.right;
        if (ignoreVertical) dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;
        dir.Normalize();
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + visualAngleOffset;
        target.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // 控制可視化顯示/隱藏，僅作用於 visualRoot
    private void SetVisualActive(bool isActive)
    {
        if (!visualRoot) return; // 建議把動畫與圖片放在子物件，並指定到 visualRoot
        if (visualRoot.gameObject.activeSelf != isActive)
        {
            visualRoot.gameObject.SetActive(isActive);
        }
    }

    // 在編輯器中調整參數時即時對齊，方便預覽
    void OnValidate()
    {
        if (rotateVisualToJetDirection)
        {
            AlignVisualToDirection();
        }
        if (hideVisualWhenIdle && !Application.isPlaying)
        {
            SetVisualActive(false);
        }
    }

    // Scene 視覺化：畫出方向
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 p = transform.position;
        Gizmos.DrawLine(p, p + transform.right * 2f);
        Gizmos.DrawWireSphere(p + transform.right * 2f, 0.1f);
    }
}


