using UnityEngine;

/// <summary>
/// 場景內的生成點（入口）
/// 由傳送門設定 SpawnId，ScenePlayerManager 生成玩家時會優先使用對應 SpawnId 的位置。
/// </summary>
[DisallowMultipleComponent]
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("生成點識別字串；陸地建議 LandDefault、海洋建議 SeaDefault（舊版 Default 仍相容）。")]
    public string spawnId = "Default";

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.9f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawLine(transform.position + Vector3.left * 0.5f, transform.position + Vector3.right * 0.5f);
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.down * 0.5f);
    }
}

