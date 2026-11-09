using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Enemy parentEnemy;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("傷害（父物件為 BaseEnemy2D / NormalEnemy 時，每次受擊）")]
    [SerializeField] private int genericEnemyDamage = 1;

    [Header("Damaged State")]
    [SerializeField] private Sprite damagedSprite;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private AudioClip hitSound;

    [Header("Status")]
    [SerializeField] private bool hasTakenDamage = false;
    public bool HasTakenDamage => hasTakenDamage;

    private AudioSource audioSource;

    void Start()
    {
        if (parentEnemy == null)
            parentEnemy = GetComponentInParent<Enemy>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (parentEnemy == null
            && GetComponentInParent<BaseEnemy2D>() == null
            && GetComponentInParent<NormalEnemy>() == null)
        {
            Debug.LogError("[Hurtbox] 父層找不到 Enemy、BaseEnemy2D 或 NormalEnemy；受擊不會扣血。", this);
        }
    }

    public void TakeDamage()
    {
        if (hasTakenDamage) return;

        hasTakenDamage = true;

        var baseEnemy = GetComponentInParent<BaseEnemy2D>();
        if (baseEnemy != null)
        {
            ApplyHurtFx();
            baseEnemy.TakeDamage(Mathf.Max(1, genericEnemyDamage));
            return;
        }

        var normalEnemy = GetComponentInParent<NormalEnemy>();
        if (normalEnemy != null)
        {
            ApplyHurtFx();
            normalEnemy.TakeDamage(Mathf.Max(1, genericEnemyDamage));
            return;
        }

        ApplyHurtFx();
        parentEnemy?.OnBodyPartDamaged();
    }

    private void ApplyHurtFx()
    {
        if (spriteRenderer != null && damagedSprite != null)
            spriteRenderer.sprite = damagedSprite;

        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);

        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, Quaternion.identity);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerAttack")) return;
        if (hasTakenDamage) return;

        TakeDamage();
        Destroy(other.gameObject);
    }
}
