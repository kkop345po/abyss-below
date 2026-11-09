using UnityEngine;

/// <summary>
/// 玩家移動緩速（與麻痺不同：仍可移動，僅降低移速倍率）。
/// </summary>
public class PlayerSlow : MonoBehaviour
{
    [Tooltip("緩速生效時的移動速度乘數（0.5 = 移速減半）")]
    [Range(0.05f, 1f)]
    public float slowedSpeedMultiplier = 0.5f;

    private float _remainingSeconds;

    public bool IsSlowed => _remainingSeconds > 0f;

    public float CurrentSpeedMultiplier => IsSlowed ? slowedSpeedMultiplier : 1f;

    /// <summary>施加或刷新緩速；較長剩餘時間會保留。</summary>
    public bool ApplySlow(float durationSeconds, float speedMultiplier)
    {
        if (durationSeconds <= 0f)
            return false;

        speedMultiplier = Mathf.Clamp(speedMultiplier, 0.05f, 1f);
        if (speedMultiplier >= 0.999f)
            return false;

        slowedSpeedMultiplier = speedMultiplier;
        _remainingSeconds = Mathf.Max(_remainingSeconds, durationSeconds);
        return true;
    }

    private void Update()
    {
        if (_remainingSeconds <= 0f)
            return;
        _remainingSeconds = Mathf.Max(0f, _remainingSeconds - Time.deltaTime);
    }
}
