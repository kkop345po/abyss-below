using UnityEngine;

public class SceneSelectTrigger : MonoBehaviour
{
    public GameObject selectionUI; // 選擇場景的 UI 面板

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && selectionUI != null)
        {
            selectionUI.SetActive(true);
            Debug.Log("Player進入場景選擇觸發器");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && selectionUI != null)
        {
            selectionUI.SetActive(false);
        }
    }
}
