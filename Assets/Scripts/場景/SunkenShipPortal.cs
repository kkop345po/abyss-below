using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ScenePortal))]
public class SunkenShipPortal : MonoBehaviour
{
	private const string TargetScene = "Sunken ship";

	[Header("覆寫 ScenePortal 設定")]
	public bool useLoadingScene = true;
	public bool requireInteractKey = false;
	public GameObject interactPromptUI;

	private ScenePortal portal;

	void Reset()
	{
		EnsurePortal();
		Apply();
	}

	void OnValidate()
	{
		EnsurePortal();
		Apply();
	}

	private void EnsurePortal()
	{
		if (portal == null)
			portal = GetComponent<ScenePortal>();
	}

	private void Apply()
	{
		if (portal == null) return;
		portal.targetSceneName = TargetScene; // 強制只傳送到沉船
		portal.useLoadingScene = useLoadingScene;
		portal.requireInteractKey = requireInteractKey;
		if (interactPromptUI != null)
			portal.interactPromptUI = interactPromptUI;
	}
}


