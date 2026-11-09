using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 掛在可接收射線的 UI 物件上（例如 Button 同一層有 Image），點擊時播 <see cref="AudioManager.PlayButtonSfx"/>（按按鈕音效）。
/// </summary>
[DisallowMultipleComponent]
public class UIButtonClickSound : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.GetInstance()?.PlayButtonSfx();
    }
}
