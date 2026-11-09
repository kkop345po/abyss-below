using UnityEngine;
using Game.Inventory.Items;
using Game.Inventory.UI;

/// <summary>
/// 跨場景常駐的可使用道具（補血等），避免僅在 Land 場景掛元件導致海中無法使用。
/// </summary>
[DefaultExecutionOrder(-50)]
public class PersistentInventoryUsables : MonoBehaviour
{
    public static PersistentInventoryUsables Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapOnLoad()
    {
        EnsureExists();
    }

    public static void EnsureExists()
    {
        if (Instance != null)
            return;
        var go = new GameObject(nameof(PersistentInventoryUsables));
        go.AddComponent<PersistentInventoryUsables>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureHealItems();
        BuffManager.EnsureExists();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void EnsureHealItems()
    {
        EnsureHealItem<HealSmallItem>(transform, "HealSmall_Persistent", InventoryQuickbarBinder.SmallHealItemId, 0f);
        EnsureHealItem<HealFullItem>(transform, "HealFull_Persistent", InventoryQuickbarBinder.FullHealItemId, 0f);
    }

    static void EnsureHealItem<T>(Transform parent, string objectName, string itemId, float cooldownSeconds)
        where T : BaseUsableItem
    {
        foreach (var existing in parent.GetComponentsInChildren<T>(true))
        {
            if (existing != null && existing.GetItemId() == itemId)
                return;
        }

        var go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        var item = go.AddComponent<T>();
        item.ConfigureRuntime(itemId, cooldownSeconds);
    }
}
