using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class Poisoncell : MonoBehaviour
{
    [Header("動畫設定")]
    public Animator animator;
    public string boomTriggerName = "Boom";
    public string growTriggerName = "Grow";

    [Header("偵測範圍")]
    public float detectRange = 5f;
    public string playerTag = "Player";
    
    [Header("傷害設定")]
    public float poisonDPS = 4f;
    public float poisonDuration = 3f;
    [Tooltip("傳給 PlayerDamageOverTime.damageTickInterval；愈大扣血愈慢。")]
    public float poisonDamageTickInterval = 0.85f;

    [Header("動畫時間")]
    public float boomAnimationTime = 1f;  // 爆炸動畫時間
    public float growAnimationTime = 1.5f;  // 生長動畫時間
    
    [Header("冷卻時間")]
    public float cooldownTime = 5f;  // 爆炸後冷卻時間

    private Transform player;
    private bool isInCooldown = false;
    private bool hasTriggered = false;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        
        player = GameObject.FindGameObjectWithTag(playerTag)?.transform;
    }

    void Update()
    {
        // 如果正在冷卻或已經觸發過爆炸，不檢查玩家距離
        if (isInCooldown || hasTriggered)
            return;

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag(playerTag)?.transform;
            if (player == null) return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // 玩家進入範圍時觸發爆炸
        if (distanceToPlayer <= detectRange && !hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(BoomSequence());
        }
    }

    private IEnumerator BoomSequence()
    {
        // 1. 播放爆炸動畫
        if (animator != null && !string.IsNullOrEmpty(boomTriggerName))
        {
            // 播放果實爆發音效
            AudioManager.GetInstance()?.PlayPoisonCellExplodeSfx();
            animator.SetTrigger(boomTriggerName);
        }

        // 2. 對玩家施加中毒效果
        ApplyPoisonToPlayer();

        // 3. 等待爆炸動畫結束
        yield return new WaitForSeconds(boomAnimationTime);

        // 爆炸音效屬於可中斷動作音效：在爆炸段落結束時切掉，避免殘留到後續狀態
        AudioManager.GetInstance()?.StopActionSFX();

        // 4. 播放生長恢復動畫
        if (animator != null && !string.IsNullOrEmpty(growTriggerName))
        {
            animator.SetTrigger(growTriggerName);
        }

        // 5. 等待生長動畫結束
        yield return new WaitForSeconds(growAnimationTime);

        // 6. 進入冷卻狀態
        StartCoroutine(CooldownCoroutine());
    }

    // ======== 給 Animation Event 使用（更精準） ========
    // 你也可以在 Boom 動畫最後一偵加事件呼叫 PoisonBoomSfxStop()，確保切動畫時立刻停聲。
    public void PoisonBoomSfxPlay() => AudioManager.GetInstance()?.PlayPoisonCellExplodeSfx();
    public void PoisonBoomSfxStop() => AudioManager.GetInstance()?.StopActionSFX();

    private void ApplyPoisonToPlayer()
    {
        if (player == null) return;

        var poisonComponent = player.GetComponent<PlayerDamageOverTime>()
            ?? player.GetComponentInChildren<PlayerDamageOverTime>(true)
            ?? player.GetComponentInParent<PlayerDamageOverTime>();
        if (poisonComponent != null)
        {
            poisonComponent.damageTickInterval = poisonDamageTickInterval;
            poisonComponent.Apply(poisonDPS, poisonDuration);
            Debug.Log($"[Poisoncell] 對玩家施加中毒: {poisonDPS} DPS, {poisonDuration}s, 每 {poisonDamageTickInterval}s 一跳");
        }
        else
        {
            var health = player.GetComponent<PlayerHealth>()
                ?? player.GetComponentInChildren<PlayerHealth>(true);
            if (health != null)
            {
                var dot = health.gameObject.AddComponent<PlayerDamageOverTime>();
                dot.damageTickInterval = poisonDamageTickInterval;
                dot.Apply(poisonDPS, poisonDuration);
                Debug.Log("[Poisoncell] 已自動補上 PlayerDamageOverTime 並施加中毒");
            }
            else
                Debug.LogWarning("[Poisoncell] 玩家身上沒有 PlayerHealth / PlayerDamageOverTime");
        }
    }

    private IEnumerator CooldownCoroutine()
    {
        isInCooldown = true;
        hasTriggered = false;

        yield return new WaitForSeconds(cooldownTime);

        isInCooldown = false;
        Debug.Log("[Poisoncell] 冷卻結束，可以再次觸發");
    }

    // 在Scene視圖中繪製偵測範圍
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}

