using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ScenePortal))]
public class MidPortal : MonoBehaviour
{
	private const string TargetScene = "Mid";
	public bool useLoadingScene = true;
	public bool requireInteractKey = false;
	public GameObject interactPromptUI;
    
    [Header("轉場（預設黑幕淡入淡出）")]
    public TransitionType transitionType = TransitionType.PixelFade;
    public bool usePixelFade = true;

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
		portal.targetSceneName = TargetScene;
		portal.useLoadingScene = useLoadingScene;
		portal.requireInteractKey = requireInteractKey;
        portal.transitionType = transitionType;
        portal.usePixelFade = usePixelFade;
		if (interactPromptUI != null) portal.interactPromptUI = interactPromptUI;
	}
}


