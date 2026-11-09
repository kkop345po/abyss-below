using UnityEngine;

public class LaserDamageZone : MonoBehaviour
{
    [Header("Settings")]
    public int damage = 10;
    public float damageInterval = 0.5f; // 傷害間隔，避免每幀扣血

    private float timer = 0f;

    private void OnEnable()
    {
        timer = 0f; // 每次啟用時重置計時器，確保第一次接觸能立即造成傷害 (視需求調整)
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 檢查是否是玩家
        if (other.CompareTag("Player"))
        {
            // 嘗試取得玩家血量腳本
            var health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                // 簡單的計時器邏輯
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    health.TakeDamage(damage);
                    timer = damageInterval;
                }
            }
        }
    }
}
