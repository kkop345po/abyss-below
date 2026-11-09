using UnityEngine;

public class UnderwaterZoneTrigger : MonoBehaviour
{
    public DepthZone zone = DepthZone.Shallow;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        OxygenManager oxygenManager = other.GetComponent<OxygenManager>();
        if (oxygenManager != null)
        {
            oxygenManager.SetUnderwaterState(true, zone);
            Debug.Log($"[Trigger] 進入區域：{zone}");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        OxygenManager oxygenManager = other.GetComponent<OxygenManager>();
        if (oxygenManager != null)
        {
            oxygenManager.SetUnderwaterState(false, zone);
            Debug.Log($"[Trigger] 離開區域：{zone}");
        }
    }
}
