using UnityEngine;

public class CameraFollowClamped : MonoBehaviour
{
    public Transform player;
    [Tooltip("水中跟隨：數值越大越黏角色（平滑）")]
    public float followSpeed = 10f;

    [Header("陸地跟隨（邊界優先、角色置中為輔）")]
    [Tooltip("在陸地上攝影機直接貼齊「夾在邊界內的角色中心」，避免平滑延遲造成捲動黏邊感")]
    public bool landUseHardClampFollow = true;
    [Tooltip("若為 false，陸地仍用 followSpeed 平滑（較鬆）")]
    public float landFollowSpeed = 24f;

    // 新增：震動偏移量，由 CameraShake 腳本控制
    [HideInInspector] public Vector3 shakeOffset;

    private Camera cam;
    private float originalSize;

    // 鎖定相關
    private bool isLocked = false;
    private Vector3 lockPosition;
    private float lockSize;
    private float lockTransitionSpeed = 2f;

    void Start()
    {
        cam = Camera.main;
        originalSize = cam.orthographicSize;
        
        // 開場時立即對齊到目標位置，避免看到邊界外
        transform.position = GetTargetPosition();
    }

    void LateUpdate()
    {
        if (isLocked)
        {
            float nextSize = Mathf.Lerp(cam.orthographicSize, lockSize, lockTransitionSpeed * Time.deltaTime);
            cam.orthographicSize = nextSize;

            // 有震動時直接疊在鎖定點上；勿 Lerp（會把 CameraShake 的偏移幾乎抹平）
            Vector3 desiredPos;
            if (shakeOffset.sqrMagnitude > 0.000001f)
                desiredPos = lockPosition + shakeOffset;
            else
                desiredPos = Vector3.Lerp(transform.position, lockPosition, lockTransitionSpeed * Time.deltaTime);

            transform.position = ClampToWorldBounds(desiredPos, nextSize);
            return;
        }

        Vector3 targetPos = GetTargetPosition();

        // 恢復縮放（Boss 鎖定結束後）
        if (Mathf.Abs(cam.orthographicSize - originalSize) > 0.01f)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, originalSize, lockTransitionSpeed * Time.deltaTime);
        }

        bool onLand = IsPlayerOnLand();

        // 陸地：邊界優先——目標點已是「以角色為中心且夾在 WorldBounds 內」的攝影機座標，盡量直接對齊
        if (onLand && landUseHardClampFollow)
        {
            Vector3 finalPos = targetPos + shakeOffset;
            transform.position = ClampToWorldBounds(finalPos, cam.orthographicSize);
            return;
        }

        float speed = onLand ? landFollowSpeed : followSpeed;
        Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPos, speed * Time.deltaTime);
        Vector3 finalPosSmooth = smoothedPos + shakeOffset;
        transform.position = ClampToWorldBounds(finalPosSmooth, cam.orthographicSize);
    }

    /// <summary>有 PlayerController 且不在水中視為陸地；無組件時與陸地相同處理。</summary>
    bool IsPlayerOnLand()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player == null) return true;

        var pc = player.GetComponent<PlayerController>();
        if (pc == null) return true;
        return !pc.IsInWater;
    }

    Vector3 GetTargetPosition()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player == null) return transform.position;

        // 每次 Update 都重新計算半高寬，以支援 Zoom (orthographicSize 改變)
        float currentSize = cam.orthographicSize;
        Vector3 targetPos = new Vector3(player.position.x, player.position.y, transform.position.z);

        return ClampToWorldBounds(targetPos, currentSize);
    }

    Vector3 ClampToWorldBounds(Vector3 targetPos, float camSize)
    {
        if (WorldBounds.Instance == null) return targetPos;

        float halfHeight = camSize;
        float halfWidth = halfHeight * cam.aspect;
        
        float minX = WorldBounds.Instance.minX + halfWidth;
        float maxX = WorldBounds.Instance.maxX - halfWidth;
        float minY = WorldBounds.Instance.minY + halfHeight;
        float maxY = WorldBounds.Instance.maxY - halfHeight;

        // 防止相機比地圖還大時出現錯誤 (取中間值)
        if (minX > maxX) { float mid = (minX + maxX) / 2f; minX = mid; maxX = mid; }
        if (minY > maxY) { float mid = (minY + maxY) / 2f; minY = mid; maxY = mid; }

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        
        return targetPos;
    }

    public void LockCamera(Vector3 targetPos, float targetSize)
    {
        isLocked = true;
        lockPosition = new Vector3(targetPos.x, targetPos.y, transform.position.z);
        lockSize = targetSize;
    }

    public void UnlockCamera()
    {
        isLocked = false;
    }
}
