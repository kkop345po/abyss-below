using UnityEngine;

public class BossZoneTrigger : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform cameraLockPosition; // 空物件，代表相機鎖定位置
    public float lockedCameraSize = 8f; // 鎖定時的相機大小

    [Header("Boss Settings")]
    public Transform bossAnchorPosition; // [New] Boss 的中心歸位點 (可選，若無則使用相機鎖定點)
    public GameObject bossObject;
    public GameObject bossUI;

    private bool isPlayerInZone = false;
    private CameraFollowClamped camScript;

    private void Start()
    {
        if (Camera.main != null)
        {
            camScript = Camera.main.GetComponent<CameraFollowClamped>();
        }
        
        if (camScript == null)
        {
            camScript = FindObjectOfType<CameraFollowClamped>();
        }

        // [Fix] 自動尋找 Boss UI
        if (bossUI == null)
        {
            var bar = FindObjectOfType<BossHealthBar>(true);
            if (bar != null)
                bossUI = bar.gameObject;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPlayerInZone) return;

        if (other.CompareTag("Player") || (other.GetComponent<PlayerHealth>() != null))
        {
            isPlayerInZone = true;
            
            // 鎖定相機
            if (camScript != null && cameraLockPosition != null)
            {
                camScript.LockCamera(cameraLockPosition.position, lockedCameraSize);
            }

            // 啟動 Boss
            if (bossObject != null)
            {
                // 檢查 Boss 是否已經u死亡（如果 BossController 有自我銷毀邏輯，這裡可能為 null）
                if (bossObject != null)
                {
                    bossObject.SetActive(true);
                    
                    // 嘗試取得舊版 BossController
                    var boss = bossObject.GetComponent<BossController>();
                    if (boss != null)
                    {
                        // 進入大BOSS領域、看見大BOSS時播放怪叫
                        AudioManager.GetInstance()?.PlaySFX("章魚怪叫");
                        boss.StartBossFight(cameraLockPosition, bossAnchorPosition);
                    }
                    
                    // [New] 嘗試取得新版 BigBoss_AncientGolem
                    var ancientGolem = bossObject.GetComponent<BigBoss_AncientGolem>();
                    if (ancientGolem != null)
                    {
                        // 進入大BOSS領域、看見大BOSS時播放怪叫
                        AudioManager.GetInstance()?.PlaySFX("章魚怪叫");
                        ancientGolem.ActivateBoss();
                    }
                }
            }

            // 血條由 BossController.StartBossFight 開啟；此處僅在 Boss 未自帶血條時才啟用 bossUI
            if (bossUI != null)
            {
                var boss = bossObject != null ? bossObject.GetComponent<BossController>() : null;
                if (boss == null || boss.healthBar == null || boss.healthBar.gameObject != bossUI)
                    bossUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!isPlayerInZone) return;

        if (other.CompareTag("Player") || (other.GetComponent<PlayerHealth>() != null))
        {
            isPlayerInZone = false;

            // 解鎖相機
            if (camScript != null)
            {
                camScript.UnlockCamera();
            }

            // 重置 Boss
            if (bossObject != null)
            {
                var boss = bossObject.GetComponent<BossController>();
                if (boss != null)
                {
                    boss.ResetBoss();
                }
                else
                {
                    // 如果沒有 Controller，直接隱藏
                    bossObject.SetActive(false);
                }
            }

            // 隱藏 UI
            if (bossUI != null)
            {
                bossUI.SetActive(false);
            }
        }
    }
}
