using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 作弊面板橋接（F12 開關 UI）。實際作弊邏輯在 <see cref="SimpleCheatManager"/>。
/// </summary>
public class CheatManager : MonoBehaviour
{
#if UNITY_EDITOR

    public static CheatManager Instance;

    [Header("面板")]
    public KeyCode togglePanelKey = KeyCode.F12;

    private CheatPanelUI _cheatPanelUiCache;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureBeforeScene()
    {
        EnsureExists();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureAfterScene()
    {
        EnsureExists();
    }

    private static void EnsureExists()
    {
        SimpleCheatManager.EnsureExists();

        if (Instance != null)
        {
            if (!Instance.gameObject.activeInHierarchy)
                Instance.gameObject.SetActive(true);
            if (!Instance.enabled)
                Instance.enabled = true;
            return;
        }

        var found = FindFirstObjectByType<CheatManager>(FindObjectsInactive.Include);
        if (found != null)
        {
            if (!found.gameObject.activeInHierarchy)
                found.gameObject.SetActive(true);
            if (!found.enabled)
                found.enabled = true;

            Instance = found;
            if (found.gameObject.scene.IsValid() && found.gameObject.scene.buildIndex >= 0)
                DontDestroyOnLoad(found.gameObject);
            return;
        }

        var go = new GameObject(nameof(CheatManager));
        go.SetActive(true);
        go.AddComponent<CheatManager>();
    }

    private void Awake()
    {
        SimpleCheatManager.EnsureExists();

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _cheatPanelUiCache = null;
    }

    private void Update()
    {
        if (IsKeyDown(togglePanelKey))
            ToggleCheatPanel();
    }

    private static bool IsKeyDown(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && key == KeyCode.F12)
            return Keyboard.current.f12Key.wasPressedThisFrame;
#endif
        return Input.GetKeyDown(key);
    }

    private SimpleCheatManager Core => SimpleCheatManager.Instance;

    public void ToggleCheatPanel()
    {
        var panel = FindCheatPanelUI();
        if (panel != null)
            panel.TogglePanel();
    }

    private CheatPanelUI FindCheatPanelUI()
    {
        if (_cheatPanelUiCache != null && _cheatPanelUiCache)
            return _cheatPanelUiCache;

        var found = FindObjectsByType<CheatPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
        {
            var p = found[i];
            if (p == null) continue;
            if (!p.gameObject.scene.IsValid()) continue;
            if (string.IsNullOrEmpty(p.gameObject.scene.name)) continue;
            _cheatPanelUiCache = p;
            return p;
        }

        _cheatPanelUiCache = null;
        return null;
    }

    private void ShowCheatMessage(string message)
    {
        var panel = FindCheatPanelUI();
        if (panel != null)
            panel.ShowMessage(message);
        else if (Core != null)
            Core.Toast(message);
    }

    public void ToggleSurvivalCheatsBundle() => Core?.ToggleSurvivalCheatsBundle();
    public void ToggleInvincible() => Core?.ToggleInvincible();
    public void FullRestore() => Core?.FullRestore();
    public void AddCoins(int amount) => Core?.AddCoins(amount);
    public void SetCoins(int amount) => Core?.SetCoins(amount);
    public void SetUpgradeLevel(UpgradeManager.UpgradeType type, int level) => Core?.SetUpgradeLevel(type, level);
    public void ToggleInfiniteOxygen() => Core?.ToggleInfiniteOxygen();
    public void ToggleInfiniteStamina() => Core?.ToggleInfiniteStamina();
    public void SetSpeedMultiplier(float multiplier) => Core?.SetSpeedMultiplier(multiplier);
    public void TeleportToScene(string sceneName) => Core?.TeleportToScene(sceneName);
    public void TeleportToMouse() => Core?.TeleportToMouse();
    public void TeleportToPosition(Vector3 position) => Core?.TeleportToPosition(position);
    public void TeleportForward(float meters) => Core?.TeleportForward(meters);
    public void ClearAllCheats() => Core?.ClearAllCheats();
    public void ResetAllChestPersistence() => Core?.ResetAllChestPersistence();
    public void ResetBossAndChestPersistence() => Core?.ResetBossAndChestPersistence();
    public void ResetBossFight(bool suppressToast = false) => Core?.ResetBossFight(suppressToast);

    public bool IsInvincible => Core != null && Core.IsInvincible;
    public bool IsInfiniteOxygen => Core != null && Core.IsInfiniteOxygen;
    public bool IsInfiniteStamina => Core != null && Core.IsInfiniteStamina;
    public float SpeedMultiplier => Core != null ? Core.SpeedMultiplier : 1f;

#else
    // 輸出版本僅保留空殼，讓既有場景引用有效；不編譯任何測試輸入或作弊邏輯。
    private void Awake() { enabled = false; }
#endif
}
