using UnityEngine;

public class OxygenRecharge : MonoBehaviour
{
    [Header("恢復設定")]
    public bool usePercentage = true; // [New] 是否使用百分比恢復
    [Range(0f, 100f)] public float percentageRate = 20f; // [New] 每秒恢復百分比 (預設 20%)
    public float rechargeRate = 20f; // 每秒恢復量 (固定值模式)
    public float maxRechargeDistance = 2f; // 最大恢復距離
    public bool continuousRecharge = true; // 是否持續恢復
    
    [Header("視覺效果")]
    public GameObject rechargeEffect;
    public AudioClip rechargeSound;
    public Color rechargeColor = Color.cyan;

    [Header("回氧時的呼吸音效（中海氣穴用）")]
    [Tooltip("只有當玩家處於中海 (Mid) 且正在回復氧氣時，才會播放此音效。")]
    public bool playBreathSfxWhileRecharging = true;
    [Tooltip("音效名稱（用 AudioManager.PlaySFX 播放）。若你已啟用 Resources 自動載入，可直接填檔名。")]
    public string breathSfxName = "呼吸";
    [Tooltip("每隔幾秒播放一次（避免每幀回氧就播一次造成爆音）")]
    public float breathIntervalSeconds = 0.9f;
    
    [Header("粒子效果")]
    public ParticleSystem bubbleEffect;
    
    private AudioSource audioSource;
    private bool isPlayerInRange = false;
    private OxygenManager playerOxygen;
    private float _nextBreathTime;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 設定粒子效果顏色
        if (bubbleEffect != null)
        {
            var main = bubbleEffect.main;
            main.startColor = rechargeColor;
        }
    }
    
    void Update()
    {
        if (isPlayerInRange && playerOxygen != null)
        {
            if (continuousRecharge)
            {
                // 計算恢復量
                float amount = 0f;
                if (usePercentage)
                {
                    // 百分比模式：每秒恢復 MaxOxygen 的 N%
                    amount = playerOxygen.MaxOxygen * (percentageRate / 100f) * Time.deltaTime;
                }
                else
                {
                    // 固定值模式
                    amount = rechargeRate * Time.deltaTime;
                }
                
                AddOxygen(amount);
            }
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerOxygen = other.GetComponent<OxygenManager>();
            _nextBreathTime = 0f;
            
            // 視覺效果
            if (rechargeEffect != null)
            {
                rechargeEffect.SetActive(true);
            }
            
            // 音效
            if (rechargeSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(rechargeSound);
            }
            
            // 粒子效果
            if (bubbleEffect != null)
            {
                bubbleEffect.Play();
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerOxygen = null;
            
            // 關閉視覺效果
            if (rechargeEffect != null)
            {
                rechargeEffect.SetActive(false);
            }
            
            // 停止粒子效果
            if (bubbleEffect != null)
            {
                bubbleEffect.Stop();
            }
        }
    }
    
    // 手動恢復氧氣（用於按鍵觸發）
    public void ManualRecharge()
    {
        if (playerOxygen != null)
        {
            AddOxygen(rechargeRate);
        }
    }
    
    // 恢復氧氣的方法
    private void AddOxygen(float amount)
    {
        if (playerOxygen == null) return;

        // 直接呼叫 OxygenManager 的公開 API，最大值由角色本身決定
        playerOxygen.IncreaseOxygen(amount);

        // 需求：呼吸音效只用在「中海氣穴回氧」時
        if (!playBreathSfxWhileRecharging) return;
        if (amount <= 0f) return;
        if (playerOxygen.CurrentZone != DepthZone.Mid) return;
        if (Time.time < _nextBreathTime) return;

        if (!string.IsNullOrWhiteSpace(breathSfxName))
            AudioManager.GetInstance()?.PlaySFX(breathSfxName.Trim());

        float interval = Mathf.Max(0.05f, breathIntervalSeconds);
        _nextBreathTime = Time.time + interval;
    }
} 