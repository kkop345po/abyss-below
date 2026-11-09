using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 掛在「開始遊戲」按鈕上：輸出檔有時 Inspector 的 UnityEvent 無法呼叫
/// <see cref="GambleUIController.StartGambleFromSelection"/>，改由同物件上的 Button 程式綁定。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class GambleStartButton : MonoBehaviour
{
    [SerializeField] GambleUIController gambleUI;

    Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        ApplyRaycastFix();
    }

    void OnEnable()
    {
        BindListeners();
    }

    public void Bind(GambleUIController ui)
    {
        gambleUI = ui;
        BindListeners();
    }

    void BindListeners()
    {
        if (_button == null)
            _button = GetComponent<Button>();
        if (_button == null)
            return;

        if (gambleUI == null)
            gambleUI = GetComponentInParent<GambleUIController>(true);

        _button.onClick.RemoveListener(OnClick);
        _button.onClick.AddListener(OnClick);
    }

    [Preserve]
    void OnClick()
    {
        if (gambleUI == null)
            gambleUI = GetComponentInParent<GambleUIController>(true);
        if (gambleUI == null)
            return;

        gambleUI.OnClickStartGambleButton();
    }

    /// <summary>TMP 子物件若開 Raycast，輸出解析度下可能吃掉點擊而不觸發 Button。</summary>
    public static void ApplyRaycastFix(Button button)
    {
        if (button == null) return;
        var tmps = button.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i] != null)
                tmps[i].raycastTarget = false;
        }
    }

    void ApplyRaycastFix()
    {
        ApplyRaycastFix(_button);
    }
}
