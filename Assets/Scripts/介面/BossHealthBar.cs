using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Boss 血條 UI（螢幕空間 Slider）。勿與玩家 HP 的 Image.fillAmount 混用。
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    public Slider hpSlider;
    public Image fillImage;
    public TMP_Text bossNameText;
    public Color normalColor = Color.red;
    public Color rageColor = new Color(1f, 0.2f, 0.2f);
    public float flashSpeed = 5f;
    [Tooltip("Boss 死亡後血條淡出秒數")]
    public float deathFadeDuration = 0.6f;
    [Tooltip("進場動畫播完後關閉 Animator，避免與 Slider 血量搶 RectTransform")]
    public bool playIntroAnimation = true;

    private bool isRageMode;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;
    private Animator _introAnimator;
    private Vector2 _anchoredPosAtAwake;
    private int _cachedMaxHp = 1;
    private int _lastCurrentHp = -1;
    private bool _introFinished;
    private Coroutine _introCoroutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        var rt = transform as RectTransform;
        if (rt != null)
            _anchoredPosAtAwake = rt.anchoredPosition;

        _introAnimator = GetComponent<Animator>();
        ResolveSliderReferences();

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (_introCoroutine != null)
        {
            StopCoroutine(_introCoroutine);
            _introCoroutine = null;
        }

        if (playIntroAnimation && !_introFinished && _introAnimator != null)
            _introCoroutine = StartCoroutine(DisableAnimatorAfterIntro());
        else if (_introAnimator != null)
            _introAnimator.enabled = false;

        ApplySliderHealth(_lastCurrentHp >= 0 ? _lastCurrentHp : _cachedMaxHp);
    }

    private void OnDisable()
    {
        if (_introCoroutine != null)
        {
            StopCoroutine(_introCoroutine);
            _introCoroutine = null;
        }

        if (_introAnimator != null)
            _introAnimator.enabled = false;
    }

    private void LateUpdate()
    {
        if (_introAnimator != null && _introAnimator.enabled)
            return;

        if (hpSlider == null || _cachedMaxHp <= 0 || _lastCurrentHp < 0)
            return;

        bool needsFix = Mathf.RoundToInt(hpSlider.maxValue) != _cachedMaxHp
            || Mathf.RoundToInt(hpSlider.value) != _lastCurrentHp;
        if (needsFix)
            ApplySliderHealth(_lastCurrentHp);
    }

    void ResolveSliderReferences()
    {
        if (hpSlider == null)
            hpSlider = GetComponent<Slider>();

        if (hpSlider != null && hpSlider.fillRect != null)
        {
            var fill = hpSlider.fillRect.GetComponent<Image>();
            if (fill != null)
                fillImage = fill;
        }
    }

    IEnumerator DisableAnimatorAfterIntro()
    {
        _introAnimator.enabled = true;
        float wait = 0.95f;
        if (_introAnimator.runtimeAnimatorController != null)
        {
            var clips = _introAnimator.runtimeAnimatorController.animationClips;
            if (clips != null && clips.Length > 0)
                wait = Mathf.Max(0.1f, clips[0].length);
        }

        yield return new WaitForSeconds(wait);
        _introAnimator.enabled = false;
        _introFinished = true;
        _introCoroutine = null;

        var rt = transform as RectTransform;
        if (rt != null)
            rt.anchoredPosition = _anchoredPosAtAwake;

        if (_lastCurrentHp >= 0)
            ApplySliderHealth(_lastCurrentHp);
    }

    /// <param name="currentHP">若 &lt; 0 則視為滿血。</param>
    public void Initialize(int maxHP, int currentHP = -1)
    {
        ResolveSliderReferences();

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        _cachedMaxHp = Mathf.Max(1, maxHP);
        int displayHp = currentHP < 0 ? _cachedMaxHp : Mathf.Clamp(currentHP, 0, _cachedMaxHp);
        _lastCurrentHp = displayHp;
        ApplySliderHealth(displayHp);
        ApplyFillColor(normalColor);
    }

    public void SetBossName(string name)
    {
        if (bossNameText != null)
            bossNameText.text = name;
    }

    public void UpdateHealth(int currentHP)
    {
        ResolveSliderReferences();

        if (hpSlider == null)
        {
            Debug.LogWarning("[BossHealthBar] UpdateHealth: hpSlider 為空，請在 Inspector 指定 Slider（本物件或子物件）。");
            return;
        }

        _lastCurrentHp = Mathf.Clamp(currentHP, 0, _cachedMaxHp > 0 ? _cachedMaxHp : Mathf.Max(1, currentHP));
        ApplySliderHealth(_lastCurrentHp);
    }

    void ApplySliderHealth(int currentHP)
    {
        if (hpSlider == null)
            return;

        int max = Mathf.Max(1, _cachedMaxHp);
        currentHP = Mathf.Clamp(currentHP, 0, max);

        hpSlider.minValue = 0f;
        hpSlider.maxValue = max;
        hpSlider.wholeNumbers = true;
        hpSlider.SetValueWithoutNotify(currentHP);
    }

    public void ForceZeroAndFadeOut()
    {
        _lastCurrentHp = 0;
        ApplySliderHealth(0);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutRoutine());
    }

    public void SetRageMode(bool rage)
    {
        isRageMode = rage;
        if (!isRageMode)
            ApplyFillColor(normalColor);
    }

    void Update()
    {
        if (!isRageMode || fillImage == null) return;

        float t = Mathf.PingPong(Time.time * flashSpeed, 1f);
        fillImage.color = Color.Lerp(normalColor, Color.white, t * 0.5f);
    }

    void ApplyFillColor(Color c)
    {
        if (fillImage != null)
            fillImage.color = c;
    }

    IEnumerator FadeOutRoutine()
    {
        if (canvasGroup == null)
        {
            gameObject.SetActive(false);
            yield break;
        }

        float duration = Mathf.Max(0.01f, deathFadeDuration);
        float t = 0f;
        float start = canvasGroup.alpha;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
