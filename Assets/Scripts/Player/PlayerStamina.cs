using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float baseStamina = 100f;
    public float stamina;
    public float sprintCostPerSecond = 20f;
    public float regenRate = 10f;

    [Header("UI")]
    public Image staminaBarFill;

    private bool isSprinting = false;

    void Start()
    {
        float[] bonus = { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
        float multiplier = 1f + bonus[Mathf.Clamp(PlayerDataManager.StaminaLevel, 0, 5)];
        float maxStamina = baseStamina * multiplier;

        // 從 PlayerDataManager 讀取保存的體力值，如果沒有則使用最大值
        float savedStamina = PlayerDataManager.CurrentStamina;
        if (savedStamina >= 0)
        {
            stamina = Mathf.Min(savedStamina, maxStamina);
            Debug.Log($"[PlayerStamina] 從存檔恢復體力: {stamina}/{maxStamina}");
        }
        else
        {
            stamina = maxStamina;
            Debug.Log($"[PlayerStamina] 初始化體力: {stamina}/{maxStamina}");
            // 避免自動存檔存到 -1，之後載入被 clamp 成 0
            SaveCurrentStamina();
        }

        // 自動尋找 StaminaBar
        if (staminaBarFill == null)
        {
            var bar = GameObject.Find("StaminaBar");
            if (bar != null)
                staminaBarFill = bar.GetComponent<UnityEngine.UI.Image>();
        }

        if (staminaBarFill != null)
            staminaBarFill.fillAmount = stamina / maxStamina;
    }

    void Update()
    {
        // 任何方向 + Shift 都可衝刺
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool hasDirection = Mathf.Abs(h) > 0f || Mathf.Abs(v) > 0f;
        isSprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && hasDirection;

        float previousStamina = stamina;

        if (isSprinting && stamina > 0f)
        {
            stamina -= sprintCostPerSecond * Time.deltaTime;
            stamina = Mathf.Max(0f, stamina);
        }
        else if (!isSprinting)
        {
            stamina += regenRate * Time.deltaTime;
            stamina = Mathf.Min(stamina, GetMaxStamina());
        }

        if (staminaBarFill != null)
            staminaBarFill.fillAmount = stamina / GetMaxStamina();

        // 只有當體力值發生變化時才保存
        if (Mathf.Abs(stamina - previousStamina) > 0.1f)
        {
            SaveCurrentStamina();
        }
    }

    float GetMaxStamina()
    {
        float[] bonus = { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
        return baseStamina * (1f + bonus[Mathf.Clamp(PlayerDataManager.StaminaLevel, 0, 5)]);
    }

    public float GetMaxStaminaValue()
    {
        return GetMaxStamina();
    }

    public bool CanSprint()
    {
        return stamina > 5f;
    }

    // 保存當前體力值到 PlayerDataManager
    public void SaveCurrentStamina()
    {
        PlayerDataManager.CurrentStamina = stamina;
        PlayerDataManager.Save();
    }

    // 獲取當前體力值（供外部調用）
    public float GetCurrentStamina()
    {
        return stamina;
    }

    // 設置體力值（供外部調用，用於場景切換時同步）
    public void SetStamina(float staminaValue)
    {
        float maxStamina = GetMaxStamina();
        stamina = Mathf.Clamp(staminaValue, 0f, maxStamina);
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = stamina / maxStamina;
        }
        Debug.Log($"[PlayerStamina] 設置體力: {stamina}/{maxStamina}");
    }
}