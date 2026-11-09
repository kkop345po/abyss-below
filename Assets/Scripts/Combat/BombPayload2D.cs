using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 2D 爆彈負載：落地或計時後爆炸，對敵人造成傷害並可擊退。
    /// 需要：Rigidbody2D + Collider2D（建議 CircleCollider2D, isTrigger=false）。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class BombPayload2D : MonoBehaviour
    {
        [Header("Explosion")]
        [SerializeField] private float fuseSeconds = 0.6f; // 引信時間
        [SerializeField] private float explosionRadius = 2.5f;
        [SerializeField] private float explosionDamage = 80f;
        [SerializeField] private bool knockback = true;
        [SerializeField] private float knockbackForce = 6f;
        [SerializeField] private AnimationCurve knockbackCurve; // 擊退力道衰減曲線
        [SerializeField] private LayerMask hitLayers; // 通用命中層
        [SerializeField] private LayerMask enemyLayer; // 專用敵人層 (方便配置)

        [Header("VFX/SFX")]
        [SerializeField] private GameObject explosionEffect;
        [SerializeField] private AudioClip explosionSound;

        private bool exploded = false;

        public void Initialize(float radius, float damage, bool kb)
        {
            explosionRadius = radius;
            explosionDamage = damage;
            knockback = kb;
        }

        public void SetExtraParams(AnimationCurve curve, LayerMask layer)
        {
            knockbackCurve = curve;
            enemyLayer = layer;
        }

        private void OnEnable()
        {
            Invoke(nameof(Explode), fuseSeconds);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // 碰到地面或牆也可直接爆炸（可視需求調整）
            if (!exploded)
            {
                Explode();
            }
        }

        private void Explode()
        {
            if (exploded) return;
            exploded = true;

            if (explosionEffect != null)
            {
                Instantiate(explosionEffect, transform.position, Quaternion.identity);
            }
            
            // 播放音效
            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position);
            }

            // 合併 LayerMask
            int combinedMask = hitLayers | enemyLayer;
            var hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, combinedMask);
            
            foreach (var col in hits)
            {
                // 優先嘗試通用敵人接口
                var normal = col.GetComponentInParent<NormalEnemy>();
                if (normal != null)
                {
                    normal.TakeDamage((int)explosionDamage);
                }
                else
                {
                    var baseEnemy = col.GetComponentInParent<BaseEnemy2D>();
                    if (baseEnemy != null)
                    {
                        baseEnemy.TakeDamage((int)explosionDamage);
                    }
                    else
                    {
                        var enemyParts = col.GetComponentInParent<Enemy>();
                        if (enemyParts != null)
                        {
                            enemyParts.OnBodyPartDamaged();
                        }
                    }
                }

                if (knockback)
                {
                    var rb = col.attachedRigidbody;
                    if (rb != null)
                    {
                        Vector2 dir = (col.transform.position - transform.position).normalized;
                        
                        // 計算距離衰減
                        float dist = Vector2.Distance(transform.position, col.transform.position);
                        float t = Mathf.Clamp01(dist / explosionRadius);
                        float forceMultiplier = (knockbackCurve != null && knockbackCurve.length > 0) 
                            ? knockbackCurve.Evaluate(t) 
                            : (1f - t); // 預設線性衰減

                        rb.AddForce(dir * knockbackForce * forceMultiplier, ForceMode2D.Impulse);
                        
                        // 若敵人有 BaseEnemy2D，呼叫 ApplyKnockback 確保狀態正確
                        var baseEnemy = col.GetComponentInParent<BaseEnemy2D>();
                        if (baseEnemy != null)
                        {
                            baseEnemy.ApplyKnockback(dir * knockbackForce * forceMultiplier);
                        }
                    }
                }
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
            Gizmos.DrawSphere(transform.position, explosionRadius);
        }
    }
}


