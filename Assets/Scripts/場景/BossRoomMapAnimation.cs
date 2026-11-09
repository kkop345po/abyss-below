using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 房「場景地圖／背景」Animator 驅動（非 UI 大地圖）。
/// </summary>
[DisallowMultipleComponent]
public class BossRoomMapAnimation : MonoBehaviour
{
    public static BossRoomMapAnimation Instance { get; private set; }

    [Header("Animator")]
    [SerializeField] private Animator mapAnimator;

    [Header("Trigger 名稱（須與 Controller 參數完全一致）")]
    [SerializeField] private string laserTrigger = "Laser";
    [SerializeField] private string dashTrigger = "Dash";

    [Header("除錯")]
    [SerializeField] private bool logMissingTriggers = true;

    Coroutine _laserDelayRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[BossRoomMapAnimation] 場景內有多個實例，將覆寫 Instance。");
        Instance = this;

        if (mapAnimator == null)
            mapAnimator = GetComponent<Animator>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayLaserDelayed(float delaySeconds = 2f)
    {
        if (_laserDelayRoutine != null)
            StopCoroutine(_laserDelayRoutine);
        _laserDelayRoutine = StartCoroutine(PlayLaserDelayedRoutine(delaySeconds));
    }

    public void PlayLaserNow() => FireTrigger(laserTrigger);

    public void PlayDash() => FireTrigger(dashTrigger);

    public void PlayMapLaser() => PlayLaserNow();

    public void PlayMapDash() => PlayDash();

    IEnumerator PlayLaserDelayedRoutine(float delaySeconds)
    {
        delaySeconds = Mathf.Max(0f, delaySeconds);
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);
        PlayLaserNow();
        _laserDelayRoutine = null;
    }

    void FireTrigger(string triggerName)
    {
        if (mapAnimator == null)
        {
            if (logMissingTriggers)
                Debug.LogWarning("[BossRoomMapAnimation] 未指派 Map Animator。");
            return;
        }

        if (string.IsNullOrWhiteSpace(triggerName))
            return;

        triggerName = triggerName.Trim();

        if (!AnimatorHasTrigger(triggerName))
        {
            if (logMissingTriggers)
            {
                Debug.LogWarning(
                    $"[BossRoomMapAnimation] Animator「{mapAnimator.runtimeAnimatorController?.name}」沒有 Trigger「{triggerName}」。" +
                    "請在 Controller 新增同名 Trigger，或改 Inspector 欄位名稱。",
                    mapAnimator);
            }
            return;
        }

        mapAnimator.SetTrigger(triggerName);
    }

    bool AnimatorHasTrigger(string triggerName)
    {
        if (mapAnimator == null || mapAnimator.runtimeAnimatorController == null)
            return false;

        var ps = mapAnimator.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].type == AnimatorControllerParameterType.Trigger && ps[i].name == triggerName)
                return true;
        }

        return false;
    }
}
