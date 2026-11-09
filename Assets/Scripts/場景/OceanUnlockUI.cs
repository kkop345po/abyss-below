using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OceanUnlockUI : MonoBehaviour
{
    public Button shallowButton;
    public Button midButton;
    public Button deepButton;

    public GameObject midLockIcon;
    public GameObject deepLockIcon;

    public GameObject midHintTooltip;
    public GameObject deepHintTooltip;

    public TextMeshProUGUI midHintText;
    public TextMeshProUGUI deepHintText;

    [Header("場景與載入")]
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private string shallowSceneName = "ShallowOceanScene";
    [SerializeField] private string midSceneName = "MidOceanScene";
    [SerializeField] private string deepSceneName = "DeepOceanScene";
    [SerializeField] private Animator harborAnimator; // 可選：專用轉場Animator


    private bool midUnlocked;
    private bool deepUnlocked;

    void Start()
    {
        UpdateButtonStates();
        if (midHintText != null) midHintText.text = "需升級肺活量 Lv.3 才可進入";
        if (deepHintText != null) deepHintText.text = "需升級體魄 Lv.5 才可進入";
    }

    public void UpdateButtonStates()
    {
        shallowButton.interactable = true;

        midUnlocked = PlayerDataManager.OxygenLevel >= 3;
        midButton.interactable = midUnlocked;
        if (midLockIcon != null) midLockIcon.SetActive(!midUnlocked);
        if (midHintTooltip != null) midHintTooltip.SetActive(false);

        deepUnlocked = PlayerDataManager.StaminaLevel >= 5;
        deepButton.interactable = deepUnlocked;
        if (deepLockIcon != null) deepLockIcon.SetActive(!deepUnlocked);
        if (deepHintTooltip != null) deepHintTooltip.SetActive(false);
    }

    public void OnPointerEnterMid()
    {
        if (!midUnlocked && midHintTooltip != null)
            midHintTooltip.SetActive(true);
    }

    public void OnPointerExitMid()
    {
        if (midHintTooltip != null)
            midHintTooltip.SetActive(false);
    }

    public void OnPointerEnterDeep()
    {
        if (!deepUnlocked && deepHintTooltip != null)
            deepHintTooltip.SetActive(true);
    }

    public void OnPointerExitDeep()
    {
        if (deepHintTooltip != null)
            deepHintTooltip.SetActive(false);
    }

    // === 新增：使用 Update 檢測滑鼠是否在按鈕範圍內 (解決偵測範圍太小的問題) ===
    void Update()
    {
        CheckHover(midButton, midHintTooltip, midUnlocked);
        CheckHover(deepButton, deepHintTooltip, deepUnlocked);
    }

    private void CheckHover(Button btn, GameObject tooltip, bool isUnlocked)
    {
        // 如果已解鎖，或缺少參照，則不處理 (保持 tooltip 關閉或由其他邏輯控制)
        if (isUnlocked || btn == null || tooltip == null) return;

        // 檢測滑鼠是否在按鈕的 RectTransform 範圍內
        // 注意：如果是 Canvas Render Mode = Screen Space Camera，第三個參數可能需要傳入 UI Camera
        bool isHovering = RectTransformUtility.RectangleContainsScreenPoint(
            btn.GetComponent<RectTransform>(), 
            Input.mousePosition, 
            null 
        );

        // 只有當狀態改變時才呼叫 SetActive (雖然 Unity 內部有優化，但這樣寫更保險)
        if (tooltip.activeSelf != isHovering)
        {
            tooltip.SetActive(isHovering);
        }
    }

    // === Button OnClick 綁定 ===
    public void GoShallow()
    {
        if (sceneLoader == null) { Debug.LogError("[OceanUnlockUI] 未指定 SceneLoader"); return; }
        // 進海域視為新一趟出航：在「出發當下」就把血/氧/體力寫成滿值，
        // 避免後續轉場保存（OnSceneTransition）把陸地殘值覆蓋掉。
        ForceVitalsFullNow();
        sceneLoader.LoadSceneWithHarborAnimation(shallowSceneName, harborAnimator);
    }

    public void GoMid()
    {
        if (!midUnlocked) { if (midHintTooltip != null) midHintTooltip.SetActive(true); return; }
        if (sceneLoader == null) { Debug.LogError("[OceanUnlockUI] 未指定 SceneLoader"); return; }
        ForceVitalsFullNow();
        sceneLoader.LoadSceneWithHarborAnimation(midSceneName, harborAnimator);
    }

    public void GoDeep()
    {
        if (!deepUnlocked) { if (deepHintTooltip != null) deepHintTooltip.SetActive(true); return; }
        if (sceneLoader == null) { Debug.LogError("[OceanUnlockUI] 未指定 SceneLoader"); return; }
        ForceVitalsFullNow();
        sceneLoader.LoadSceneWithHarborAnimation(deepSceneName, harborAnimator);
    }

    private static void ForceVitalsFullNow()
    {
        var ph = Object.FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
        if (ph != null)
        {
            int max = ph.GetMaxHealth();
            PlayerDataManager.CurrentHealth = Mathf.Max(1, max);
            PlayerDataManager.SavedMaxHealth = Mathf.Max(1, max);
        }
        else
        {
            // 後備：至少不要是 -1，避免存檔/載入時被夾成 0
            PlayerDataManager.CurrentHealth = Mathf.Max(1, PlayerDataManager.CurrentHealth);
            if (PlayerDataManager.SavedMaxHealth < 0)
                PlayerDataManager.SavedMaxHealth = PlayerDataManager.CurrentHealth;
        }

        var om = Object.FindFirstObjectByType<OxygenManager>(FindObjectsInactive.Exclude);
        if (om != null)
            PlayerDataManager.CurrentOxygen = Mathf.Max(0f, om.MaxOxygen);
        else
            PlayerDataManager.CurrentOxygen = Mathf.Max(0f, PlayerDataManager.CurrentOxygen);

        var ps = Object.FindFirstObjectByType<PlayerStamina>(FindObjectsInactive.Exclude);
        if (ps != null)
            PlayerDataManager.CurrentStamina = Mathf.Max(0f, ps.GetMaxStaminaValue());
        else
            PlayerDataManager.CurrentStamina = Mathf.Max(0f, PlayerDataManager.CurrentStamina);

        PlayerDataManager.Save();
    }
}
