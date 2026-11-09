using System.Collections.Generic;
using UnityEngine;

/// <summary>玩家進入 2D 觸發範圍時淡化指定精靈，離開後恢復。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
[AddComponentMenu("Effects/Player Pass Through Fade 2D")]
public class PlayerPassThroughFade2D : MonoBehaviour
{
    [Header("淡化對象")]
    [Tooltip("留空時，自動取得本物件與子物件的 SpriteRenderer。可手動指定其他物件。")]
    [SerializeField] private SpriteRenderer[] targetSprites;

    [Header("透明效果")]
    [Tooltip("原始透明度的倍率。0.35 表示保留原本 35% 的不透明度。")]
    [Range(0f, 1f)] [SerializeField] private float opacityMultiplier = 0.35f;
    [Tooltip("完成一次淡入或淡出的時間（秒）。設為 0 可立即切換。")]
    [Min(0f)] [SerializeField] private float fadeDuration = 0.2f;
    [Tooltip("玩家完全離開後，等待多久才恢復（秒）。")]
    [Min(0f)] [SerializeField] private float restoreDelay = 0.1f;

    [Header("玩家辨識")]
    [Tooltip("玩家或其父物件需使用這個 Tag，預設為 Player。")]
    [SerializeField] private string playerTag = "Player";

    private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
    private float[] originalAlphas;
    private float fadeAmount;
    private float restoreAt;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Awake()
    {
        if (targetSprites == null || targetSprites.Length == 0)
            targetSprites = GetComponentsInChildren<SpriteRenderer>(true);

        originalAlphas = new float[targetSprites.Length];
        for (int i = 0; i < targetSprites.Length; i++)
            if (targetSprites[i] != null)
                originalAlphas[i] = targetSprites[i].color.a;

        if (targetSprites.Length == 0)
            Debug.LogWarning("PlayerPassThroughFade2D：找不到 SpriteRenderer，請指定淡化對象。", this);
        if (!GetComponent<BoxCollider2D>().isTrigger)
            Debug.LogWarning("PlayerPassThroughFade2D：請勾選 BoxCollider2D 的 Is Trigger。", this);
    }

    private bool IsPlayer(Collider2D other)
    {
        // 支援碰撞器放在玩家的子物件，Tag 只放在玩家根物件。
        for (Transform current = other.transform; current != null; current = current.parent)
            if (current.tag == playerTag)
                return true;
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActiveAndEnabled && IsPlayer(other))
            occupants.Add(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 元件在重疊期間重新啟用時，也能重新取得玩家。
        if (isActiveAndEnabled && IsPlayer(other))
            occupants.Add(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (occupants.Remove(other) && occupants.Count == 0)
            restoreAt = Time.time + restoreDelay;
    }

    private void LateUpdate()
    {
        // 玩家被銷毀、停用或關閉碰撞器時，Unity 不一定發送 Exit。
        int previousCount = occupants.Count;
        occupants.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        if (previousCount > 0 && occupants.Count == 0)
            restoreAt = Time.time + restoreDelay;

        float target = occupants.Count > 0 || Time.time < restoreAt ? 1f : 0f;
        fadeAmount = fadeDuration <= 0f
            ? target
            : Mathf.MoveTowards(fadeAmount, target, Time.deltaTime / fadeDuration);
        ApplyAlpha(Mathf.Lerp(1f, opacityMultiplier, fadeAmount));
    }

    private void ApplyAlpha(float multiplier)
    {
        for (int i = 0; i < targetSprites.Length; i++)
        {
            if (targetSprites[i] == null)
                continue;
            Color color = targetSprites[i].color;
            color.a = originalAlphas[i] * multiplier;
            targetSprites[i].color = color;
        }
    }

    private void OnDisable()
    {
        occupants.Clear();
        fadeAmount = 0f;
        restoreAt = 0f;
        if (originalAlphas != null)
            ApplyAlpha(1f);
    }
}
