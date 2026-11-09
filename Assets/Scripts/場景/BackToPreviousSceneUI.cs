using UnityEngine;

public class BackToPreviousSceneUI : MonoBehaviour
{
    public void OnConfirm()
    {
        var trigger = GetComponentInParent<BackToPreviousSceneTrigger>();
        trigger?.NotifyConfirmClosedFromUi();

        SceneHistoryManager.EnsureInstance().GoBack();
    }

    public void OnCancel()
    {
        var trigger = GetComponentInParent<BackToPreviousSceneTrigger>();
        trigger?.NotifyConfirmClosedFromUi();

        gameObject.SetActive(false);
    }
}  