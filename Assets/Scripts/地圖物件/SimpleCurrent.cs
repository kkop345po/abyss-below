using UnityEngine;

public class SimpleCurrent : MonoBehaviour
{
    [Header("氣流設定")]
    public Vector2 currentForce = new Vector2(5f, 0f); // 氣流方向與強度
    public float currentRadius = 3f; // 影響範圍
    public bool isActive = true;
    
    [Header("間歇性設定")]
    public bool useIntermittent = false; // 是否使用間歇性氣流
    public float duration = 2f; // 氣流持續時間
    public float cooldown = 3f; // 冷卻時間
    
    [Header("視覺效果")]
    public GameObject currentEffect;
    public AudioClip currentSound;
    public ParticleSystem waterParticles;
    
    private AudioSource audioSource;
    private bool isPlayerInRange = false;
    private Rigidbody2D playerRb;
    private bool isCurrentActive = false;
    private float timer = 0f;
    private float cooldownTimer = 0f;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 設定粒子效果
        if (waterParticles != null)
        {
            var main = waterParticles.main;
            main.startSpeed = currentForce.magnitude * 0.5f;
        }
        
        // 如果使用間歇性，初始狀態為活躍
        if (useIntermittent)
        {
            isCurrentActive = true;
        }
    }
    
    void Update()
    {
        if (!isActive) return;
        
        // 間歇性氣流邏輯
        if (useIntermittent)
        {
            if (isCurrentActive)
            {
                timer += Time.deltaTime;
                if (timer >= duration)
                {
                    isCurrentActive = false;
                    cooldownTimer = 0f;
                    StopCurrentEffects();
                }
            }
            else
            {
                cooldownTimer += Time.deltaTime;
                if (cooldownTimer >= cooldown)
                {
                    isCurrentActive = true;
                    timer = 0f;
                    if (isPlayerInRange)
                    {
                        StartCurrentEffects();
                    }
                }
            }
        }
        
        // 檢查玩家是否在範圍內
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            bool wasInRange = isPlayerInRange;
            isPlayerInRange = distance <= currentRadius;
            
            // 玩家進入範圍
            if (isPlayerInRange && !wasInRange)
            {
                OnPlayerEnterRange(player);
            }
            // 玩家離開範圍
            else if (!isPlayerInRange && wasInRange)
            {
                OnPlayerExitRange();
            }
        }
    }
    
    void OnPlayerEnterRange(GameObject player)
    {
        playerRb = player.GetComponent<Rigidbody2D>();
        
        // 如果使用間歇性且當前活躍，或者不使用間歇性，則啟動效果
        if ((useIntermittent && isCurrentActive) || !useIntermittent)
        {
            StartCurrentEffects();
        }
    }
    
    void OnPlayerExitRange()
    {
        playerRb = null;
        StopCurrentEffects();
    }
    
    void StartCurrentEffects()
    {
        // 視覺效果
        if (currentEffect != null)
        {
            currentEffect.SetActive(true);
        }
        
        // 音效
        if (currentSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(currentSound);
        }
        
        // 粒子效果
        if (waterParticles != null)
        {
            waterParticles.Play();
        }
    }
    
    void StopCurrentEffects()
    {
        // 關閉視覺效果
        if (currentEffect != null)
        {
            currentEffect.SetActive(false);
        }
        
        // 停止粒子效果
        if (waterParticles != null)
        {
            waterParticles.Stop();
        }
    }
    
    void FixedUpdate()
    {
        if (!isActive || !isPlayerInRange || playerRb == null) return;
        
        // 檢查是否應該施加氣流力
        bool shouldApplyForce = !useIntermittent || (useIntermittent && isCurrentActive);
        
        if (shouldApplyForce)
        {
            // 對玩家施加氣流力
            playerRb.AddForce(currentForce, ForceMode2D.Force);
        }
    }
    
    // 切換氣流狀態
    public void ToggleCurrent()
    {
        isActive = !isActive;
        
        if (!isActive)
        {
            OnPlayerExitRange();
        }
    }
    
    // 設定氣流強度
    public void SetCurrentForce(Vector2 force)
    {
        currentForce = force;
        
        if (waterParticles != null)
        {
            var main = waterParticles.main;
            main.startSpeed = force.magnitude * 0.5f;
        }
    }
    
    // 設定間歇性參數
    public void SetIntermittentSettings(bool useIntermittent, float duration, float cooldown)
    {
        this.useIntermittent = useIntermittent;
        this.duration = duration;
        this.cooldown = cooldown;
    }
    
    void OnDrawGizmosSelected()
    {
        // 顯示氣流方向
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, currentForce.normalized * 2f);
        
        // 顯示影響範圍
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, currentRadius);
        
        // 如果使用間歇性，顯示狀態
        if (useIntermittent)
        {
            Gizmos.color = isCurrentActive ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(0.5f, 0.5f, 0.5f));
        }
    }
} 