using System.Collections;
using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    public float lifetime = 5f;
    public float explosionRadius = 1.5f;
    [Tooltip("爆炸動畫 Trigger 名稱（若 Animator 有此參數會優先使用）。")]
    public string explodeTriggerName = "Explode";
    [Tooltip("若沒有 Trigger，改直接播放此狀態名稱（預設對應你目前 Animator 的狀態）。")]
    public string explodeStateName = "water bomb end";
    public float fallbackDestroyDelay = 0.5f;

    private Vector3 direction;
    private bool launched = false;
    private bool exploded = false;

    public void Launch(Vector3 targetPos)
    {
        // 直線飛行：計算方向
        direction = (targetPos - transform.position).normalized;
        LaunchDirection(direction);
    }

    public void LaunchDirection(Vector3 dir)
    {
        direction = dir.normalized;
        launched = true;
        
        // 旋轉子彈朝向目標
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 因為原圖是向左的 (180度)，所以要轉 180 度修正回來
        transform.rotation = Quaternion.AngleAxis(angle + 180f, Vector3.forward);

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!launched) return;
        // 改用世界座標移動，避免受旋轉影響
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (exploded) return;

        // 忽略 Boss 自己
        if (other.GetComponent<BossController>() != null || other.GetComponentInParent<BossController>() != null) return;

        // 碰到玩家（含子碰撞器）或地面
        bool isPlayerHit = other.CompareTag("Player")
            || other.GetComponent<PlayerHealth>() != null
            || other.GetComponentInParent<PlayerHealth>() != null;
        bool isGroundHit = other.gameObject.layer == LayerMask.NameToLayer("Ground");
        if (isPlayerHit || isGroundHit)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;
        launched = false;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; // 關閉碰撞以免重複觸發

        // 播放水飛彈命中/爆炸音效
        if (AudioManager.GetInstance() != null) AudioManager.GetInstance().PlayWaterMissileSfx();

        // 造成傷害
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            var health = hit.GetComponent<PlayerHealth>() ?? hit.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage((int)damage);
            }
        }

        // 處理爆炸表現：自身播放爆炸動畫
        var anim = GetComponent<Animator>();
        if (anim != null)
        {
            // 優先使用 Trigger；若無此參數，改為直接播放爆炸狀態。
            bool hasParam = false;
            foreach (var param in anim.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger && param.name == explodeTriggerName)
                {
                    hasParam = true;
                    break;
                }
            }

            if (hasParam)
            {
                anim.SetTrigger(explodeTriggerName);
                StartCoroutine(DestroyAfterExplosionAnimation(anim, 0));
            }
            else
            {
                int stateHash = Animator.StringToHash(explodeStateName);
                if (anim.HasState(0, stateHash))
                {
                    anim.Play(stateHash, 0, 0f);
                    StartCoroutine(DestroyAfterExplosionAnimation(anim, stateHash));
                }
                else
                {
                    Destroy(gameObject, fallbackDestroyDelay);
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator DestroyAfterExplosionAnimation(Animator anim, int preferredStateHash)
    {
        float delay = Mathf.Max(0.02f, fallbackDestroyDelay);

        if (anim != null && anim.isActiveAndEnabled)
        {
            // 等待 Animator 套用 Trigger/Play，才能讀到正確狀態長度。
            yield return null;
            yield return null;

            var info = anim.GetCurrentAnimatorStateInfo(0);
            if (preferredStateHash != 0 && anim.HasState(0, preferredStateHash))
            {
                // 若指定了爆炸狀態，優先使用該狀態長度（避免剛好還在 transition）。
                if (info.shortNameHash == preferredStateHash || info.fullPathHash == preferredStateHash)
                    delay = Mathf.Max(delay, info.length);
            }
            else
            {
                delay = Mathf.Max(delay, info.length);
            }

            // 若狀態資訊仍抓不到，退回從 clip 名稱搜尋一次。
            if (delay <= fallbackDestroyDelay + 0.001f && !string.IsNullOrWhiteSpace(explodeStateName))
            {
                float clipLen = TryGetClipLengthByName(anim, explodeStateName);
                if (clipLen > 0f) delay = Mathf.Max(delay, clipLen);
            }
        }

        Destroy(gameObject, delay);
    }

    private static float TryGetClipLengthByName(Animator anim, string clipName)
    {
        if (anim == null || anim.runtimeAnimatorController == null || string.IsNullOrEmpty(clipName))
            return 0f;

        var clips = anim.runtimeAnimatorController.animationClips;
        if (clips == null) return 0f;

        foreach (var clip in clips)
        {
            if (clip == null) continue;
            if (clip.name == clipName) return clip.length;
        }
        return 0f;
    }
}
