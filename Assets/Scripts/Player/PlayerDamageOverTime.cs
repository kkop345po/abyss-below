using UnityEngine;

public class PlayerDamageOverTime : MonoBehaviour
{
    [Header("持續傷害狀態")]
    public bool isActive;
    public float damagePerSecond = 3f;
    public float remainingSeconds;
    [Tooltip("每隔幾秒扣一次血（愈大愈慢）；實際 DPS ≈ 每跳傷害 / 間隔。")]
    public float damageTickInterval = 0.85f;

    private PlayerHealth _playerHealth;
    private float _tickTimer;

    void Awake()
    {
        ResolvePlayerHealth();
    }

    void ResolvePlayerHealth()
    {
        if (_playerHealth != null) return;
        _playerHealth = GetComponent<PlayerHealth>()
            ?? GetComponentInParent<PlayerHealth>()
            ?? GetComponentInChildren<PlayerHealth>(true);
    }

    public void Apply(float dps, float duration)
    {
        if (duration <= 0f || dps <= 0f) return;
        damagePerSecond = Mathf.Max(damagePerSecond, dps);
        remainingSeconds = Mathf.Max(remainingSeconds, duration);
        isActive = true;
        _tickTimer = 0f;
    }

    // 公開 API：清除目前持續傷害/負面
    public void Clear()
    {
        isActive = false;
        remainingSeconds = 0f;
        _tickTimer = 0f;
    }

    void Update()
    {
        if (!isActive) return;
        if (_playerHealth == null)
            ResolvePlayerHealth();
        if (_playerHealth == null) return;

        remainingSeconds -= Time.deltaTime;

        float interval = Mathf.Max(0.25f, damageTickInterval);
        _tickTimer -= Time.deltaTime;
        if (_tickTimer <= 0f)
        {
            int damagePoints = Mathf.Max(1, Mathf.RoundToInt(damagePerSecond * interval));
            _playerHealth.TakeDamage(damagePoints, ignoreInvincibility: true);
            _tickTimer = interval;
        }

        if (remainingSeconds <= 0f)
        {
            isActive = false;
            remainingSeconds = 0f;
            _tickTimer = 0f;
        }
    }
}


