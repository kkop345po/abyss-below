using UnityEngine;

public class RockProjectile : MonoBehaviour
{
    [Header("Settings")]
    public int damage = 20;
    public float lifeTime = 5f;
    [Tooltip("LayerMask for Ground/Walls to destroy the rock")]
    public LayerMask collisionLayer;

    [Header("Visual Effects")]
    [Tooltip("Optional: Spawn this effect when destroyed (e.g. dust particles)")]
    public GameObject hitEffectPrefab;
    [Tooltip("Optional: Trigger name for destruction animation on this object")]
    public string destroyTrigger = "Break";
    [Tooltip("Time to wait before Destroy() after hitting (to let animation play)")]
    public float destroyDelay = 0.5f;

    private bool isDestroyed = false;
    private Animator animator;
    private Collider2D col;
    private Rigidbody2D rb;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // 播放石頭落下音效
        if (AudioManager.GetInstance() != null) AudioManager.GetInstance().PlaySFX("石頭落下");
        
        // Auto-destroy after keeping alive for some time to prevent clutter
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed) return;

        // 1. Hit Player
        if (other.CompareTag("Player"))
        {
            Debug.Log("RockProjectile: Trigger hit Player!");
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            DestroyRock();
        }
        // 2. Hit Ground/Wall
        else if (((1 << other.gameObject.layer) & collisionLayer) != 0)
        {
            Debug.Log($"RockProjectile: Trigger hit Layer {other.gameObject.layer}");
            DestroyRock();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed) return;

        // 1. Hit Player
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("RockProjectile: Collision hit Player!");
            PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            DestroyRock();
        }
        // 2. Hit Ground/Wall
        else if (((1 << collision.gameObject.layer) & collisionLayer) != 0)
        {
             Debug.Log($"RockProjectile: Collision hit Layer {collision.gameObject.layer}");
            DestroyRock();
        }
    }

    private void DestroyRock()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        Debug.Log("RockProjectile: DestroyRock called!");

        // 1. Spawn Effect (Optional)
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. Play Animation (if configured)
        if (animator != null && !string.IsNullOrEmpty(destroyTrigger))
        {
            // Disable physics so it doesn't slide or hit again
            if (col) col.enabled = false;
            // Stop moving
            if (rb) 
            {
                rb.velocity = Vector2.zero;
                rb.simulated = false;
            }

            animator.SetTrigger(destroyTrigger);
            Destroy(gameObject, destroyDelay); // Wait for anim
        }
        else
        {
            // No animation, destroy immediately
            Destroy(gameObject);
        }
    }
}
