using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactRange = 2f;

    void Start()
    {
        Debug.Log("[PlayerInteract] ??????");
    }

    /// <summary>
    /// Collider ?????????????NPCInteractable ??????????????????§î???
    /// </summary>
    private static NPCInteractable FindNpcInteractable(Collider2D hit)
    {
        if (hit == null) return null;
        return hit.GetComponent<NPCInteractable>()
               ?? hit.GetComponentInParent<NPCInteractable>()
               ?? hit.GetComponentInChildren<NPCInteractable>();
    }

    private static GambleInteractable FindGambleInteractable(Collider2D hit)
    {
        if (hit == null) return null;
        return hit.GetComponent<GambleInteractable>()
               ?? hit.GetComponentInParent<GambleInteractable>()
               ?? hit.GetComponentInChildren<GambleInteractable>();
    }

    private static FishingSpotInteractable FindFishingSpot(Collider2D hit)
    {
        if (hit == null) return null;
        return hit.GetComponent<FishingSpotInteractable>()
               ?? hit.GetComponentInParent<FishingSpotInteractable>()
               ?? hit.GetComponentInChildren<FishingSpotInteractable>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (FishingUIController.Instance != null && FishingUIController.Instance.BlocksGameplayInput)
                return;

            if (FishingManager.Instance != null && FishingManager.Instance.IsFishing)
                return;

            // ????? E ?Y????????????????? Interact ???_???
            if (DialogueManager.LastDialogueEndedFrame == Time.frameCount)
                return;

            foreach (var dm in FindObjectsByType<DialogueManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (dm != null && dm.TryConsumeInteractKey())
                    return;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRange);

            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                NPCInteractable npc = FindNpcInteractable(hit);
                if (npc != null)
                {
                    npc.Interact();
                    break;
                }

                GambleInteractable gamble = FindGambleInteractable(hit);
                if (gamble != null)
                {
                    gamble.Interact();
                    break;
                }

                FishingSpotInteractable fishingSpot = FindFishingSpot(hit);
                if (fishingSpot != null && fishingSpot.IsPlayerInside)
                {
                    fishingSpot.Interact();
                    break;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
