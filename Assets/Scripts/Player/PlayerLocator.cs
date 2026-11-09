using UnityEngine;

/// <summary>
/// 跨場景解析玩家物件（輸出檔中 FindGameObjectWithTag 常找不到未啟用或剛生成的玩家）。
/// </summary>
public static class PlayerLocator
{
    public static PlayerHealth GetHealth()
    {
        if (PlayerHealth.Instance != null)
            return PlayerHealth.Instance;

        var health = Object.FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        if (health != null)
            return health;

        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
        {
            var onTagged = tagged.GetComponent<PlayerHealth>()
                ?? tagged.GetComponentInParent<PlayerHealth>()
                ?? tagged.GetComponentInChildren<PlayerHealth>(true);
            if (onTagged != null)
                return onTagged;
        }

        return null;
    }

    public static GameObject GetGameObject()
    {
        var health = GetHealth();
        return health != null ? health.gameObject : null;
    }
}
