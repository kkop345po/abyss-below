using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI 按鈕按下時，同步縮放按鈕與文字（使用 unscaled time，暫停時也有效）。
/// </summary>
public class UIButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("縮放目標（可空）")]
    [SerializeField] private RectTransform buttonTarget;
    [SerializeField] private RectTransform textTarget;

    [Header("縮放參數")]
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float lerpSpeed = 18f;

    private Vector3 _buttonGoalScale = Vector3.one;
    private Vector3 _textGoalScale = Vector3.one;

    private void Awake()
    {
        if (buttonTarget == null)
            buttonTarget = transform as RectTransform;

        if (textTarget == null)
        {
            var tmp = GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) textTarget = tmp.rectTransform;
        }

        _buttonGoalScale = Vector3.one * normalScale;
        _textGoalScale = Vector3.one * normalScale;

        if (buttonTarget != null) buttonTarget.localScale = _buttonGoalScale;
        if (textTarget != null) textTarget.localScale = _textGoalScale;
    }

    private void OnEnable()
    {
        // 換場／關閉面板時若曾卡在按下狀態（未收到 PointerUp），避免 localScale 殘留導致按鈕「跑位」
        _buttonGoalScale = Vector3.one * normalScale;
        _textGoalScale = Vector3.one * normalScale;
        if (buttonTarget != null) buttonTarget.localScale = _buttonGoalScale;
        if (textTarget != null) textTarget.localScale = _textGoalScale;
    }

    private void Update()
    {
        float t = 1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime);
        if (buttonTarget != null)
            buttonTarget.localScale = Vector3.Lerp(buttonTarget.localScale, _buttonGoalScale, t);
        if (textTarget != null)
            textTarget.localScale = Vector3.Lerp(textTarget.localScale, _textGoalScale, t);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _buttonGoalScale = Vector3.one * pressedScale;
        _textGoalScale = Vector3.one * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _buttonGoalScale = Vector3.one * normalScale;
        _textGoalScale = Vector3.one * normalScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _buttonGoalScale = Vector3.one * normalScale;
        _textGoalScale = Vector3.one * normalScale;
    }
}

