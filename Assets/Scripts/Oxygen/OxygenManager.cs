using UnityEngine;
using UnityEngine.UI;

public enum DepthZone
{
    Shallow,
    Mid,
    Deep
}

public class OxygenManager : MonoBehaviour
{
    [Header("氧氣設定")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float shallowBaseRate = 10f;
    [SerializeField] private float midBaseRate = 15f;
    [SerializeField] private float deepBaseRate = 20f;
    [SerializeField] private float oxygenIncreaseRate = 20f;

    [Header("狀態")]
    [SerializeField] private bool isUnderwater = false;
    [SerializeField] private DepthZone currentZone = DepthZone.Shallow;

    [Header("UI 元件")]
    [SerializeField] private Slider oxygenSlider;

    [Header("音效")]
    [SerializeField] private AudioClip oxygenWarningSound;
    [SerializeField] private AudioClip oxygenDepletedSound;

    private float currentOxygen;
    private AudioSource audioSource;
    private bool isDead = false;
    private float initialMaxOxygen;
    private PlayerHealth playerHealth; // [New] 引用 PlayerHealth


    void Start()
    {
        // ⛳ 自動綁定 Slider
        // ⛳ 自動綁定 Slider
        TryFindSlider();

        initialMaxOxygen = maxOxygen;
        // 依升級等級套用固定上限表
        RecalculateFromUpgrades();

        // 從 PlayerDataManager 讀取保存的氧氣值，如果沒有則使用最大值
        float savedOxygen = PlayerDataManager.CurrentOxygen;
        if (savedOxygen >= 0)
        {
            currentOxygen = Mathf.Min(savedOxygen, maxOxygen);
            Debug.Log($"[OxygenManager] 從存檔恢復氧氣: {currentOxygen}/{maxOxygen}");
        }
        else
        {
            currentOxygen = maxOxygen;
            Debug.Log($"[OxygenManager] 初始化氧氣: {currentOxygen}/{maxOxygen}");
            // 同 PlayerHealth：避免自動存檔存到 -1，之後載入被 clamp 成 0
            SaveCurrentOxygen();
        }

        if (oxygenSlider != null)
        {
            oxygenSlider.maxValue = maxOxygen;
            oxygenSlider.value = currentOxygen;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // [New] 尋找 PlayerHealth
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
    }

    // 對外公開存取：讀取最大氧氣與提升氧氣值
    public float MaxOxygen => maxOxygen;
    public DepthZone CurrentZone => currentZone;
    public bool IsUnderwater => isUnderwater;

    public void IncreaseOxygen(float amount)
    {
        // 如果已經死亡，只有在補充氧氣時才允許執行 (復活)
        if (isDead)
        {
            if (amount > 0)
            {
                isDead = false;
                Debug.Log("[OxygenManager] 氧氣恢復，角色復活");
            }
            else
            {
                return;
            }
        }

        currentOxygen = Mathf.Clamp(currentOxygen + amount, 0f, maxOxygen);

        if (oxygenSlider == null) TryFindSlider();
        if (oxygenSlider != null)
            oxygenSlider.value = currentOxygen;

        SaveCurrentOxygen();
    }

    // 升級後重新計算（最大氧氣與 UI 上限）
    public void RecalculateFromUpgrades()
    {
        int level = Mathf.Clamp(PlayerDataManager.OxygenLevel, 0, PlayerDataManager.MaxLevel);
        // Lv0/Lv1=500, Lv2=2000, Lv3=4000, Lv4=6000, Lv5=10000
        int[] table = { 500, 500, 2000, 4000, 6000, 10000 };
        maxOxygen = table[Mathf.Clamp(level, 0, table.Length - 1)];
        currentOxygen = Mathf.Clamp(currentOxygen, 0f, maxOxygen);

        if (oxygenSlider != null)
        {
            oxygenSlider.maxValue = maxOxygen;
            oxygenSlider.value = currentOxygen;
        }
    }

    void Update()
    {
        if (isDead || Time.timeScale == 0) return;

        float previousOxygen = currentOxygen;

        if (isUnderwater)
            currentOxygen -= GetOxygenConsumptionRate() * Time.deltaTime;
        else
            currentOxygen += oxygenIncreaseRate * Time.deltaTime;

        currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);

        if (oxygenSlider != null)
            oxygenSlider.value = currentOxygen;

        // [Refactor] 移除 Update 中的頻繁存檔，避免卡頓
        // if (Mathf.Abs(currentOxygen - previousOxygen) > 0.1f)
        // {
        //     SaveCurrentOxygen();
        // }

        if (currentOxygen <= 0 && !isDead)
        {
            Die();
        }
    }

    private float GetOxygenConsumptionRate()
    {
        float multiplier = GetOxygenReductionFactor(PlayerDataManager.OxygenLevel);
        return currentZone switch
        {
            DepthZone.Shallow => shallowBaseRate * multiplier,
            DepthZone.Mid => midBaseRate * multiplier,
            DepthZone.Deep => deepBaseRate * multiplier,
            _ => shallowBaseRate * multiplier
        };
    }

    private float GetOxygenReductionFactor(int level)
    {
        float[] reduce = { 1.00f, 0.85f, 0.75f, 0.65f, 0.55f };
        return reduce[Mathf.Clamp(level, 0, reduce.Length - 1)];
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("[OxygenManager] 氧氣耗盡，角色死亡");

        if (oxygenDepletedSound != null)
            audioSource.PlayOneShot(oxygenDepletedSound);

        // 你可以在這裡加入重生、回主選單或黑幕
        
        // [Fix] 呼叫 PlayerHealth 執行真正死亡
        if (playerHealth != null)
        {
            playerHealth.Die();
        }
        else
        {
            // 嘗試再次尋找 (以防萬一)
            playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null) playerHealth.Die();
        }
    }

    public void SetUnderwaterState(bool state, DepthZone zone)
    {
        isUnderwater = state;
        currentZone = zone;
        Debug.Log($"[OxygenManager] 設定水中狀態: {isUnderwater}, 區域: {currentZone}");
    }

    // 保存當前氧氣值到 PlayerDataManager
    public void SaveCurrentOxygen()
    {
        PlayerDataManager.CurrentOxygen = currentOxygen;
        PlayerDataManager.Save();
    }

    // 獲取當前氧氣值（供外部調用）
    public float GetCurrentOxygen()
    {
        return currentOxygen;
    }

    // 設置氧氣值（供外部調用，用於場景切換時同步）
    public void SetOxygen(float oxygen)
    {
        currentOxygen = Mathf.Clamp(oxygen, 0f, maxOxygen);
        if (oxygenSlider == null) TryFindSlider();
        if (oxygenSlider != null)
        {
            oxygenSlider.value = currentOxygen;
        }
        Debug.Log($"[OxygenManager] 設置氧氣: {currentOxygen}/{maxOxygen}");
    }

    private void TryFindSlider()
    {
        if (oxygenSlider == null)
        {
            oxygenSlider = GameObject.Find("OxygenSlider")?.GetComponent<Slider>();
            if (oxygenSlider != null)
            {
                oxygenSlider.maxValue = maxOxygen;
                oxygenSlider.value = currentOxygen;
            }
        }
    }
}
