using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 僅限 Unity 編輯器的作弊核心；所有輸出版本不編譯作弊功能。
/// 快捷鍵：F2 生存組合／F3 回滿／F4 加金／F7 向前傳送／F8 滑鼠傳送／F9 重置 Boss+寶箱／F10 清檔重開。
/// F12 保留給 <see cref="CheatManager"/> 開關作弊面板。
/// </summary>
public sealed class SimpleCheatManager : MonoBehaviour
{
#if UNITY_EDITOR

    public static SimpleCheatManager Instance { get; private set; }

    private static readonly HashSet<KeyCode> GuiKeysDownThisFrame = new HashSet<KeyCode>();

    [Header("快捷鍵")]
    public KeyCode survivalBundleKey = KeyCode.F2;
    public KeyCode fullRestoreKey = KeyCode.F3;
    public KeyCode addCoinsKey = KeyCode.F4;
    public KeyCode teleportForwardKey = KeyCode.F7;
    public KeyCode teleportToMouseKey = KeyCode.F8;
    public KeyCode resetBossAndChestKey = KeyCode.F9;
    public KeyCode resetAllAndRestartKey = KeyCode.F10;

    [Header("傳送")]
    public float teleportForwardMeters = 6f;
    [Header("加金幣附帶素材")]
    [Tooltip("AddCoins 時每種 MaterialType（不含 None）各加幾個")]
    public int addCoinsMaterialBundlePerType = 20;

    [Header("編輯器提示")]
    public bool showToast = true;

    private bool _isInvincible;
    private bool _infiniteOxygen;
    private bool _infiniteStamina;
    private float _speedMultiplier = 1f;

    private GameObject _player;
    private PlayerHealth _playerHealth;
    private OxygenManager _oxygenManager;
    private PlayerStamina _playerStamina;
    private PlayerController _playerController;
    private Rigidbody2D _playerRb;

    private float _toastUntil;
    private string _toastMsg;
    private Coroutine _refreshPlayerCoroutine;

    public bool IsInvincible => _isInvincible;
    public bool IsInfiniteOxygen => _infiniteOxygen;
    public bool IsInfiniteStamina => _infiniteStamina;
    public float SpeedMultiplier => _speedMultiplier;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    /// <summary>場景內玩家生成後呼叫（例如 <see cref="ScenePlayerManager"/>）。</summary>
    public void RefreshPlayerCache()
    {
        FindPlayer();
    }

    public static void EnsureExists()
    {
        if (Instance != null)
        {
            if (!Instance.gameObject.activeInHierarchy)
                Instance.gameObject.SetActive(true);
            if (!Instance.enabled)
                Instance.enabled = true;
            return;
        }

        var found = FindFirstObjectByType<SimpleCheatManager>(FindObjectsInactive.Include);
        if (found != null)
        {
            if (!found.gameObject.activeInHierarchy)
                found.gameObject.SetActive(true);
            if (!found.enabled)
                found.enabled = true;
            return;
        }

        var go = new GameObject(nameof(SimpleCheatManager));
        go.hideFlags = HideFlags.None;
        DontDestroyOnLoad(go);
        go.AddComponent<SimpleCheatManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);



