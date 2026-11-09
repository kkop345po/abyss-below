using UnityEngine;

public class PlayerParalyze : MonoBehaviour
{
    [Header("麻痺狀態")]
    public bool isParalyzed = false;
    public Animator animator;
    [Tooltip("Animator Bool；須與 Player.controller 內參數一致（建議 IsParalyzed）。")]
    public string paralyzeBoolParam = "IsParalyzed";
    [Tooltip("可選 Trigger，進入麻痺時額外觸發一次。")]
    public string paralyzeTrigger = "Paralyze";

    [Header("麻痺免疫")]
    [Tooltip("麻痺結束後此秒數內不會再次被套麻痺（仍可受傷），方便逃離鯊魚等敵人。")]
    public float paralyzeImmunityCooldown = 3.5f;

    private float remainingSeconds = 0f;
    private float immunityRemainingSeconds = 0f;

    /// <summary>麻痺結束後的免疫剩餘時間（>0 時 <see cref="ApplyParalyze"/> 無效）。</summary>
    public bool IsImmuneToParalyze => immunityRemainingSeconds > 0f;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        SyncAnimatorParalyze(false);
    }

    void SyncAnimatorParalyze(bool on)
    {
        if (animator == null) return;
        if (!string.IsNullOrEmpty(paralyzeBoolParam))
            animator.SetBool(paralyzeBoolParam, on);
        if (on && !string.IsNullOrEmpty(paralyzeTrigger))
            animator.SetTrigger(paralyzeTrigger);
    }

    /// <returns>是否成功施加麻痺。</returns>
    public bool ApplyParalyze(float seconds)
    {
        if (seconds <= 0f) return false;
        if (isParalyzed || immunityRemainingSeconds > 0f)
            return false;

        isParalyzed = true;
        remainingSeconds = seconds;
        SyncAnimatorParalyze(true);
        return true;
    }

    void Update()
    {
        if (immunityRemainingSeconds > 0f)
            immunityRemainingSeconds = Mathf.Max(0f, immunityRemainingSeconds - Time.deltaTime);

        if (!isParalyzed) return;

        remainingSeconds -= Time.deltaTime;
        if (remainingSeconds <= 0f)
        {
            isParalyzed = false;
            remainingSeconds = 0f;
            SyncAnimatorParalyze(false);
            if (paralyzeImmunityCooldown > 0f)
                immunityRemainingSeconds = paralyzeImmunityCooldown;
        }
    }

    void OnDisable() => SyncAnimatorParalyze(false);
}


