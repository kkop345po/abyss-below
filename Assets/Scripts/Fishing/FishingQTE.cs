using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 釣魚 QTE：掙扎階段連打按鍵。進度條隨按鍵填滿；時間條持續倒數（較易看出在動）。
/// </summary>
public class FishingQTE : MonoBehaviour
{
    public enum QTEMode
    {
        Mash,
        SinglePress
    }

    [Header("UI 參考")]
    [SerializeField] private GameObject fishingQTEPanel;
    [Tooltip("按 E 時從左填滿（進度）")]
    [SerializeField] private Image pressProgressBar;
    [Tooltip("可選：隨時間從右減少；未指定時會嘗試用 Press Progress Bar 的兄弟物件 TimerBarTime")]
    [SerializeField] private Image timeLimitBar;
    [SerializeField] private Image qteKeyImage;
    [SerializeField] private TMP_Text pressCountText;

    [Header("模式")]
    [SerializeField] private QTEMode qteMode = QTEMode.Mash;
    [SerializeField] private KeyCode confirmKey = KeyCode.E;

    [Header("連打模式（Mash）")]
    [SerializeField] private int requiredPressCount = 6;
    [SerializeField] private float mashTimeLimit = 2.5f;
    [SerializeField] private int nuclearRequiredPressCount = 10;
    [SerializeField] private float nuclearMashTimeLimit = 1.4f;
    [SerializeField] private float minPressInterval = 0.08f;
    [Tooltip("QTE 面板出現後，此秒數內按鍵不計入（避免誤觸／輸入殘留）")]
    [SerializeField] private float mashInputDelay = 0.15f;
    [SerializeField] private bool playSfxOnEachPress = true;

    [Header("進度條動效")]
    [SerializeField] private float fillSmoothSpeed = 8f;
    [SerializeField] private float pressFillOvershoot = 0.12f;
    [SerializeField] private Color timeBarNormalColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color timeBarLowColor = new Color(1f, 0.25f, 0.2f, 1f);

    [Header("單次按鍵模式（SinglePress，舊版）")]
    [SerializeField] private float singlePressDuration = 1.2f;
    [SerializeField] private float nuclearSinglePressDuration = 0.6f;

    [Header("脈衝效果")]
    [SerializeField] private float pulseMinScale = 0.85f;
    [SerializeField] private float pulseMaxScale = 1.2f;
    [SerializeField] private float pulseSpeed = 8f;
    [SerializeField] private float pressPulseBoost = 0.25f;

    [Header("相容舊欄位（可留空，會對應到 Press Progress Bar）")]
    [SerializeField] private Image timerBar;

    RectTransform _iconRect;
    Vector3 _iconBaseScale = Vector3.one;
    Coroutine _pulseRoutine;
    float _pressPulseTimer;
    float _displayPressFill;
    float _targetPressFill;
    float _fillOvershoot;

    public float DefaultDuration => mashTimeLimit;
    public float NuclearWasteDuration => nuclearMashTimeLimit;

    public int GetRequiredPressCount(bool isNuclearWaste) =>
        Mathf.Max(1, isNuclearWaste ? nuclearRequiredPressCount : requiredPressCount);

    void Awake()
    {
        if (pressProgressBar == null && timerBar != null)
            pressProgressBar = timerBar;

        if (timeLimitBar == null && pressProgressBar != null)
        {
            var t = pressProgressBar.transform.parent != null
                ? pressProgressBar.transform.parent.Find("TimerBarTime")
                : null;
            if (t != null)
                timeLimitBar = t.GetComponent<Image>();
        }

        EnsureFilledBar(pressProgressBar, true);
        EnsureFilledBar(timeLimitBar, false);

        if (qteKeyImage != null)
        {
            _iconRect = qteKeyImage.rectTransform;
            _iconBaseScale = _iconRect.localScale;
        }

        Hide();
    }

    static void EnsureFilledBar(Image bar, bool fillFromLeft)
    {
        if (bar == null)
            return;

        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        bar.fillOrigin = fillFromLeft
            ? (int)Image.OriginHorizontal.Left
            : (int)Image.OriginHorizontal.Right;
        bar.preserveAspect = false;
    }

    public void Hide()
    {
        StopPulse();
        if (fishingQTEPanel != null)
            fishingQTEPanel.SetActive(false);
    }

    public IEnumerator RunQTE(QTEResult result, bool isNuclearWaste)
    {
        if (qteMode == QTEMode.SinglePress)
            yield return RunSinglePressQTE(result, isNuclearWaste);
        else
            yield return RunMashQTE(result, isNuclearWaste);
    }

    public IEnumerator RunQTE(QTEResult result, float duration)
    {
        if (qteMode == QTEMode.Mash)
        {
            yield return RunMashQTE(result, false);
            yield break;
        }

        result.Success = false;
        duration = Mathf.Max(0.05f, duration);

        if (fishingQTEPanel != null)
            fishingQTEPanel.SetActive(true);

        ResetBarVisuals();
        if (timeLimitBar != null)
            timeLimitBar.fillAmount = 1f;

        StartPulse();

        float elapsed = 0f;
        bool pressed = false;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            UpdateTimeLimitBar(elapsed, duration);

            if (Input.GetKeyDown(confirmKey))
            {
                pressed = true;
                break;
            }

            yield return null;
        }

