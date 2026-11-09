using UnityEngine;

/// <summary>釣魚點：按 E 開啟 <see cref="FishingUIController"/>（與 <see cref="GambleInteractable"/> 相同）。</summary>
[RequireComponent(typeof(Collider2D))]
public class FishingSpotInteractable : MonoBehaviour
{
    [SerializeField] private FishingUIController fishingUI;
    [SerializeField] private string spotDisplayName = "釣魚";
    [SerializeField] private GameObject promptUI;
    [SerializeField] private string playerTag = "Player";

    bool _playerInside;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    void Start()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInside = true;
        if (promptUI != null)
            promptUI.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInside = false;
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    public void Interact()
    {
        if (!_playerInside)
            return;

        var ui = ResolveFishingUI();
        if (ui == null)
        {
            Debug.LogWarning("[FishingSpot] 找不到 FishingUIController。");
            return;
        }

        ui.OpenFromSpot(spotDisplayName);
    }

    public bool IsPlayerInside => _playerInside;

    FishingUIController ResolveFishingUI()
    {
        if (fishingUI != null)
            return fishingUI;

        return FishingUIController.Instance;
    }
}
