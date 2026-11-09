using UnityEngine;
using UnityEngine.UI;

public class DragItemVisual : MonoBehaviour
{
    private static DragItemVisual instance;
    public static DragItemVisual Instance => instance;

    public Image dragImage;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        dragImage.enabled = false;
    }

    public Image Show(Sprite icon)
    {
        dragImage.enabled = true;
        dragImage.sprite = icon;
        dragImage.transform.SetAsLastSibling(); // 確保顯示在最上層
        return dragImage;
    }

    public void Hide()
    {
        dragImage.enabled = false;
    }

    void Update()
    {
        if (dragImage.enabled)
        {
            transform.position = Input.mousePosition;
        }
    }
}
