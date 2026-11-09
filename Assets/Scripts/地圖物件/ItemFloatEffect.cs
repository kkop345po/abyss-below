using UnityEngine;

/// <summary>
/// 掉落物漂浮效果組件 - 模擬Minecraft風格的物品漂浮效果
/// 添加上下漂浮和輕微旋轉效果
/// </summary>
public class ItemFloatEffect : MonoBehaviour
{
    [Header("漂浮設定")]
    [SerializeField] private float floatAmplitude = 0.2f; // 漂浮幅度（上下移動距離）
    [SerializeField] private float floatSpeed = 2f; // 漂浮速度
    [SerializeField] private float floatOffset = 0f; // 漂浮相位偏移（用於多個物品不同步）
    
    [Header("旋轉設定")]
    [SerializeField] private bool enableRotation = false; // 是否啟用旋轉（預設關閉，只保留上下漂浮）
    [SerializeField] private float rotationSpeed = 90f; // 旋轉速度（度/秒）
    
    [Header("生成彈出效果")]
    [SerializeField] private bool enableSpawnPop = true; // 是否啟用生成彈出動畫
    [SerializeField] private float popDuration = 0.5f; // 彈出動畫時間
    [SerializeField] private AnimationCurve popCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.7f, 1.2f), new Keyframe(1, 1)); // 彈性曲線

    [Header("縮放設定（可選）")]
    [SerializeField] private bool enablePulse = true; // 預設啟用脈衝效果
    [SerializeField] private float pulseAmplitude = 0.15f; // 稍微增加脈衝幅度
    [SerializeField] private float pulseSpeed = 3f; // 稍微加快脈衝速度
    
    private Vector3 startWorldPosition; // 初始世界座標位置（相對於父物件或世界）
    private Vector3 startLocalPosition; // 初始本地座標位置
    private Vector3 startScale; // 初始縮放
    private float timeOffset; // 時間偏移（用於隨機化）
    private Rigidbody2D rb; // 可能存在的剛體組件
    private bool useLocalSpace = true; // 是否使用本地座標空間
    private bool isPopping = false; // 是否正在播放彈出動畫

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // 如果有剛體，為了漂浮效果，建議設置為Kinematic（但仍支持Dynamic）
        if (rb != null)
        {
            useLocalSpace = false;
            startWorldPosition = transform.position;
            
            // 如果剛體是Dynamic，可以改為Kinematic以獲得更好的漂浮效果
            // 但這可能會影響其他物理效果，所以這裡只建議不強制
            if (rb.bodyType == RigidbodyType2D.Dynamic)
            {
                // 可選：自動設置為Kinematic（如果需要，取消下面的註釋）
                // rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }
        else
        {
            useLocalSpace = true;
            startLocalPosition = transform.localPosition;
        }
        
        startScale = transform.localScale;
        
        // 使用隨機偏移，讓多個物品的漂浮不同步
        timeOffset = floatOffset + Random.Range(0f, Mathf.PI * 2f);

        // 啟動彈出動畫
        if (enableSpawnPop)
        {
            StartCoroutine(SpawnPopCoroutine());
        }
    }

    private System.Collections.IEnumerator SpawnPopCoroutine()
    {
        isPopping = true;
        float elapsed = 0f;
        transform.localScale = Vector3.zero; // 初始縮放為0

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            float scaleMultiplier = popCurve.Evaluate(t);
            transform.localScale = startScale * scaleMultiplier;
            yield return null;
        }

        transform.localScale = startScale;
        isPopping = false;
    }

    private void Update()
    {
        // 如果有剛體，使用FixedUpdate；否則使用Update
        if (rb != null && rb.bodyType != RigidbodyType2D.Static)
        {
            return; // 有剛體時，在FixedUpdate中處理
        }
        
        ApplyFloatEffect();
    }
    
    private void FixedUpdate()
    {
        // 如果有動態剛體，在FixedUpdate中更新位置
        if (rb != null && rb.bodyType != RigidbodyType2D.Static)
        {
            ApplyFloatEffect();
        }
    }
    
    private void ApplyFloatEffect()
    {
        float time = Time.time + timeOffset;
        
        // 上下漂浮效果（正弦波）
        float floatY = Mathf.Sin(time * floatSpeed) * floatAmplitude;
        
        if (useLocalSpace)
        {
            // 使用本地座標（適合靜態物品或子物件）
            Vector3 newPosition = startLocalPosition + new Vector3(0, floatY, 0);
            transform.localPosition = newPosition;
        }
        else
        {
            // 使用世界座標（適合有剛體的物品）
            
            // 如果有剛體，根據類型選擇更新方式
            if (rb != null)
            {
                if (rb.bodyType == RigidbodyType2D.Kinematic)
                {
                    // Kinematic剛體使用MovePosition
                    Vector3 newPosition = startWorldPosition + new Vector3(0, floatY, 0);
                    rb.MovePosition(newPosition);
                }
                else
                {
                    // Dynamic剛體：使用物理力模擬浮力，而不是強制設定位置
                    // 這樣可以保留「噴發」的物理慣性
                    // 施加一個抵消重力的力 + 上下浮動的力
                    float buoyancy = -Physics2D.gravity.y * rb.gravityScale * rb.mass; // 抵消重力
                    float waveForce = Mathf.Cos(time * floatSpeed) * floatAmplitude * 5f; // 波動力
                    
                    // 只有在垂直速度不大時才施加波動力，避免干擾噴發
                    if (Mathf.Abs(rb.velocity.y) < 2f)
                    {
                         rb.AddForce(Vector2.up * (buoyancy + waveForce) * Time.deltaTime * 50f);
                    }
                }
            }
            else
            {
                Vector3 newPosition = startWorldPosition + new Vector3(0, floatY, 0);
                transform.position = newPosition;
            }
        }
        
        // 旋轉效果（始終使用本地旋轉，避免干擾父物件）
        if (enableRotation)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
        
        // 脈衝縮放效果（始終使用本地縮放）
        // 如果正在播放彈出動畫，則暫停脈衝，避免衝突
        if (enablePulse && !isPopping)
        {
            float pulseScale = 1f + Mathf.Sin(time * pulseSpeed) * pulseAmplitude;
            transform.localScale = startScale * pulseScale;
        }
    }
    
    /// <summary>
    /// 重置到初始位置（用於外部調用，例如掉落後重新計算基準位置）
    /// </summary>
    public void ResetPosition()
    {
        if (useLocalSpace)
        {
            startLocalPosition = transform.localPosition;
        }
        else
        {
            startWorldPosition = transform.position;
        }
    }
}

