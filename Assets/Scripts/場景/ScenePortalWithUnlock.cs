using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ScenePortalWithCondition : MonoBehaviour
{
    [Header("基本設定")]
    [SerializeField] private string targetScene;
    [Tooltip("可選：指定目標場景的入口 SpawnId（需放置 SpawnPoint）。")]
    [SerializeField] private string targetSpawnId;
    [SerializeField] private ConditionType conditionType;
    [SerializeField] private int requiredLevel = 1;

    [Header("警告 UI")]
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private string lockedMessage = "等級未達標準，無法進入";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (!ConditionMet())
        {
            Debug.Log("[場景傳送] 條件未達");
            if (warningPanel != null && warningText != null)
            {
                warningText.text = lockedMessage;
                warningPanel.SetActive(true);
            }
            return;
        }

        // 若場景有 ScenePortal，沿用其記錄返回位置/存狀態等流程
        var portal = GetComponent<ScenePortal>();
        if (portal != null)
        {
            portal.targetSceneName = targetScene;
            portal.targetSpawnId = targetSpawnId;
            portal.TriggerTransfer();
            return;
        }

        // 保底：仍可直接切場，但至少要設定 SpawnId
        if (!string.IsNullOrEmpty(targetSpawnId))
            SceneSpawnRouter.SetPendingSpawnId(targetSpawnId);
        if (PixelFadeTransition.Instance != null)
            PixelFadeTransition.Instance.TransitionToScene(targetScene);
        else
            SceneManager.LoadScene(targetScene);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }

    private bool ConditionMet()
    {
        return conditionType switch
        {
            ConditionType.OxygenLevel => PlayerDataManager.OxygenLevel >= requiredLevel,
            ConditionType.staminaLevel => PlayerDataManager.StaminaLevel >= requiredLevel,
            ConditionType.SpeedLevel => PlayerDataManager.SpeedLevel >= requiredLevel,
            _ => true
        };
    }

    public enum ConditionType
    {
        OxygenLevel,
        staminaLevel,
        SpeedLevel
    }
}