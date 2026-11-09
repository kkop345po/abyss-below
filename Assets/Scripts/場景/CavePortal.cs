using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ScenePortal))]
public class CavePortal : MonoBehaviour
{
	[Header("目標洞穴場景名")]
	public string targetSceneName = "Cave"; // 可於 Inspector 指定實際場景名
	public bool useLoadingScene = true;
	public bool requireInteractKey = false;
	public GameObject interactPromptUI;

	private ScenePortal portal;

	void Reset() { EnsurePortal(); Apply(); }
	void OnValidate() { EnsurePortal(); Apply(); }

	private void EnsurePortal()
	{
		if (portal == null) portal = GetComponent<ScenePortal>();
	}

	private void Apply()
	{
		if (portal == null) return;
		portal.targetSceneName = string.IsNullOrEmpty(targetSceneName) ? "Cave" : targetSceneName;
		portal.useLoadingScene = useLoadingScene;
		portal.requireInteractKey = requireInteractKey;
		if (interactPromptUI != null) portal.interactPromptUI = interactPromptUI;
	}
}