        Debug.Log("[SimpleCheatManager] 作弊核心已啟動\n" + CheatSceneTeleportTable.BuildCheatHelpText());
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
        ScheduleRefreshPlayer();

    }

    private void Update()
    {
        if (!IsPlayerCacheValid())
            FindPlayer();

        if (IsKeyDown(survivalBundleKey))
            ToggleSurvivalCheatsBundle();

        if (IsKeyDown(fullRestoreKey))
            FullRestore();

        if (IsKeyDown(addCoinsKey))
            AddCoins(1000);

        if (IsKeyDown(teleportForwardKey))
            TeleportForward(teleportForwardMeters);

        if (IsKeyDown(teleportToMouseKey))
            TeleportToMouse();

        if (IsKeyDown(resetBossAndChestKey))
            ResetBossAndChestPersistence();

        if (IsKeyDown(resetAllAndRestartKey))
            ResetAllAndRestart();

        UpdateCheatEffects();
    }

    private void LateUpdate()
    {
        GuiKeysDownThisFrame.Clear();
    }

    private void OnGUI()
    {
        var e = Event.current;
        if (e != null && e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
            GuiKeysDownThisFrame.Add(e.keyCode);

        if (!showToast) return;
        if (Time.unscaledTime > _toastUntil) return;
        if (string.IsNullOrEmpty(_toastMsg)) return;

        const int w = 280;
        const int h = 46;
        GUI.Box(new Rect(Screen.width - w - 12, 12, w, h), _toastMsg);
    }

    private void ScheduleRefreshPlayer()
    {
        if (_refreshPlayerCoroutine != null)
            StopCoroutine(_refreshPlayerCoroutine);
        _refreshPlayerCoroutine = StartCoroutine(CoRefreshPlayerAfterSceneLoad());
    }

    /// <summary>
    /// 海中場景由 <see cref="ScenePlayerManager"/> 在 Start 才生成玩家；
    /// sceneLoaded 當下 FindPlayer 會失敗，需延遲重試（輸出後亦同）。
    /// </summary>
    private IEnumerator CoRefreshPlayerAfterSceneLoad()
    {
        const int maxFrames = 30;
        for (int i = 0; i < maxFrames; i++)
        {
            FindPlayer();
            if (IsPlayerCacheValid())
                break;
            yield return null;
        }

        FindPlayer();
        _refreshPlayerCoroutine = null;
    }

    private bool IsPlayerCacheValid()
    {
        if (_player == null)
            return false;
        return _playerHealth != null || _oxygenManager != null || _playerStamina != null || _playerController != null;
    }

    private void FindPlayer()
    {
        _playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        _oxygenManager = FindFirstObjectByType<OxygenManager>(FindObjectsInactive.Include);
        _playerStamina = FindFirstObjectByType<PlayerStamina>(FindObjectsInactive.Include);
        _playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        Transform root = null;
        if (_playerHealth != null)
            root = _playerHealth.transform.root;
        else if (_oxygenManager != null)
            root = _oxygenManager.transform.root;
        else if (_playerStamina != null)
            root = _playerStamina.transform.root;
        else if (_playerController != null)
            root = _playerController.transform.root;
        else
        {
            var tagged = GameObject.FindWithTag("Player");
            if (tagged != null)
                root = tagged.transform.root;
        }

        if (root == null)
        {
            _player = null;
            _playerRb = null;
            return;
        }

        _player = root.gameObject;

        if (_playerHealth == null)
            _playerHealth = _player.GetComponentInChildren<PlayerHealth>(true);
        if (_oxygenManager == null)
            _oxygenManager = _player.GetComponentInChildren<OxygenManager>(true);
        if (_playerStamina == null)
            _playerStamina = _player.GetComponentInChildren<PlayerStamina>(true);
        if (_playerController == null)
            _playerController = _player.GetComponentInChildren<PlayerController>(true);
        _playerRb = _player.GetComponentInChildren<Rigidbody2D>(true);
    }

    private static bool IsKeyDown(KeyCode key)
    {
        if (GuiKeysDownThisFrame.Contains(key))
            return true;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            switch (key)
            {
                case KeyCode.F2:
                    if (Keyboard.current.f2Key.wasPressedThisFrame) return true;
                    break;
                case KeyCode.F3:
                    if (Keyboard.current.f3Key.wasPressedThisFrame) return true;
                    break;
                case KeyCode.F4:
                    if (Keyboard.current.f4Key.wasPressedThisFrame) return true;
                    break;
                case KeyCode.F7:
                    if (Keyboard.current.f7Key.wasPressedThisFrame) return true;
                    break;
                case KeyCode.F8:
                    if (Keyboard.current.f8Key.wasPressedThisFrame) return true;
                    break;
                case KeyCode.F9:
                    if (Keyboard.current.f9Key.wasPressedThisFrame) return true;
                    break;
                case KeyCode.F10:
                    if (Keyboard.current.f10Key.wasPressedThisFrame) return true;
                    break;
                case KeyCode.F12:
                    if (Keyboard.current.f12Key.wasPressedThisFrame) return true;
                    break;
            }
        }
#endif
        return Input.GetKeyDown(key);
    }

    private void UpdateCheatEffects()
    {
        if (!IsPlayerCacheValid())
            return;

        if (_infiniteOxygen && _oxygenManager != null)
            _oxygenManager.SetOxygen(_oxygenManager.MaxOxygen);

        if (_infiniteStamina && _playerStamina != null)
            _playerStamina.SetStamina(_playerStamina.GetMaxStaminaValue());
    }

    public void ToggleSurvivalCheatsBundle()
    {
        FindPlayer();
        if (!IsPlayerCacheValid())
        {
            Toast("未找到玩家");
            return;
        }

        bool enable = !(_isInvincible && _infiniteOxygen && _infiniteStamina);
        SetInvincibleEnabled(enable);
        _infiniteOxygen = enable;
        _infiniteStamina = enable;

        string state = enable ? "開啟" : "關閉";
        Debug.Log($"[SimpleCheatManager] 生存作弊組合 {state}");
        Toast($"無敵+無限氧+無限體力: {state}");
    }

    public void ToggleInvincible()
    {
        SetInvincibleEnabled(!_isInvincible);
        Toast($"無敵模式: {(_isInvincible ? "開啟" : "關閉")}");
    }

    private void SetInvincibleEnabled(bool enable)
    {
        if (_isInvincible == enable)
            return;

        _isInvincible = enable;
        FindPlayer();

        if (_playerHealth != null)
        {
            if (_isInvincible)
                _playerHealth.InvincibleFor(999999f);
            else
                _playerHealth.CancelInvincible();
        }
    }

    public void FullRestore()
    {
        FindPlayer();
        if (!IsPlayerCacheValid())
        {
            Toast("未找到玩家（海中請稍候再試）");
            return;
        }

        if (_playerHealth != null)
            _playerHealth.HealFull();

        if (_oxygenManager != null)
            _oxygenManager.SetOxygen(_oxygenManager.MaxOxygen);

        if (_playerStamina != null)
            _playerStamina.SetStamina(_playerStamina.GetMaxStaminaValue());

        Debug.Log("[SimpleCheatManager] 恢復全部生命值、氧氣和體力");
        Toast("已恢復全部生命值、氧氣和體力");
    }

    public void AddCoins(int amount)
    {
        PlayerDataManager.Coins += amount;
        if (CoinManager.Instance != null)
            CoinManager.Instance.UpdateCoinUI();

        int matEach = Mathf.Max(0, addCoinsMaterialBundlePerType);
        if (matEach > 0)
            GrantAllMaterials(matEach);

        SaveSystem.SavePlayerData();
        Toast(matEach > 0
            ? $"增加 {amount} 金幣，各素材 +{matEach}"
            : $"增加 {amount} 金幣");
    }

    private void GrantAllMaterials(int eachAmount)
    {
        if (eachAmount <= 0) return;
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[SimpleCheatManager] 無 InventoryManager，略過素材發放");
            return;
        }

        foreach (MaterialType t in Enum.GetValues(typeof(MaterialType)))
        {
            if (t == MaterialType.None) continue;
            InventoryManager.Instance.AddItem("mat_" + (int)t, eachAmount);
        }
    }

    public void SetCoins(int amount)
    {
        PlayerDataManager.Coins = amount;
        if (CoinManager.Instance != null)
            CoinManager.Instance.UpdateCoinUI();
        SaveSystem.SavePlayerData();
        Toast($"設置金幣為: {amount}");
    }

    public void SetUpgradeLevel(UpgradeManager.UpgradeType type, int level)
    {
        level = Mathf.Clamp(level, 0, PlayerDataManager.MaxLevel);

        switch (type)
        {
            case UpgradeManager.UpgradeType.Speed:
                PlayerDataManager.SpeedLevel = level;
                break;
            case UpgradeManager.UpgradeType.OxygenEfficiency:
                PlayerDataManager.OxygenLevel = level;
                break;
            case UpgradeManager.UpgradeType.Stamina:
                PlayerDataManager.StaminaLevel = level;
                break;
        }

        FindPlayer();
        var land = FindFirstObjectByType<LandPlayerController>();
        if (land != null)
            land.RecalculateFromUpgrades();
        if (_playerController != null)
            _playerController.RecalculateFromUpgrades();
        if (_playerHealth != null)
            _playerHealth.RecalculateFromUpgrades();
        if (_oxygenManager != null)
            _oxygenManager.RecalculateFromUpgrades();

        SaveSystem.SavePlayerData();
        Toast($"{type} 升級到等級 {level}");
    }

    public void ToggleInfiniteOxygen()
    {
        _infiniteOxygen = !_infiniteOxygen;
        Toast($"無限氧氣: {(_infiniteOxygen ? "開啟" : "關閉")}");
    }

    public void ToggleInfiniteStamina()
    {
        _infiniteStamina = !_infiniteStamina;
        Toast($"無限體力: {(_infiniteStamina ? "開啟" : "關閉")}");
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = multiplier;
        Toast($"速度倍數: {multiplier}x");
    }

    public void TeleportToScene(string sceneName)
    {
        // 場景作弊傳送已移除；保留舊事件綁定的安全空方法。
    }

    public void TeleportToMouse()
    {
        FindPlayer();
        if (_player == null)
        {
            Toast("未找到玩家");
            return;
        }

        var cam = Camera.main ?? FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);
        if (cam == null)
        {
            Toast("找不到相機");
            return;
        }

        Vector2 screenPos = Input.mousePosition;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            screenPos = Mouse.current.position.ReadValue();
