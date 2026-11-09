using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class GoBackPortal : MonoBehaviour
{
	public enum BackDestination
	{
		PreviousScene,
		Land
	}

	public bool requireInteractKey = false;
	public KeyCode interactKey = KeyCode.E;
	public GameObject interactPromptUI;

	[Header("回程目的地")]
	public bool showDestinationChoiceUI = false;
	[Tooltip("可與多顆回程門共用；按鈕綁哪顆 GoBackPortal 皆可，實際會使用「開啟面板」的那顆門的 SpawnId 與轉場設定。")]
	public GameObject destinationChoicePanel; // 按鈕：ChoosePreviousScene / ChooseLand

	public BackDestination defaultDestination = BackDestination.PreviousScene;

	[Header("回到岸上設定（可選）")]
	public string landSceneName = "Land";
	[Tooltip("回到岸上時要生成的 SpawnId（目標場景需有 SpawnPoint）。留空則使用 Default。")]
	public string landSpawnId = "Default";

	[Header("轉場：回上一個場景（黑幕）")]
	[Tooltip("回上一個場景時使用 PixelFade 黑幕轉場（若 PixelFadeTransition 不存在則退回直接載入）。")]
	public bool usePixelFadeForPreviousScene = true;
	[Tooltip("回到上一個場景時要生成的 SpawnId（目標場景需有 SpawnPoint）。留空則不寫入 PendingSpawnId，沿用生成預設邏輯。")]
	public string previousSceneSpawnId = "Default";

	[Header("回上一海域：可選固定場景")]
	[Tooltip("填寫後，「回上一個海域」改為載入此場景（須與 Build Settings 檔名一致）。留空則仍由 SceneHistoryManager 決定上一場景。")]
	public string previousSceneOverride = "";
	[Tooltip("使用覆寫場景時的 SpawnId；留空則沿用 previousSceneSpawnId（若也留空則不寫入 PendingSpawnId）。")]
	public string previousSceneOverrideSpawnId = "";

	[Header("轉場：回岸上（海港動畫）")]
	[Tooltip("回岸上時使用 HarborTransitionManager（與 ScenePortal 的 HarborAnimation 相同流程）。")]
	public bool useHarborAnimationForLand = true;
	[Tooltip("出港（或一般海港轉場）用 Animator；目標不是 Land 時使用。")]
	public Animator harborAnimator;
	[Tooltip("回港（回到 Land）用 Animator；目標為 Land 時優先使用。若留空則回退使用 harborAnimator。")]
	public Animator returnHarborAnimator;

	[Header("選擇 UI 行為")]
	[Tooltip("顯示回程選擇面板時暫停遊戲時間（Time.timeScale=0），關閉面板後還原。")]
	public bool pauseTimeWhileChoosing = true;
	[Tooltip("顯示回程選擇面板時可用 ESC 關閉面板並恢復時間。")]
	public bool cancelChoiceWithEscape = true;

	private bool isPlayerInside = false;
	private float _nextGoBackAllowedTime = -999f;
	private bool choiceUiOpen;
	private bool pausedByThis;
	private float savedTimeScale = 1f;

	/// <summary>
	/// 多個 GoBackPortal 共用同一個 Destination 面板時，Button 往往只綁定其中一顆；
	/// 記錄「是哪顆門打開面板」，讓按鈕不論綁在哪顆上都能使用正確的 SpawnId / 轉場設定。
	/// </summary>
	private static GoBackPortal s_choicePanelOwner;

	void Reset()
	{
		var col = GetComponent<BoxCollider2D>();
		col.isTrigger = true;
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (!other.CompareTag("Player")) return;
		isPlayerInside = true;
		if (interactPromptUI != null) interactPromptUI.SetActive(requireInteractKey && !showDestinationChoiceUI);
		if (destinationChoicePanel != null) destinationChoicePanel.SetActive(false);
		if (!requireInteractKey) GoBack();
	}

	void OnTriggerExit2D(Collider2D other)
	{
		if (!other.CompareTag("Player")) return;
		isPlayerInside = false;
		if (interactPromptUI != null) interactPromptUI.SetActive(false);
		CloseChoiceUiIfOpen();
	}

	void Update()
	{
		if (cancelChoiceWithEscape && choiceUiOpen && Input.GetKeyDown(KeyCode.Escape))
		{
			CloseChoiceUiIfOpen();
			return;
		}

		if (requireInteractKey && isPlayerInside && Input.GetKeyDown(interactKey))
		{
			AudioManager.GetInstance()?.PlayButtonSfx();
			GoBack();
		}
	}

	private void GoBack()
	{
		if (Time.unscaledTime < _nextGoBackAllowedTime)
			return;

		if (showDestinationChoiceUI && destinationChoicePanel != null)
		{
			OpenChoiceUi();
			return;
		}

		_nextGoBackAllowedTime = Time.unscaledTime + 0.85f;
		ExecuteBack(defaultDestination);
	}

	private GoBackPortal ResolvePortalForUiClick()
	{
		return s_choicePanelOwner != null ? s_choicePanelOwner : this;
	}

	// UI Button: 回上一個場景（可綁在任意物件上；會優先使用「開啟選擇面板」的那顆 GoBackPortal）
	public void ChoosePreviousScene()
	{
		ResolvePortalForUiClick().ExecuteBack(BackDestination.PreviousScene);
	}

	// UI Button: 回岸上
	public void ChooseLand()
	{
		ResolvePortalForUiClick().ExecuteBack(BackDestination.Land);
	}

	void OnDisable()
	{
		if (s_choicePanelOwner == this)
			s_choicePanelOwner = null;
		// 物件被切場景/銷毀時，避免把 timeScale 卡在 0
		CloseChoiceUiIfOpen();
	}

	private void OpenChoiceUi()
	{
		if (destinationChoicePanel == null) return;
		if (choiceUiOpen) return;

		s_choicePanelOwner = this;
		destinationChoicePanel.SetActive(true);
		choiceUiOpen = true;

		if (pauseTimeWhileChoosing)
		{
			savedTimeScale = Time.timeScale;
			Time.timeScale = 0f;
			pausedByThis = true;
		}

		AudioManager.GetInstance()?.PushChoicePanelSfxSuppress();
	}

	private void CloseChoiceUiIfOpen()
	{
		if (!choiceUiOpen) return;

		if (s_choicePanelOwner == this)
			s_choicePanelOwner = null;

		if (destinationChoicePanel != null)
			destinationChoicePanel.SetActive(false);

		if (pausedByThis)
		{
			Time.timeScale = Mathf.Approximately(savedTimeScale, 0f) ? 1f : savedTimeScale;
			pausedByThis = false;
		}

		choiceUiOpen = false;

		AudioManager.GetInstance()?.PopChoicePanelSfxSuppress();
	}

	private void ExecuteBack(BackDestination dest)
	{
		if (Time.unscaledTime < _nextGoBackAllowedTime)
			return;

		// 開始切場景前務必還原時間，避免載入後卡在 timeScale=0
		CloseChoiceUiIfOpen();
		
		if (dest == BackDestination.PreviousScene)
		{
			SceneHistoryManager history = SceneHistoryManager.EnsureInstance();

			if (PlayerPositionManager.Instance != null)
				PlayerPositionManager.Instance.SaveCurrentPosition();

			string sceneToLoad;
			bool useOverride = !string.IsNullOrWhiteSpace(previousSceneOverride);
			if (useOverride)
			{
				sceneToLoad = previousSceneOverride.Trim();
			}
			else if (!history.TryConsumePreviousScene(out sceneToLoad))
			{
				Debug.LogWarning("[GoBackPortal] 無法返回上一場景：沒有可用的 previousScene（請確認進入本場景前有經過會記錄歷史的轉場，例如 PixelFade / ScenePortal）。");
				return;
			}

			string spawnForPrevious = useOverride
				? (!string.IsNullOrEmpty(previousSceneOverrideSpawnId) ? previousSceneOverrideSpawnId : previousSceneSpawnId)
				: previousSceneSpawnId;
			if (!string.IsNullOrEmpty(spawnForPrevious))
				SceneSpawnRouter.SetPendingSpawnId(spawnForPrevious);

			if (PlayerStateManager.Instance != null)
				PlayerStateManager.Instance.OnSceneTransition();

			_nextGoBackAllowedTime = Time.unscaledTime + 0.85f;

			if (usePixelFadeForPreviousScene && PixelFadeTransition.Instance != null)
			{
				PixelFadeTransition.Instance.TransitionToScene(sceneToLoad);
				return;
			}

			SceneManager.LoadScene(sceneToLoad);
			return;
		}

		if (dest == BackDestination.Land)
		{
			if (!string.IsNullOrEmpty(landSpawnId))
				SceneSpawnRouter.SetPendingSpawnId(landSpawnId);

			if (PlayerStateManager.Instance != null)
				PlayerStateManager.Instance.OnSceneTransition();

			_nextGoBackAllowedTime = Time.unscaledTime + 0.85f;

			if (useHarborAnimationForLand && HarborTransitionManager.Instance != null)
			{
				bool isReturningToLand = landSceneName == "Land";
				Animator configured = isReturningToLand
					? (returnHarborAnimator != null ? returnHarborAnimator : harborAnimator)
					: harborAnimator;
				Animator animatorToUse = configured != null ? configured : GetComponent<Animator>();

				HarborTransitionManager.Instance.TransitionToScene(landSceneName, animatorToUse);
				return;
			}

			if (PixelFadeTransition.Instance != null)
			{
				PixelFadeTransition.Instance.TransitionToScene(landSceneName);
				return;
			}

			SceneManager.LoadScene(landSceneName);
			return;
		}
	}
}


