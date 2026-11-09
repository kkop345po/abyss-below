using System;
using UnityEngine;

/// <summary>
/// 花枝丸「能力倍率」Buff：僅執行時存在，不寫入 PlayerPrefs／存檔。
/// </summary>
public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    /// <summary>目前套用於能力計算的倍率（無 Buff 時為 1）。</summary>
    public float CurrentAbilityMultiplier { get; private set; } = 1f;

    /// <summary>剩餘秒數（僅除錯或 UI 用）。</summary>
    public float RemainingBuffSeconds { get; private set; }

    public static event Action<float> OnMultiplierChanged;
    public static event Action<float> OnBuffTimeChanged;

    float activeMultiplier = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapOnLoad()
    {
        EnsureExists();
    }

    /// <summary>確保場景內有 BuffManager（花枝丸等需要）。</summary>
    public static void EnsureExists()
    {
        if (Instance != null)
            return;
        var go = new GameObject(nameof(BuffManager));
        go.AddComponent<BuffManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        // 新 Session（此元件第一次建立）必為乾淨倍率；同一 Session 內換場景不會再跑 Awake。
        ResetBuffStateInMemory();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (RemainingBuffSeconds <= 0f) return;

        RemainingBuffSeconds -= Time.deltaTime;
        OnBuffTimeChanged?.Invoke(RemainingBuffSeconds);

        if (RemainingBuffSeconds <= 0f)
        {
            // Buff 計時結束：倍率還原為 1，且不再寫入任何持久化資料。
            RemainingBuffSeconds = 0f;
            activeMultiplier = 1f;
            ApplyMultiplierToPublic(1f);
        }
    }

    /// <summary>
    /// 從存檔槽還原、Reset 等時清空執行中 Buff（花枝丸不寫入存檔，也不應在讀槽後沿用舊 Session）。
    /// </summary>
    public void ClearAllBuffs()
    {
        ResetBuffStateInMemory();
    }

    /// <summary>供存檔載入、主選單讀檔等呼叫；Instance 尚未建立時安全 no-op。</summary>
    public static void ClearAllBuffsStatic()
    {
        if (Instance != null)
            Instance.ClearAllBuffs();
    }

    void ResetBuffStateInMemory()
    {
        RemainingBuffSeconds = 0f;
        activeMultiplier = 1f;
        ApplyMultiplierToPublic(1f);
        OnBuffTimeChanged?.Invoke(0f);
    }

    void ApplyMultiplierToPublic(float m)
    {
        CurrentAbilityMultiplier = m;
        OnMultiplierChanged?.Invoke(m);
    }

    /// <summary>由花枝丸道具使用時呼叫：套用對應秒數與倍率（新效果會覆蓋舊的計時與倍率）。</summary>
    public void ApplySquidBuff(float durationSeconds, float abilityMultiplier)
    {
        if (durationSeconds <= 0f || abilityMultiplier <= 0f) return;

        activeMultiplier = abilityMultiplier;
        RemainingBuffSeconds = durationSeconds;
        ApplyMultiplierToPublic(activeMultiplier);
        OnBuffTimeChanged?.Invoke(RemainingBuffSeconds);
    }
}
