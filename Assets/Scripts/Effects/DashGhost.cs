using UnityEngine;

public class DashGhost : MonoBehaviour
{
    private SpriteRenderer sr;
    private float duration;
    private float elapsed = 0f;
    private Color startColor;

    public void Init(Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale, float lifeTime, Color color, bool flipX, int sortingOrder)
    {
        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale;
        
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        
        sr.sprite = sprite;
        sr.color = color;
        sr.flipX = flipX;
        sr.sortingOrder = sortingOrder - 1; // 在玩家後面
        
        startColor = color;
        duration = lifeTime;
        elapsed = 0f;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }

        float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
        sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
    }
}
