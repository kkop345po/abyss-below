using System.Collections;
using UnityEngine;

// 巫魚的投射物：命中玩家時施加持續傷害（DoT）
public class WitchProjectile : MonoBehaviour
{
    public float damagePerSecond = 3f;
    public float duration = 3f;
    public bool destroyOnHit = true;
    [Tooltip("飛行最久幾秒後自動消失（由巫魚 projectileLife 覆寫）")]
    public float maxLifetime = 3f;

    [Header("緩速（可選，由巫魚等發射端覆寫）")]
    [Tooltip("勾選後，命中時依機率對玩家施加移動緩速")]
    public bool enableSlowOnHit;
    [Range(0f, 1f)]
    [Tooltip("命中時觸發緩速的機率（0.3 = 30%）")]
    public float slowHitChance = 0.3f;
    [Tooltip("緩速持續秒數")]
    public float slowDuration = 2f;
    [Range(0.05f, 1f)]
    [Tooltip("緩速時移動速度乘數（0.5 = 移速減半）")]
    public float slowSpeedMultiplier = 0.5f;

    [Header("命中爆炸動畫（可選）")]
    [Tooltip("勾選且 Animator 有對應狀態時，撞到玩家後播爆炸再銷毀")]
    public bool playExplosionOnPlayerHit;
    [Tooltip("留空則自動抓本物件或子物件的 Animator")]
    public Animator projectileAnimator;
    [Tooltip("Animator 內爆炸狀態名稱（須與 Controller 一致）")]
    public string explosionStateName = "Explosion";
    [Tooltip("讀不到片段長度時使用的等待秒數")]
    public float explosionFallbackDuration = 0.35f;

    [Header("旋轉設定")]
    public bool autoRotate = true;
    public float rotationOffset = 0f;

    private bool _hitHandled;
    private Coroutine _lifetimeCoroutine;
    private Rigidbody2D _rb;
    private Collider2D _collider;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        if (projectileAnimator == null)
            projectileAnimator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
    }

    void Start()
    {
        if (_rb != null)
            _rb.gravityScale = 0f;

        if (autoRotate && _rb != null && _rb.velocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(_rb.velocity.y, _rb.velocity.x) * Mathf.Rad2Deg + rotationOffset;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        if (maxLifetime > 0f)
            _lifetimeCoroutine = StartCoroutine(LifetimeTimeout());
    }

    private IEnumerator LifetimeTimeout()
    {
        yield return new WaitForSeconds(maxLifetime);
        if (!_hitHandled)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hitHandled) return;
        if (!other.CompareTag("Player")) return;

        var dot = other.GetComponent<PlayerDamageOverTime>()
                  ?? other.GetComponentInParent<PlayerDamageOverTime>();
        if (dot != null)
            dot.Apply(damagePerSecond, duration);

        TryApplySlow(other);

        if (TryPlayExplosionOnHit())
            return;

        if (destroyOnHit)
            Destroy(gameObject);
    }

    private bool TryPlayExplosionOnHit()
    {
        if (!playExplosionOnPlayerHit || projectileAnimator == null)
            return false;
        if (string.IsNullOrWhiteSpace(explosionStateName))
            return false;

        _hitHandled = true;
        if (_lifetimeCoroutine != null)
        {
            StopCoroutine(_lifetimeCoroutine);
            _lifetimeCoroutine = null;
        }

        FreezeProjectile();
        StartCoroutine(PlayExplosionThenDestroy());
        return true;
    }

    private void FreezeProjectile()
    {
        if (_rb != null)
        {
            _rb.velocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.simulated = false;
        }

        if (_collider != null)
            _collider.enabled = false;
    }

    private IEnumerator PlayExplosionThenDestroy()
    {
        int layer = 0;
        projectileAnimator.Play(explosionStateName.Trim(), layer, 0f);

        float wait = explosionFallbackDuration;
        yield return null;
        var state = projectileAnimator.GetCurrentAnimatorStateInfo(layer);
        if (state.length > 0.01f)
            wait = state.length;

        yield return new WaitForSeconds(wait);

        if (destroyOnHit)
            Destroy(gameObject);
    }

    private void TryApplySlow(Collider2D other)
    {
        if (!enableSlowOnHit || slowHitChance <= 0f || slowDuration <= 0f)
            return;
        if (Random.value >= slowHitChance)
            return;

        var slow = other.GetComponent<PlayerSlow>() ?? other.GetComponentInParent<PlayerSlow>();
        if (slow == null)
        {
            var root = other.transform.root;
            slow = root.GetComponent<PlayerSlow>();
            if (slow == null)
                slow = root.gameObject.AddComponent<PlayerSlow>();
        }

        slow.ApplySlow(slowDuration, slowSpeedMultiplier);
    }
}
