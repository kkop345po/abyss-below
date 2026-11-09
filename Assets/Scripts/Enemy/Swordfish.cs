using UnityEngine;

// 🐟 旗魚（中型/主動）：高速直線衝刺
public class Swordfish : BaseEnemy2D
{
    [Header("衝刺")]
    public float windupTime = 0.3f;
    public float dashSpeed = 12f;
    public float dashDistance = 8f;
    public float dashCooldown = 2.5f;

    private enum State { Idle, Windup, Dashing, Overshoot, Stuck }
    private State state = State.Idle;
    private float stateTimer = 0f;
    private Vector2 dashDir;
    private float lastDashTime = -999f;

    [Header("衝刺進階")]
    public float overshootDuration = 0.8f; // 衝過頭煞車時間
    public float stuckDuration = 2f; // 撞牆卡住時間
    public LayerMask wallLayer; // 需設定牆壁 Layer

    protected override void Awake()
    {
        base.Awake();
        isAggressive = true; // 主動怪
        facingAngleOffset = 180f; // 反向修正
    }

    protected override void TickBehavior()
    {
        float dist = DistanceToPlayer();
        switch (state)
        {
            case State.Idle:
                if (dist <= chaseRange && Time.time - lastDashTime > dashCooldown)
                {
                    state = State.Windup;
                    stateTimer = windupTime;
                    dashDir = (player.position - transform.position).normalized;
                    // 預警特效?
                }
                else
                {
                    Patrol();
                }
                break;

            case State.Windup:
                stateTimer -= Time.fixedDeltaTime;
                // 鎖定方向，但稍微追蹤一點點? 這裡暫時鎖定
                if (stateTimer <= 0f)
                {
                    state = State.Dashing;
                    stateTimer = dashDistance / dashSpeed;
                }
                break;

            case State.Dashing:
                // 撞牆檢測
                if (wallLayer.value != 0)
                {
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, dashDir, 0.5f, wallLayer);
                    if (hit.collider != null)
                    {
                        state = State.Stuck;
                        stateTimer = stuckDuration;
                        // 播放撞擊音效/特效
                        Debug.Log("Swordfish stuck in wall!");
                        if (animator) animator.SetTrigger("Stuck"); // 需在 Animator 設 Trigger
                        return;
                    }
                }

                rb.MovePosition(rb.position + dashDir * dashSpeed * Time.fixedDeltaTime);
                FaceByVector(dashDir);
                stateTimer -= Time.fixedDeltaTime;
                
                if (stateTimer <= 0f)
                {
                    // 衝刺結束，進入煞車 (Overshoot)
                    state = State.Overshoot;
                    stateTimer = overshootDuration;
                }
                break;

            case State.Overshoot:
                // 慣性滑行，無法轉向
                stateTimer -= Time.fixedDeltaTime;
                float brakeSpeed = Mathf.Lerp(dashSpeed, 0f, 1f - (stateTimer / overshootDuration));
                rb.MovePosition(rb.position + dashDir * brakeSpeed * Time.fixedDeltaTime);
                
                if (stateTimer <= 0f)
                {
                    lastDashTime = Time.time;
                    state = State.Idle;
                }
                break;

            case State.Stuck:
                // 卡住不動，是玩家攻擊的好機會
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0f)
                {
                    lastDashTime = Time.time; // 卡住結束也算一次 CD
                    state = State.Idle;
                }
                break;
        }
    }
}


