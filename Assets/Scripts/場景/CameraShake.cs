using System.Collections;
using UnityEngine;

/// <summary>
/// 相機震動效果：用於怪物死亡、Boss 攻擊等。與 CameraFollowClamped 兼容。
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    CameraFollowClamped _cameraFollow;
    Coroutine _impulseRoutine;
    Coroutine _sustainRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _cameraFollow = GetComponent<CameraFollowClamped>();
        if (_cameraFollow == null)
            Debug.LogWarning("[CameraShake] 未找到 CameraFollowClamped，震動可能無法正常運作。", this);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Shake(float duration = 0.2f, float magnitude = 0.1f)
    {
        if (_cameraFollow == null)
            _cameraFollow = GetComponent<CameraFollowClamped>();

        if (_impulseRoutine != null)
            StopCoroutine(_impulseRoutine);
        _impulseRoutine = StartCoroutine(ImpulseShakeRoutine(duration, magnitude));
    }

    /// <summary>持續震動（例如衝撞全程），須呼叫 <see cref="StopSustainShake"/> 結束。</summary>
    public void StartSustainShake(float magnitude = 0.35f)
    {
        if (_cameraFollow == null)
            _cameraFollow = GetComponent<CameraFollowClamped>();

        if (_impulseRoutine != null)
        {
            StopCoroutine(_impulseRoutine);
            _impulseRoutine = null;
        }

        StopSustainShake();
        _sustainRoutine = StartCoroutine(SustainShakeRoutine(magnitude));
    }

    public void StopSustainShake()
    {
        if (_sustainRoutine != null)
        {
            StopCoroutine(_sustainRoutine);
            _sustainRoutine = null;
        }

        if (_sustainRoutine == null && _impulseRoutine == null && _cameraFollow != null)
            _cameraFollow.shakeOffset = Vector3.zero;
    }

    public void ShakeOnEnemyDeath() => Shake(0.15f, 0.15f);

    public void ShakeOnShoot() => Shake(0.1f, 0.09f);

    /// <summary>水彈等小幅度。</summary>
    public void ShakeLight() => Shake(0.12f, 0.1f);

    /// <summary>拍打／落石等大幅度。</summary>
    public void ShakeHeavy() => Shake(0.45f, 0.38f);

    /// <summary>雷射發射瞬間等極強短震。</summary>
    public void ShakeMassive() => Shake(0.35f, 0.55f);

    IEnumerator ImpulseShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            ApplyOffset(RandomOffset(magnitude * (1f - elapsed / duration)));
            elapsed += DeltaTime();
            yield return null;
        }

        _impulseRoutine = null;
        ClearOffsetIfIdle();
    }

    IEnumerator SustainShakeRoutine(float magnitude)
    {
        magnitude = Mathf.Max(0.01f, magnitude);
        while (true)
        {
            ApplyOffset(RandomOffset(magnitude));
            yield return null;
        }
    }

    static Vector3 RandomOffset(float mag)
    {
        return new Vector3(Random.Range(-1f, 1f) * mag, Random.Range(-1f, 1f) * mag, 0f);
    }

    void ApplyOffset(Vector3 offset)
    {
        if (_cameraFollow != null)
            _cameraFollow.shakeOffset = offset;
    }

    void ClearOffsetIfIdle()
    {
        if (_sustainRoutine == null && _impulseRoutine == null && _cameraFollow != null)
            _cameraFollow.shakeOffset = Vector3.zero;
    }

    static float DeltaTime() =>
        Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
}
