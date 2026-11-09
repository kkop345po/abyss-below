using System.Collections;
using UnityEngine;

public class BossLaser : MonoBehaviour
{
    [Header("Visuals")]
    public Sprite warningSprite;
    // 其他階段改用 Animator 控制

    [Header("Settings")]
    public float warningDuration = 1.5f;
    public float fireDuration = 5f;
    public float endDuration = 1.0f; // 結束動畫時間
    public int damage = 5;
    public float rotationSpeed = 360f; // 提高預設轉速
    public float angleOffset = 0f; // 讓使用者調整圖片角度修正

    private SpriteRenderer sr;
    private BoxCollider2D col;
    private Animator anim;
    private Vector3 targetPos;
    private bool isFiring = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        if (col != null) col.enabled = false; // 初始關閉傷害
        if (anim != null) anim.enabled = false; // 初始關閉動畫，顯示警告圖
    }

    public void Fire(Transform origin, Vector3 targetPosition)
    {
        targetPos = targetPosition;
        transform.position = origin.position;
        
        // 立即面向目標
        UpdateRotation(float.MaxValue);
        
        StartCoroutine(LaserRoutine());
    }

    private IEnumerator LaserRoutine()
    {
        // 1. Warning Phase (Blinking Sprite)
        if (sr != null && warningSprite != null) sr.sprite = warningSprite;
        
        // 再次確保方向正確
        UpdateRotation(float.MaxValue); 
        
        // 閃爍效果
        float elapsedWarning = 0f;
        float blinkInterval = 0.1f;
        float blinkTimer = 0f;

        while (elapsedWarning < warningDuration)
        {
            elapsedWarning += Time.deltaTime;
            blinkTimer += Time.deltaTime;

            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                if (sr != null) sr.enabled = !sr.enabled;
            }
            yield return null;
        }
        
        // 確保最後是顯示的
        if (sr != null) sr.enabled = true;

        // 2. Fire Phase (Animation: Start -> Loop)
        if (anim != null)
        {
            anim.enabled = true;
            anim.SetTrigger("Fire");
        }
        
        // 開啟傷害判定
        yield return new WaitForSeconds(0.2f); 
        if (col != null) col.enabled = true;
        isFiring = true;

        float elapsed = 0f;
        while (elapsed < fireDuration)
        {
            // 如果需要追蹤可開啟，目前設為定點
            // UpdateRotation(rotationSpeed); 
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. End Phase (Animation: End)
        isFiring = false;
        if (col != null) col.enabled = false;
        
        if (anim != null)
        {
            anim.SetTrigger("End");
            yield return new WaitForSeconds(endDuration); 
        }

        Destroy(gameObject);
    }

    private void UpdateRotation(float speed)
    {
        Vector3 dir = targetPos - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        
        // 加上修正角度
        Quaternion targetRot = Quaternion.AngleAxis(angle + angleOffset, Vector3.forward);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, speed * Time.deltaTime);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isFiring) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            // 這裡簡單處理，實際建議用計時器避免每幀扣血
            // 假設 PlayerHealth 有無敵時間
            health.TakeDamage(damage);
        }
    }
}
