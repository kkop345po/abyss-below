using UnityEngine;
using UnityEngine.UI;
using TMPro; // [New] 引入 TextMeshPro 命名空間
using System.Collections;

public class PickupLine : MonoBehaviour
{
    public TMP_Text uiText; // [Modified] 改用 TMP_Text 以支援 TextMeshPro
    public CanvasGroup canvasGroup;
    public float lifetime = 2f;
    public float fadeDuration = 0.5f;

    private float timer;
    private bool isFading;

    public void Initialize(string text)
    {
        if (uiText != null) uiText.text = text;
        timer = lifetime;
        isFading = false;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    public void UpdateText(string text)
    {
        if (uiText != null) uiText.text = text;
        // 重置計時器，讓文字多留一會
        timer = lifetime;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        isFading = false;
    }

    private void Update()
    {
        if (isFading) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            StartCoroutine(FadeOut());
        }
    }

    private IEnumerator FadeOut()
    {
        isFading = true;
        float elapsed = 0f;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
