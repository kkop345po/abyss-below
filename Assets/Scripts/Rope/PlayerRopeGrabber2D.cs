using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class PlayerRopeGrabber2D : MonoBehaviour
{
    [Header("Refs")]
    public Transform handHook;           // 玩家身上的掛點（建議在角色骨架上放一個空物件）
    public Animator animator;            // 可選：抓取時驅動動畫參數
    public bool autoFindAnimator = true; // 若未指定，會在 Awake 嘗試自動尋找

    [Header("Animation")]
    public string grabAnimationName = "grasp"; // 抓繩動畫名稱
    public int animationLayer = 0;   // 動畫層索引
    public bool debugAnimationLogs = true; // 簡單除錯輸出
    public bool useGrabbingBool = false; // 是否使用布林參數來觸發動畫
    public string grabbingBoolName = "isGrabbingRope"; // 抓繩布林參數名稱
    public string grabbingTriggerName = "grasp"; // 抓繩觸發器名稱

    [Header("Animator Safety While Grabbing")]
    public bool forceAnimatorSpeedWhileGrabbing = true;
    public float grabbingAnimatorSpeed = 1f;
    public bool forceUpdateModeNormal = false; // 避免使用 AnimatePhysics/被 TimeScale 影響的停更情況
    [Tooltip("抓繩期間強制 Animator 永遠更新（避免 Culling 導致 normalizedTime 卡 0）。")]
    public bool forceAlwaysAnimateWhileGrabbing = true;
    [Tooltip("抓繩期間每幀手動呼叫 Animator.Update（少數專案會遇到 Animator 不推進時間的情況）。")]
    public bool manualAnimatorUpdateWhileGrabbing = true;

    [Header("Animator Layer Override While Grabbing")]
    [Tooltip("抓繩期間是否將非抓繩動畫層的權重壓到 0，避免與 SwimTree/移動層同時混合。")]
    public bool overrideOtherLayerWeightsWhileGrabbing = true;
    [Tooltip("抓繩期間抓繩層的目標權重（通常 1）。")]
    [Range(0f, 1f)] public float grabbingLayerWeight = 1f;

    [Header("Control")]
    public KeyCode toggleGrabKey = KeyCode.E;  // 按一次抓、再按一次放
    public float pumpForce = 5f;               // 左右擺盪推力
    public float pumpMaxSpeed = 8f;            // 擺盪水平速度上限
    public LayerMask ropeLayer;                // Rope 所在 Layer
    public float detectRadius = 0.8f;          // 抓取半徑

    [Header("Tags (optional)")]
    public string ropeTailTag = "RopeTail";    // 尾段 Tag（用於優先抓取）

    private Rigidbody2D _rb;
    private HingeJoint2D _grabJoint;           // 掛在玩家身上的關節
    private Rigidbody2D _currentRopeRB;        // 目前抓到的繩段剛體
    private bool _isGrabbing;
    /// <summary>目前是否抓住繩索（供 PlayerController 等阻擋攻擊）。</summary>
    public bool IsGrabbing => _isGrabbing;
    private Coroutine _ensureGrabAnimRoutine;
    private float[] _cachedLayerWeights;
    private bool _layerWeightsOverridden;
    private float _lastGrabAnimCheckTime;
    private float _lastGrabAnimNormalizedTime;
    private AnimatorCullingMode _cachedCullingMode;
    private AnimatorUpdateMode _cachedUpdateMode;
    private bool _animatorRuntimeTweaked;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        
        if (animator == null && autoFindAnimator)
        {
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (debugAnimationLogs)
            {
                if (animator)
                    Debug.Log("[PlayerRopeGrabber2D] 自動找到 Animator：" + animator.gameObject.name, this);
                else
                    Debug.LogWarning("[PlayerRopeGrabber2D] 未找到 Animator，請在 Inspector 指定或關閉 autoFindAnimator。", this);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleGrabKey))
        {
            if (_isGrabbing) ReleaseRope();
            else TryGrabNearestRope();
        }

        // 在抓繩期間，確保動畫正常播放
        if (_isGrabbing && animator)
        {
            ApplyAnimatorRuntimeTweaks();

            if (overrideOtherLayerWeightsWhileGrabbing)
                ApplyLayerWeightOverride();

            if (manualAnimatorUpdateWhileGrabbing && animator.enabled)
                animator.Update(Time.deltaTime);

            // 1. 強制確保動畫速度 (使用者要求：只要抓著就要一直播，不隨移動停止)
            if (forceAnimatorSpeedWhileGrabbing)
            {
                animator.speed = grabbingAnimatorSpeed;
            }
            
            // 2. 只要抓著，就強制維持抓繩動畫並循環播放（直到 Release）
            //    這樣可以避免 Animator Controller 沒設轉場 / 動畫非 Loop / 被其他腳本切回 Idle
            if (!string.IsNullOrEmpty(grabAnimationName))
            {
                var state = animator.GetCurrentAnimatorStateInfo(animationLayer);
                
                // 如果當前狀態 *不是* 抓繩動畫，強制播放 (覆蓋 PlayerController 的 Idle)
                if (!state.IsName(grabAnimationName))
                {
                    animator.Play(grabAnimationName, animationLayer);
                }
                // 如果是抓繩動畫且播放完畢，重播 (模擬 Loop)
                else if (state.normalizedTime >= 1.0f)
                {
                    animator.Play(grabAnimationName, animationLayer, 0f);
                }

                // 3. 診斷/保險：如果已在 grasp 狀態但 normalizedTime 長時間不變，通常是 animator.speed=0 或一直卡在轉場
                if (debugAnimationLogs && state.IsName(grabAnimationName))
                {
                    float now = Time.unscaledTime;
                    if (_lastGrabAnimCheckTime <= 0f)
                    {
                        _lastGrabAnimCheckTime = now;
                        _lastGrabAnimNormalizedTime = state.normalizedTime;
                    }
                    else if (now - _lastGrabAnimCheckTime >= 0.5f)
                    {
                        bool stuck = Mathf.Abs(state.normalizedTime - _lastGrabAnimNormalizedTime) < 0.0001f;
                        if (stuck)
                        {
                            Debug.LogWarning(
                                $"[PlayerRopeGrabber2D] 抓繩動畫疑似卡住：state='{grabAnimationName}', normalizedTime={state.normalizedTime:F3}, " +
                                $"length={state.length:F3}, shortNameHash={state.shortNameHash}, fullPathHash={state.fullPathHash}, " +
                                $"animator.speed={animator.speed:F3}, stateSpeed={state.speed:F3}, " +
                                $"inTransition={animator.IsInTransition(animationLayer)}, updateMode={animator.updateMode}, culling={animator.cullingMode}, " +
                                $"enabled={animator.enabled}, gameObject={animator.gameObject.name}",
                                this);

                            // 常見：別的腳本把 animator.speed 設為 0；抓繩時拉回 1
                            if (animator.speed <= 0.0001f)
                            {
                                animator.speed = 1f;
                                Debug.LogWarning("[PlayerRopeGrabber2D] animator.speed 為 0，已自動拉回 1 以避免卡幀。", this);
                            }
                        }

                        _lastGrabAnimCheckTime = now;
                        _lastGrabAnimNormalizedTime = state.normalizedTime;
                    }
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (_isGrabbing)
        {
            float x = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(x) > 0.01f && Mathf.Abs(_rb.velocity.x) < pumpMaxSpeed)
            {
                _rb.AddForce(new Vector2(x * pumpForce, 0f), ForceMode2D.Force);
            }
        }
    }

    void TryGrabNearestRope()
    {
        // 搜索半徑內的 Rope 物件（不限繩尾），並優先選擇尾段
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRadius, ropeLayer);
        if (hits == null || hits.Length == 0) return;

        Rigidbody2D bestCandidate = null;
        float bestScore = float.MaxValue;

        foreach (var h in hits)
        {
            var rb = h.attachedRigidbody;
            if (rb == null) continue;

            float distSqr = (h.transform.position - transform.position).sqrMagnitude;
            // 使用安全的字串比較，避免未註冊 Tag 造成 CompareTag 例外
            bool isTail = (h.tag == ropeTailTag);
            float score = distSqr + (isTail ? -0.25f : 0f); // 尾段加權

            if (score < bestScore)
            {
                bestScore = score;
                bestCandidate = rb;
            }
        }

        if (bestCandidate == null) return;

        if (_grabJoint == null) _grabJoint = gameObject.AddComponent<HingeJoint2D>();
        if (handHook == null)
        {
            // 若未指定手部掛點，退化為接在玩家中心
            handHook = transform;
        }

        _grabJoint.autoConfigureConnectedAnchor = false;
        _grabJoint.connectedBody = bestCandidate;
        _grabJoint.anchor = transform.InverseTransformPoint(handHook.position);
        _grabJoint.connectedAnchor = Vector2.zero;
        _grabJoint.useLimits = true;
        var lim = _grabJoint.limits; lim.min = -60f; lim.max = 60f; _grabJoint.limits = lim;
        _grabJoint.breakForce = Mathf.Infinity;
        _grabJoint.breakTorque = Mathf.Infinity;
        _grabJoint.enableCollision = false; // 避免自身關節碰撞干擾
        _grabJoint.enabled = true;

        _currentRopeRB = bestCandidate;
        _isGrabbing = true;

        // 抓到繩子時播放一下音效
        AudioManager.GetInstance()?.PlaySFX("抓藤蔓");
        
        // 立即播放抓繩動畫
        if (animator)
        {
            if (overrideOtherLayerWeightsWhileGrabbing)
                ApplyLayerWeightOverride();

            // 避免抓繩期間 Animator 被其他腳本停播/停更
            if (forceUpdateModeNormal)
            {
                animator.updateMode = AnimatorUpdateMode.Normal;
            }

            if (useGrabbingBool && !string.IsNullOrEmpty(grabbingBoolName))
            {
                // 使用布林參數觸發動畫
                if (AnimatorHasParameter(animator, grabbingBoolName, AnimatorControllerParameterType.Bool))
                {
                    animator.SetBool(grabbingBoolName, true);
                    if (debugAnimationLogs) Debug.Log($"[PlayerRopeGrabber2D] 設定布林參數 '{grabbingBoolName}' = true", this);
                }
                else
                {
                    if (debugAnimationLogs) Debug.LogWarning($"[PlayerRopeGrabber2D] 找不到布林參數 '{grabbingBoolName}'，改用觸發器", this);
                    if (AnimatorHasParameter(animator, grabbingTriggerName, AnimatorControllerParameterType.Trigger))
                    {
                        animator.SetTrigger(grabbingTriggerName);
                        if (debugAnimationLogs) Debug.Log($"[PlayerRopeGrabber2D] 觸發器 '{grabbingTriggerName}'", this);
                        StartEnsureGrabAnimationFallback();
                    }
                    else
                    {
                        animator.Play(grabAnimationName, animationLayer, 0f);
                        if (debugAnimationLogs) Debug.Log($"[PlayerRopeGrabber2D] 直接播放抓繩動畫 '{grabAnimationName}'", this);
                    }
                }
            }
            else
            {
                // 優先使用觸發器
                if (!string.IsNullOrEmpty(grabbingTriggerName) && AnimatorHasParameter(animator, grabbingTriggerName, AnimatorControllerParameterType.Trigger))
                {
                    animator.SetTrigger(grabbingTriggerName);
                    if (debugAnimationLogs) Debug.Log($"[PlayerRopeGrabber2D] 觸發器 '{grabbingTriggerName}'", this);
                    StartEnsureGrabAnimationFallback();
                }
                else if (!string.IsNullOrEmpty(grabAnimationName))
                {
                    animator.Play(grabAnimationName, animationLayer, 0f);
                    if (debugAnimationLogs) Debug.Log($"[PlayerRopeGrabber2D] 直接播放抓繩動畫 '{grabAnimationName}'", this);
                }
            }
        }

        // 避免玩家與抓到的段落立即互撞彈開
        var myCol = GetComponent<Collider2D>();
        var ropeCol = bestCandidate.GetComponent<Collider2D>();
        if (myCol && ropeCol) Physics2D.IgnoreCollision(myCol, ropeCol, true);
    }

    private void StartEnsureGrabAnimationFallback()
    {
        if (!animator || string.IsNullOrEmpty(grabAnimationName))
            return;

        if (_ensureGrabAnimRoutine != null)
            StopCoroutine(_ensureGrabAnimRoutine);
        _ensureGrabAnimRoutine = StartCoroutine(EnsureGrabAnimationFallback());
    }

    private System.Collections.IEnumerator EnsureGrabAnimationFallback()
    {
        // 讓 Animator Controller 有機會在下一幀進行轉場
        yield return null;
        if (!_isGrabbing || !animator) yield break;

        var st = animator.GetCurrentAnimatorStateInfo(animationLayer);
        if (!st.IsName(grabAnimationName))
        {
            // 觸發器有設但 Controller 沒配置轉場時，強制播放狀態，避免「有抓沒動畫」
            animator.Play(grabAnimationName, animationLayer, 0f);
            if (debugAnimationLogs) Debug.Log($"[PlayerRopeGrabber2D] 觸發器未轉場，改為強制播放 '{grabAnimationName}'", this);
        }
    }

    private bool AnimatorHasParameter(Animator anim, string paramName, AnimatorControllerParameterType type)
    {
        if (anim == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (var p in anim.parameters)
        {
            if (p.type == type && p.name == paramName) return true;
        }
        return false;
    }


    private bool TryCrossFade(Animator anim, string stateName, int layer, float duration, bool log, string context)
    {
        if (anim == null || string.IsNullOrEmpty(stateName)) return false;

        // 檢查層是否有效
        if (layer < 0 || layer >= anim.layerCount)
        {
            if (log) Debug.LogWarning($"[PlayerRopeGrabber2D] {context} 層索引 {layer} 無效，Animator 只有 {anim.layerCount} 層", this);
            return false;
        }

        if (log)
        {
            Debug.Log($"[PlayerRopeGrabber2D] {context} 嘗試播放狀態 '{stateName}' 在層 {layer}，當前層名：'{anim.GetLayerName(layer)}'", this);
            
            // 列出當前層的所有狀態名稱（除錯用）
            var currentState = anim.GetCurrentAnimatorStateInfo(layer);
            Debug.Log($"[PlayerRopeGrabber2D] {context} 當前狀態 hash={currentState.fullPathHash}，normalizedTime={currentState.normalizedTime:F2}", this);
        }

        // 嘗試多種可能的名稱格式
        string layerName = anim.GetLayerName(layer);
        string[] candidates = new string[] 
        { 
            stateName, 
            "Base Layer/" + stateName, 
            layerName + "/" + stateName,
            "Base Layer." + stateName,
            layerName + "." + stateName,
            "Any State/" + stateName,
            "Any State." + stateName
        };

        foreach (var candidate in candidates)
        {
            int hash = Animator.StringToHash(candidate);
            
            
            try
            {
                // 嘗試 Play 方法（更直接）
                anim.Play(hash, layer, 0f);
                
                if (log) Debug.Log($"[PlayerRopeGrabber2D] {context} Play 嘗試='{candidate}' (hash={hash})", this);
                
                // 等待一幀檢查是否切換成功
                StartCoroutine(CheckPlayResult(anim, layer, hash, candidate, log, context));
                return true;
            }
            catch (System.Exception e)
            {
                if (log) Debug.LogWarning($"[PlayerRopeGrabber2D] {context} Play 例外='{candidate}': {e.Message}", this);
                
                // Play 失敗，嘗試 CrossFade
                try
                {
                    anim.CrossFade(hash, duration, layer);
                    if (log) Debug.Log($"[PlayerRopeGrabber2D] {context} CrossFade 備用嘗試='{candidate}' (hash={hash})", this);
                    StartCoroutine(CheckPlayResult(anim, layer, hash, candidate, log, context));
                    return true;
                }
                catch (System.Exception e2)
                {
                    if (log) Debug.LogWarning($"[PlayerRopeGrabber2D] {context} CrossFade 也失敗='{candidate}': {e2.Message}", this);
                    continue;
                }
            }
        }

        if (log)
        {
            Debug.LogWarning($"[PlayerRopeGrabber2D] {context} 找不到狀態 '{stateName}'。", this);
            Debug.LogWarning($"[PlayerRopeGrabber2D] 請檢查：1)狀態名稱是否正確 2)是否在正確的層 3)狀態是否存在於 Animator Controller 中", this);
            Debug.LogWarning($"[PlayerRopeGrabber2D] 建議：在 Animator 視窗中右鍵點擊狀態，選擇 'Copy State Name' 來獲取正確的名稱", this);
        }
        return false;
    }

    private System.Collections.IEnumerator CheckPlayResult(Animator anim, int layer, int expectedHash, string stateName, bool log, string context)
    {
        yield return null; // 等待一幀
        
        var currentState = anim.GetCurrentAnimatorStateInfo(layer);
        if (currentState.fullPathHash == expectedHash)
        {
            if (log) Debug.Log($"[PlayerRopeGrabber2D] {context} 成功播放 '{stateName}'", this);
        }
        else
        {
            if (log) Debug.LogWarning($"[PlayerRopeGrabber2D] {context} 播放失敗：期望 '{stateName}' (hash={expectedHash})，實際 hash={currentState.fullPathHash}", this);
        }
    }


    public void ReleaseRope()
    {
        if (!_isGrabbing) return;

        // 還原碰撞
        var myCol = GetComponent<Collider2D>();
        if (_currentRopeRB)
        {
            var ropeCol = _currentRopeRB.GetComponent<Collider2D>();
            if (myCol && ropeCol) Physics2D.IgnoreCollision(myCol, ropeCol, false);
        }

        if (_grabJoint)
        {
            _grabJoint.enabled = false;
            _grabJoint.connectedBody = null;
        }

        _currentRopeRB = null;
        _isGrabbing = false;

        if (_ensureGrabAnimRoutine != null)
        {
            StopCoroutine(_ensureGrabAnimRoutine);
            _ensureGrabAnimRoutine = null;
        }

        RestoreLayerWeightsIfNeeded();
        RestoreAnimatorRuntimeTweaksIfNeeded();
        _lastGrabAnimCheckTime = 0f;
        _lastGrabAnimNormalizedTime = 0f;
        
        // 停止抓繩動畫，回到 idle
        if (animator)
        {
            if (useGrabbingBool && !string.IsNullOrEmpty(grabbingBoolName))
            {
                // 使用布林參數停止動畫
                if (AnimatorHasParameter(animator, grabbingBoolName, AnimatorControllerParameterType.Bool))
                {
                    animator.SetBool(grabbingBoolName, false);
                    if (debugAnimationLogs) Debug.Log($"[PlayerRopeGrabber2D] 設定布林參數 '{grabbingBoolName}' = false", this);
                }
                else
                {
                    if (debugAnimationLogs) Debug.LogWarning($"[PlayerRopeGrabber2D] 找不到布林參數 '{grabbingBoolName}'，動畫會自動回到 idle", this);
                }
            }
            else
            {
                // 使用觸發器時，動畫會自動回到 idle 狀態
                if (debugAnimationLogs) Debug.Log("[PlayerRopeGrabber2D] 動畫會自動回到 idle 狀態", this);
            }
        }
    }

    private void ApplyLayerWeightOverride()
    {
        if (!animator) return;

        int layerCount = animator.layerCount;
        if (layerCount <= 0) return;

        // 快速保護：層索引不合法就不要做 override，避免誤傷
        if (animationLayer < 0 || animationLayer >= layerCount) return;

        if (_cachedLayerWeights == null || _cachedLayerWeights.Length != layerCount)
            _cachedLayerWeights = new float[layerCount];

        // 只在第一次進入抓繩時記錄一次，避免每幀重覆覆寫原始值
        if (!_layerWeightsOverridden)
        {
            for (int i = 0; i < layerCount; i++)
                _cachedLayerWeights[i] = animator.GetLayerWeight(i);
            _layerWeightsOverridden = true;
        }

        for (int i = 0; i < layerCount; i++)
        {
            float w = (i == animationLayer) ? grabbingLayerWeight : 0f;
            animator.SetLayerWeight(i, w);
        }
    }

    private void RestoreLayerWeightsIfNeeded()
    {
        if (!animator) { _layerWeightsOverridden = false; return; }
        if (!_layerWeightsOverridden) return;
        if (_cachedLayerWeights == null) { _layerWeightsOverridden = false; return; }

        int layerCount = animator.layerCount;
        int n = Mathf.Min(layerCount, _cachedLayerWeights.Length);
        for (int i = 0; i < n; i++)
            animator.SetLayerWeight(i, _cachedLayerWeights[i]);

        _layerWeightsOverridden = false;
    }

    private void ApplyAnimatorRuntimeTweaks()
    {
        if (!animator) return;
        if (_animatorRuntimeTweaked) return;

        _cachedCullingMode = animator.cullingMode;
        _cachedUpdateMode = animator.updateMode;
        _animatorRuntimeTweaked = true;

        if (forceAlwaysAnimateWhileGrabbing)
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        if (forceUpdateModeNormal)
            animator.updateMode = AnimatorUpdateMode.Normal;
    }

    private void RestoreAnimatorRuntimeTweaksIfNeeded()
    {
        if (!animator) { _animatorRuntimeTweaked = false; return; }
        if (!_animatorRuntimeTweaked) return;

        animator.cullingMode = _cachedCullingMode;
        animator.updateMode = _cachedUpdateMode;
        _animatorRuntimeTweaked = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}


