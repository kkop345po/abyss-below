using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("傷害設定")]
    public int damage = 10;
    public float knockbackForce = 5f;
    public float invincibilityTime = 1f; // 無敵時間
    
    [Header("視覺效果")]
    public GameObject hitEffect;
    public AudioClip hitSound;
    
    private AudioSource audioSource;
    private bool canDamage = true;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 檢查是否有碰撞器
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("SpikeTrap 缺少 Collider2D 組件！");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning("SpikeTrap 的 Collider2D 應該設置為 Trigger！");
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"SpikeTrap 觸發碰撞，對象: {other.name}, 標籤: {other.tag}");
        
        if (other.CompareTag("Player") && canDamage)
        {
            Debug.Log("玩家進入尖刺陷阱範圍");
            
            // 對玩家造成傷害
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log($"對玩家造成 {damage} 點傷害");
                playerHealth.TakeDamage(damage);
            }
            else
            {
                Debug.LogError("玩家沒有 PlayerHealth 組件！");
            }
            
            // 擊退效果 - 使用PlayerHealth中的擊退系統
            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
                playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                Debug.Log("應用擊退效果");
            }
            
            // 視覺效果
            if (hitEffect != null)
            {
                Instantiate(hitEffect, other.transform.position, Quaternion.identity);
            }
            
            // 音效
            if (hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
            
            // 無敵時間
            StartCoroutine(InvincibilityCooldown());
        }
        else if (!other.CompareTag("Player"))
        {
            Debug.Log($"碰撞對象不是玩家，標籤: {other.tag}");
        }
        else if (!canDamage)
        {
            Debug.Log("尖刺陷阱在冷卻中，無法造成傷害");
        }
    }
    
    System.Collections.IEnumerator InvincibilityCooldown()
    {
        canDamage = false;
        yield return new WaitForSeconds(invincibilityTime);
        canDamage = true;
        Debug.Log("尖刺陷阱冷卻結束，可以再次造成傷害");
    }
} 