        result.Success = pressed;
        result.PressCount = pressed ? 1 : 0;
        result.RequiredPressCount = 1;
        Hide();
    }

    IEnumerator RunMashQTE(QTEResult result, bool isNuclearWaste)
    {
        result.Success = false;
        result.PressCount = 0;

        int required = isNuclearWaste ? nuclearRequiredPressCount : requiredPressCount;
        float timeLimit = isNuclearWaste ? nuclearMashTimeLimit : mashTimeLimit;
        required = Mathf.Max(1, required);
        timeLimit = Mathf.Max(0.2f, timeLimit);

        result.RequiredPressCount = required;

        if (fishingQTEPanel != null)
            fishingQTEPanel.SetActive(true);

        ResetBarVisuals();
        SetPressProgress(0, required);

        StartPulse();

        float elapsed = 0f;
        float lastPressTime = -999f;
        int presses = 0;

        while (elapsed < timeLimit)
        {
            elapsed += Time.unscaledDeltaTime;
            UpdateTimeLimitBar(elapsed, timeLimit);
            UpdatePressProgressBarAnimated();
            UpdateMashTimeLabel(presses, required, timeLimit - elapsed);

            bool acceptInput = elapsed >= mashInputDelay;
            if (acceptInput
                && Input.GetKeyDown(confirmKey)
                && elapsed - lastPressTime >= minPressInterval)
            {
                lastPressTime = elapsed;
                presses++;
                result.PressCount = presses;
                SetPressProgress(presses, required);
                _fillOvershoot = pressFillOvershoot;
                _pressPulseTimer = pressPulseBoost;

                if (playSfxOnEachPress)
                    AudioManager.GetInstance()?.PlayButtonSfx();

                if (presses >= required)
                {
                    result.Success = true;
                    break;
                }
            }

            yield return null;
        }

        UpdatePressProgressBarAnimated();
        UpdateTimeLimitBar(timeLimit, timeLimit);

        if (!result.Success)
            result.Success = presses >= required;

        Hide();
    }

    IEnumerator RunSinglePressQTE(QTEResult result, bool isNuclearWaste)
    {
        float duration = isNuclearWaste ? nuclearSinglePressDuration : singlePressDuration;
        yield return RunQTE(result, duration);
    }

    void ResetBarVisuals()
    {
        _displayPressFill = 0f;
        _targetPressFill = 0f;
        _fillOvershoot = 0f;

        if (pressProgressBar != null)
            pressProgressBar.fillAmount = 0f;

        if (timeLimitBar != null)
        {
            timeLimitBar.fillAmount = 1f;
            timeLimitBar.color = timeBarNormalColor;
        }
    }

    void SetPressProgress(int current, int required)
    {
        _targetPressFill = required > 0 ? Mathf.Clamp01((float)current / required) : 0f;
        UpdateMashTimeLabel(current, required, -1f);
    }

    void UpdatePressProgressBarAnimated()
    {
        if (pressProgressBar == null)
            return;

        if (_fillOvershoot > 0f)
            _fillOvershoot = Mathf.Max(0f, _fillOvershoot - Time.unscaledDeltaTime * 3f);

        _displayPressFill = Mathf.MoveTowards(
            _displayPressFill,
            _targetPressFill,
            Time.unscaledDeltaTime * fillSmoothSpeed);

        pressProgressBar.fillAmount = Mathf.Clamp01(_displayPressFill + _fillOvershoot);
    }

    void UpdateTimeLimitBar(float elapsed, float timeLimit)
    {
        if (timeLimitBar == null)
            return;

        float ratio = 1f - Mathf.Clamp01(elapsed / timeLimit);
        timeLimitBar.fillAmount = ratio;
        timeLimitBar.color = Color.Lerp(timeBarLowColor, timeBarNormalColor, ratio);
    }

    void UpdateMashTimeLabel(int current, int required, float secondsLeft)
    {
        if (pressCountText == null)
            return;

        if (secondsLeft >= 0f)
            pressCountText.text = $"{current} / {required}  ·  {secondsLeft:0.0}s";
        else
            pressCountText.text = $"{current} / {required}";
    }

    void StartPulse()
    {
        StopPulse();
        if (_iconRect != null)
            _pulseRoutine = StartCoroutine(PulseRoutine());
    }

    void StopPulse()
    {
        if (_pulseRoutine != null)
        {
            StopCoroutine(_pulseRoutine);
            _pulseRoutine = null;
        }

        if (_iconRect != null)
            _iconRect.localScale = _iconBaseScale;
    }

    IEnumerator PulseRoutine()
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, t);

            if (_pressPulseTimer > 0f)
            {
                _pressPulseTimer -= Time.unscaledDeltaTime;
                scale = pulseMaxScale + pressPulseBoost;
            }

            if (_iconRect != null)
                _iconRect.localScale = _iconBaseScale * scale;

            yield return null;
        }
    }
}

public class QTEResult
{
    public bool Success;
    public int PressCount;
    public int RequiredPressCount;
}
