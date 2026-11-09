using UnityEngine;
using UnityEngine.UI;

public class PlayerDisplayController : MonoBehaviour
{
    [Header("UI 圖層")]
    public Image baseImage;            // 人物基本樣貌圖
    public Image weaponOverlayImage;   // 武器圖層疊圖

    [Header("Sprite 資料")]
    public Sprite baseSprite;          // 人物底圖（例如 player_idle）
    public Sprite weaponNoneSprite;    // 無武器時顯示圖（可為 null）
    public Sprite weaponSpearSprite;   // 拿矛
    public Sprite weaponTridentSprite; // 拿三叉戟

    void Start()
    {
        // 預設顯示人物與無武器
        ShowBaseCharacter();
        UpdateWeaponDisplay("none");
    }

    public void ShowBaseCharacter()
    {
        if (baseImage != null && baseSprite != null)
            baseImage.sprite = baseSprite;
    }

    public void UpdateWeaponDisplay(string weaponId)
    {
        if (weaponOverlayImage == null) return;

        switch (weaponId)
        {
            case "spear":
                weaponOverlayImage.sprite = weaponSpearSprite;
                break;
            case "trident":
                weaponOverlayImage.sprite = weaponTridentSprite;
                break;
            default:
                weaponOverlayImage.sprite = weaponNoneSprite;
                break;
        }

        // 顯示武器圖
        weaponOverlayImage.color = weaponOverlayImage.sprite != null ? Color.white : new Color(1, 1, 1, 0);
    }
}
