using System.Collections;
using UnityEngine;

/// <summary>
/// 掛在陸地「港口」區域的 Trigger2D 上：進入時改播港口循環環境音，離開還原一般陸地循環音。
/// 陸地場景用 <see cref="LandPlayerController"/>；水中場景用 <see cref="PlayerController"/>。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class LandHarborAmbientZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    [Header("除錯")]
    [Tooltip("勾選後會在 Console 印出 Trigger／重疊檢測與是否找到 PlayerController（發布版也可開）。")]
    [SerializeField] private bool enableDebugLogs;

    /// <summary>避免 Start 重疊檢測與 OnTriggerEnter 同一幀各加一次。</summary>
    private bool _playerInside;

    void Dbg(string msg)
    {
        if (enableDebugLogs)
            Debug.Log($"[LandHarborAmbientZone:{gameObject.name}] {msg}", this);
    }

    private void Reset()
    {
        var c = GetComponent<BoxCollider2D>();
        if (c != null) c.isTrigger = true;
    }

    private void Start()
    {
        StartCoroutine(CoCheckSpawnInsideNextFrame());
    }

    private IEnumerator CoCheckSpawnInsideNextFrame()
    {
        yield return null;
        if (_playerInside)
        {
            Dbg("CoCheckSpawnInside：已在區域內（_playerInside），略過重疊檢測。");
            yield break;
        }
        Dbg("CoCheckSpawnInside：執行 Overlap 補測（角色可能生成在 Trigger 內）。");
        TryNotifyPlayerInsideFromOverlap();
    }

    private static PlayerController FindPlayerController(Collider2D hit)
    {
        if (hit == null) return null;
        return hit.GetComponent<PlayerController>()
               ?? hit.GetComponentInParent<PlayerController>()
               ?? hit.GetComponentInChildren<PlayerController>();
    }

    private static LandPlayerController FindLandPlayerController(Collider2D hit)
    {
        if (hit == null) return null;
        return hit.GetComponent<LandPlayerController>()
               ?? hit.GetComponentInParent<LandPlayerController>()
               ?? hit.GetComponentInChildren<LandPlayerController>();
    }

    /// <summary>角色生成時已在 Trigger 內時，Enter 可能不會觸發，用重疊補一次。</summary>
    private void TryNotifyPlayerInsideFromOverlap()
    {
        var col = GetComponent<Collider2D>();
        if (col == null || !col.isTrigger)
        {
            Dbg(col == null ? "TryOverlap：沒有 Collider2D。" : "TryOverlap：Collider 未勾 Is Trigger，Trigger 事件不會觸發。");
            return;
        }

        var b = col.bounds;
        // OverlapBoxAll 的 size 為「全寬高」，使用 bounds.size（勿用 extents 否則範圍過小）
        var hits = Physics2D.OverlapBoxAll(b.center, (Vector2)b.size, col.transform.eulerAngles.z);
        Dbg($"TryOverlap：bounds center={b.center} size={(Vector2)b.size} angleZ={col.transform.eulerAngles.z:F1}，命中 Collider 數={hits.Length}");
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h == null) continue;
            if (!h.CompareTag(playerTag))
            {
                Dbg($"  略過 {h.name}：Tag 為「{h.tag}」，需要「{playerTag}」。");
                continue;
            }
            var landPc = FindLandPlayerController(h);
            if (landPc != null)
            {
                _playerInside = true;
                Dbg($"  → 通知陸地玩家進入港口（LandPlayerController.NotifyEnter），物件={h.name}");
                landPc.NotifyEnterHarborAmbientZone();
                return;
            }
            var pc = FindPlayerController(h);
            if (pc == null)
            {
                Dbg($"  略過 {h.name}：Tag 正確但找不到 LandPlayerController 或 PlayerController。");
                continue;
            }
            if (pc.IsInWater)
            {
                Dbg($"  略過 {h.name}：PlayerController 在水中（IsInWater），不觸發港口環境音。");
                continue;
            }
            _playerInside = true;
            Dbg($"  → 通知水中玩家進入港口（PlayerController.NotifyEnter），物件={h.name}");
            pc.NotifyEnterHarborAmbientZone();
            return;
        }
        Dbg("TryOverlap：範圍內沒有符合條件的玩家。");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Dbg($"OnTriggerEnter2D：{other.name} tag={other.tag}");
        if (!other.CompareTag(playerTag))
        {
            Dbg($"  略過：需要 Tag「{playerTag}」。");
            return;
        }
        var landPc = FindLandPlayerController(other);
        if (landPc != null)
        {
            if (_playerInside)
            {
                Dbg("  略過：已標記在區域內（避免重複 Enter）。");
                return;
            }
            _playerInside = true;
            Dbg("  → LandPlayerController.NotifyEnterHarborAmbientZone");
            landPc.NotifyEnterHarborAmbientZone();
            return;
        }

        var pc = FindPlayerController(other);
        if (pc == null)
        {
            Dbg("  略過：找不到 LandPlayerController 或 PlayerController（同物件／父／子）。");
            return;
        }
        if (pc.IsInWater)
        {
            Dbg("  略過：玩家在水中。");
            return;
        }
        if (_playerInside)
        {
            Dbg("  略過：已標記在區域內（避免重複 Enter）。");
            return;
        }
        _playerInside = true;
        Dbg("  → PlayerController.NotifyEnterHarborAmbientZone");
        pc.NotifyEnterHarborAmbientZone();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Dbg($"OnTriggerExit2D：{other.name} tag={other.tag}");
        if (!other.CompareTag(playerTag)) return;
        var landPc = FindLandPlayerController(other);
        if (landPc != null)
        {
            if (!_playerInside) return;
            _playerInside = false;
            Dbg("  → LandPlayerController.NotifyExitHarborAmbientZone");
            landPc.NotifyExitHarborAmbientZone();
            return;
        }
        var pc = FindPlayerController(other);
        if (pc == null) return;
        if (!_playerInside) return;
        _playerInside = false;
        Dbg("  → PlayerController.NotifyExitHarborAmbientZone");
        pc.NotifyExitHarborAmbientZone();
    }
}
