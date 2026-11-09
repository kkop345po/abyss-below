using UnityEngine;

/// <summary>場景傳送介面已移除；保留類型以相容舊場景引用。</summary>
public class CheatSceneTeleportOverlay : MonoBehaviour
{
    private void Awake() { enabled = false; }
}
