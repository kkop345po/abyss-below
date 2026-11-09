using UnityEngine;

/// <summary>
/// 世界邊界管理
/// 用於設定場景的邊界範圍，供攝影機或其他物件使用
/// </summary>
public class WorldBounds : MonoBehaviour
{
    public static WorldBounds Instance;

    [Header("World Boundaries")]
    public float minX = -20f;
    public float maxX = 20f;
    public float minY = -10f;
    public float maxY = 10f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 限制位置在邊界內
    /// </summary>
    /// <param name="position">目標位置</param>
    /// <returns>限制後的位置</returns>
    public Vector3 ClampPosition(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        return position;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        // 繪製邊界框
        Vector3 center = new Vector3((minX + maxX) / 2, (minY + maxY) / 2, 0);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 1);
        Gizmos.DrawWireCube(center, size);
    }
}
