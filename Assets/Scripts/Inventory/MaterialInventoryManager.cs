using UnityEngine;

public class MaterialInventoryManager : MonoBehaviour
{
    public static MaterialInventoryManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[MaterialInventoryManager] 初始化並設置為持久化（已棄用，但保留Instance）");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private readonly int[] materialCounts = new int[System.Enum.GetValues(typeof(MaterialType)).Length];

    // 以下方法已棄用，請改用 InventoryManager
    public void AddMaterial(MaterialType type, int amount) { /* 已棄用 */ }
    public bool RemoveMaterial(MaterialType type, int amount) { return false; /* 已棄用 */ }
    public int GetMaterialCount(MaterialType type) { return 0; /* 已棄用 */ }
} 