#endif

        Vector3 mousePos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        mousePos.z = _player.transform.position.z;

        if (_playerRb != null)
        {
            _playerRb.position = new Vector2(mousePos.x, mousePos.y);
            _playerRb.velocity = Vector2.zero;
            _playerRb.angularVelocity = 0f;
        }
        else
        {
            _player.transform.position = mousePos;
        }

        Toast("傳送到滑鼠位置");
    }

    public void TeleportToPosition(Vector3 position)
    {
        FindPlayer();
        if (_player == null)
        {
            Toast("未找到玩家");
            return;
        }

        if (_playerRb != null)
        {
            _playerRb.position = new Vector2(position.x, position.y);
            _playerRb.velocity = Vector2.zero;
            _playerRb.angularVelocity = 0f;
        }
        else
        {
            _player.transform.position = position;
        }

        Toast($"傳送到: {position}");
    }

    public void TeleportForward(float meters)
    {
        FindPlayer();
        if (_player == null)
        {
            Toast("未找到玩家");
            return;
        }

        meters = Mathf.Clamp(meters, 0.1f, 200f);

        float facing = 1f;
        var sr = _player.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null)
            facing = sr.flipX ? -1f : 1f;
        else
            facing = _player.transform.lossyScale.x < 0f ? -1f : 1f;

        Vector3 from = _player.transform.position;
        Vector3 to = from + new Vector3(meters * facing, 0f, 0f);
        to.z = from.z;

        if (_playerRb != null)
        {
            _playerRb.position = new Vector2(to.x, to.y);
            _playerRb.velocity = Vector2.zero;
            _playerRb.angularVelocity = 0f;
        }
        else
        {
            _player.transform.position = to;
        }

        Toast($"向前傳送 {meters:0.#}m");
    }

    public void ClearAllCheats()
    {
        _isInvincible = false;
        _infiniteOxygen = false;
        _infiniteStamina = false;
        _speedMultiplier = 1f;

        FindPlayer();
        if (_playerHealth != null)
            _playerHealth.CancelInvincible();

        Toast("清除所有作弊效果");
    }

    public void ResetAllChestPersistence()
    {
        ChestPersistence.ClearAllOpenedForCheats();
        Toast("已重置寶箱開啟紀錄");
    }

    public void ResetBossAndChestPersistence()
    {
        ResetBossFight(suppressToast: true);
        ChestPersistence.ClearAllOpenedForCheats();
        Toast("已重置 Boss 與寶箱紀錄");
    }

    public void ResetBossFight(bool suppressToast = false)
    {
        var boss = FindFirstObjectByType<BossController>();
        if (boss != null)
        {
            string key = boss.bossID + "_Dead";
            if (PlayerPrefs.HasKey(key))
                PlayerPrefs.DeleteKey(key);

            boss.gameObject.SetActive(true);
            boss.ResetBoss();
        }

        FullRestore();

        if (!suppressToast)
            Toast("Boss 戰鬥已重置");
    }

    public void ResetAllAndRestart()
    {
        PlayerDataManager.ResetData();
        PlayerDataManager.ClearPersistedVitalsForFreshSpawn();
        SceneSpawnRouter.SetPendingSpawnId("Default");

        const string menu = "Menu";
        const string land = "Land";
        string target = SceneExistsInBuild(menu) ? menu : land;

        var loader = FindFirstObjectByType<SceneLoader>(FindObjectsInactive.Exclude);
        if (loader != null)
            loader.LoadSceneWithLoading(target);
        else
            SceneManager.LoadScene(target);

        Toast("清檔並重新開始");
    }

    private static bool SceneExistsInBuild(string sceneName)
    {
        return CheatSceneTeleportTable.IsInBuildSettings(sceneName);
    }

    public void Toast(string msg)
    {
        if (!showToast) return;
        _toastMsg = msg;
        _toastUntil = Time.unscaledTime + 1.25f;
    }

#else
    // 輸出版本僅保留空殼，讓既有場景引用有效；不編譯任何測試輸入或作弊邏輯。
    private void Awake() { enabled = false; }
#endif
}
