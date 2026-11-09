using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Effects/Heal")]
public class HealEffectSO : ItemEffectSO
{
    public int healAmount;

    public override bool ExecuteEffect(GameObject user)
    {
        var health = user.GetComponent<PlayerHealth>();
        if (health != null)
        {
            int current = health.GetCurrentHealth();
            int max = health.GetMaxHealth();
            
            int target = Mathf.Min(current + healAmount, max);
            health.SetHealth(target);
            
            // 播放使用血包音效
            if (AudioManager.GetInstance() != null) AudioManager.GetInstance().PlaySFX("用血包");
            
            Debug.Log($"[HealEffect] 恢復血量 {healAmount}，當前: {target}/{max}");
            return true;
        }
        return false;
    }

    public override bool CanExecute(GameObject user)
    {
        var health = user.GetComponent<PlayerHealth>();
        if (health != null)
        {
            return health.GetCurrentHealth() < health.GetMaxHealth();
        }
        return false;
    }
}
