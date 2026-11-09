using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 掛在 <see cref="Canvas"/> 上：為底下所有 <see cref="Button"/> 綁定按鈕點擊音效（<see cref="AudioManager.PlayButtonSfx"/>）。
/// 會在子物件變動（動態開面板）時自動重新掃描。
/// </summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public class UiRootButtonSoundBinder : MonoBehaviour
{
    static readonly UnityEngine.Events.UnityAction s_play = () => AudioManager.GetInstance()?.PlayButtonSfx();

    int _lastButtonCount = int.MinValue;

    void Awake()
    {
        Wire();
        _lastButtonCount = CountButtons();
    }

    void LateUpdate()
    {
        int n = CountButtons();
        if (n == _lastButtonCount) return;
        _lastButtonCount = n;
        Wire();
    }

    int CountButtons() => GetComponentsInChildren<Button>(true).Length;

    /// <summary>可選：動態生成按鈕後手動呼叫以立即綁定。</summary>
    public void Wire()
    {
        foreach (var b in GetComponentsInChildren<Button>(true))
        {
            b.onClick.RemoveListener(s_play);
            b.onClick.AddListener(s_play);
        }
    }
}

/// <summary>
/// 每個載入的場景裡，若 Canvas 上尚未有 <see cref="UiRootButtonSoundBinder"/> 則自動加上，省去手動掛載。
/// </summary>
public static class UiButtonSoundSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        WireAllLoadedScenes();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => WireScene(scene);

    static void WireAllLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            WireScene(SceneManager.GetSceneAt(i));
    }

    static void WireScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.gameObject.GetComponent<UiRootButtonSoundBinder>() != null)
                    continue;
                canvas.gameObject.AddComponent<UiRootButtonSoundBinder>();
            }
        }
    }
}
