using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoIntroLoader : MonoBehaviour
{
	[Header("影片設定")]
	[SerializeField] private bool useExistingVideoPlayer = false; // 是否使用場景中已存在的 VideoPlayer
	[SerializeField] private VideoPlayer existingVideoPlayer; // 若 useExistingVideoPlayer 為 true，指定要使用的 VideoPlayer
	[SerializeField] private VideoClip videoClip; // 若未指定，將使用 StreamingAssets 的檔名
	[SerializeField] private string streamingAssetsFileName; // 例如：Intro.mp4（放在 Assets/StreamingAssets）
	[SerializeField] private bool skipWithAnyKey = true;
	[SerializeField] private bool skipWithClick = true;
	[SerializeField] private float inputSkipDelaySeconds = 0.25f; // 避免按下開始按鈕的輸入立刻略過
	[SerializeField] private KeyCode explicitSkipKey = KeyCode.None; // 可選：指定按鍵略過，None 表示不使用
	[SerializeField] private bool pauseGameDuringVideo = true; // 影片播放時暫停玩家/遊戲時間

	[Header("載入設定")]
	[SerializeField] private SceneLoader sceneLoader;
	[SerializeField] private string targetSceneName = "GameScene";
	[SerializeField] private bool useLoadingScene = true; // true: 黑幕漸層轉場載入；false: 直接載入

	[Header("片尾結束後（可選）")]
	[Tooltip("勾選後：影片播完/略過後會先重置整個遊戲資料，然後載入 Menu。")]
	[SerializeField] private bool resetGameAndReturnToMenuAfterVideo = false;
	[SerializeField] private string menuSceneName = "Menu";

	[Header("片尾後：教學圖 → 直接進場景（不經 LoadingScene）")]
	[Tooltip("勾選後：不經 LoadingScene，直接載入 targetSceneName（例如 Land）。若有指定教學圖，會先全螢幕顯示，任意鍵關閉後才載入。")]
	[SerializeField] private bool afterVideoShowTutorialThenDirectLoad = false;
	[Tooltip("可選；留空則片尾後直接進場景，不顯示教學圖。")]
	[SerializeField] private Sprite tutorialSpriteAfterVideo;
	[Tooltip("顯示教學圖時是否暫停遊戲（與其他 UI 面板一致）。")]
	[SerializeField] private bool pauseDuringPostVideoTutorial = true;

	[Header("顯示層設定")]
	[SerializeField] private Canvas parentCanvas; // 可選：若留空，會自動建立全螢幕Canvas
	[SerializeField] private Color backgroundColor = Color.black;
	[SerializeField] private GameObject[] hideDuringVideo; // 播放影片時隱藏的物件（例如主選單UI）

	private GameObject playerRoot;
	private VideoPlayer videoPlayer;
	private AudioSource audioSource;
	private RawImage rawImage;
	private RenderTexture renderTexture;
	private bool isPlaying;
	private bool isUsingExistingPlayer = false; // 標記是否使用現有的 VideoPlayer
	private GameObject createdBackgroundGO; // 記錄建立的背景物件
	private GameObject createdRawImageGO; // 記錄建立的 RawImage 物件
	private float _timeScaleBeforeVideo = 1f;
	private bool _timeScalePausedByVideo = false;
	/// <summary>若本次 PlayAndLoad 有指定，優先於 Inspector 的 targetSceneName；載入後清除。</summary>
	private string _pendingTargetScene;

	/// <param name="sceneAfterVideo">片尾播畢後要載入的場景（例：大 BOSS 結局後填 Menu）；null 或空則用 targetSceneName。</param>
	public void PlayAndLoad(string sceneAfterVideo = null)
	{
		if (isPlaying) return;
		_pendingTargetScene = string.IsNullOrWhiteSpace(sceneAfterVideo) ? null : sceneAfterVideo.Trim();
		StartCoroutine(PlayCoroutine());
	}

	private IEnumerator PlayCoroutine()
	{
		isPlaying = true;
		var am = AudioManager.GetInstance();
		if (am != null) am.PushVideoPlaybackMute();
		ApplyVideoPauseIfNeeded();

		// 隱藏主選單UI（如果指定）
		if (hideDuringVideo != null)
		{
			foreach (var obj in hideDuringVideo)
			{
				if (obj != null) obj.SetActive(false);
			}
		}

		SetupUI();
		SetupVideoPlayer();

		// 準備影片
		Debug.Log($"[VideoIntroLoader] 準備播放影片... VideoPlayer.isPrepared: {videoPlayer.isPrepared}, clip: {(videoPlayer.clip != null ? videoPlayer.clip.name : "null")}");
		
		// 如果已經準備好了，直接跳過準備步驟
		if (!videoPlayer.isPrepared)
		{
			videoPlayer.Prepare();
			float prepareStartTime = Time.realtimeSinceStartup;
			while (!videoPlayer.isPrepared)
			{
				if (Time.realtimeSinceStartup - prepareStartTime > 5f)
				{
					Debug.LogError("[VideoIntroLoader] 影片準備超時！");
					CleanupPlayer();
					if (am != null) am.PopVideoPlaybackMute();
					RestoreVideoPauseIfNeeded();
					yield return LoadTargetSceneRoutine();
					isPlaying = false;
					yield break;
				}
				yield return null;
			}
			Debug.Log("[VideoIntroLoader] 影片準備完成");
		}
		else
		{
			Debug.Log("[VideoIntroLoader] VideoPlayer 已經準備完成，跳過準備步驟");
		}
		
		Debug.Log("[VideoIntroLoader] 開始播放影片");

		// 開始播放
		Debug.Log($"[VideoIntroLoader] 嘗試播放 - VideoPlayer.clip: {(videoPlayer.clip != null ? videoPlayer.clip.name : "null")}, renderMode: {videoPlayer.renderMode}, targetTexture: {(videoPlayer.targetTexture != null ? "有" : "無")}");
		videoPlayer.Play();
		if (audioSource != null) audioSource.Play();
		Debug.Log($"[VideoIntroLoader] 播放指令已發出 - VideoPlayer.isPlaying: {videoPlayer.isPlaying}");

		// 等待至少一幀確保影片開始播放
		yield return null;
		Debug.Log($"[VideoIntroLoader] 等待一幀後 - VideoPlayer.isPlaying: {videoPlayer.isPlaying}");

		// 確認影片真的在播放
		float waitStart = Time.realtimeSinceStartup;
		int frameCount = 0;
		while (!videoPlayer.isPlaying && Time.realtimeSinceStartup - waitStart < 2f)
		{
			frameCount++;
			if (frameCount % 30 == 0) // 每30幀輸出一次狀態
			{
				Debug.Log($"[VideoIntroLoader] 等待播放中... ({Time.realtimeSinceStartup - waitStart:F2}秒) - isPlaying: {videoPlayer.isPlaying}, isPrepared: {videoPlayer.isPrepared}");
			}
			yield return null;
		}

		if (!videoPlayer.isPlaying)
		{
			Debug.LogError($"[VideoIntroLoader] 影片無法播放！isPrepared: {videoPlayer.isPrepared}, clip: {(videoPlayer.clip != null ? videoPlayer.clip.name : "null")}, renderMode: {videoPlayer.renderMode}, 跳過影片直接載入場景");
			CleanupPlayer();
			if (am != null) am.PopVideoPlaybackMute();
			RestoreVideoPauseIfNeeded();
			yield return LoadTargetSceneRoutine();
			isPlaying = false;
			yield break;
		}
		
		Debug.Log("[VideoIntroLoader] 影片開始播放成功！");

		// 等待播放完成或被略過
		bool completed = false;
		VideoPlayer.EventHandler onComplete = (VideoPlayer vp) => { completed = true; };
		videoPlayer.loopPointReached += onComplete;
		float startedAt = Time.realtimeSinceStartup;

		while (!completed && videoPlayer.isPlaying)
		{
			// 正常跳過邏輯
			float elapsed = Time.realtimeSinceStartup - startedAt;
			bool allowSkip = elapsed >= inputSkipDelaySeconds;
			if (allowSkip)
			{
				if (explicitSkipKey != KeyCode.None && Input.GetKeyDown(explicitSkipKey)) break;
				if (explicitSkipKey == KeyCode.None)
				{
					if (skipWithAnyKey && Input.anyKeyDown) break;
					if (skipWithClick && Input.GetMouseButtonDown(0)) break;
				}
			}
			yield return null;
		}

		// 清理事件訂閱
		videoPlayer.loopPointReached -= onComplete;

		CleanupPlayer();

		if (am != null) am.PopVideoPlaybackMute();
		RestoreVideoPauseIfNeeded();
		yield return LoadTargetSceneRoutine();
		
		isPlaying = false;
	}

	private void ApplyVideoPauseIfNeeded()
	{
		if (!pauseGameDuringVideo || _timeScalePausedByVideo) return;
		_timeScaleBeforeVideo = Time.timeScale;
		Time.timeScale = 0f;
		_timeScalePausedByVideo = true;
	}

	private void RestoreVideoPauseIfNeeded()
	{
		if (!_timeScalePausedByVideo) return;
		Time.timeScale = _timeScaleBeforeVideo;
		_timeScalePausedByVideo = false;
	}

	private void SetupUI()
	{
		// Canvas
		Canvas canvas = parentCanvas;
		if (canvas == null)
		{
			GameObject canvasGO = new GameObject("IntroVideoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			canvas = canvasGO.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 9999; // 確保在最上層
			CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920, 1080);
		}
		else
		{
			// 確保使用現有Canvas時也在最上層
			canvas.sortingOrder = 9999;
		}

		// 背景遮罩
		createdBackgroundGO = new GameObject("IntroBackground", typeof(Image));
		createdBackgroundGO.transform.SetParent(canvas.transform, false);
		Image bg = createdBackgroundGO.GetComponent<Image>();
		bg.color = backgroundColor;
		RectTransform bgRt = bg.rectTransform;
		bgRt.anchorMin = Vector2.zero;
		bgRt.anchorMax = Vector2.one;
		bgRt.offsetMin = Vector2.zero;
		bgRt.offsetMax = Vector2.zero;

		// 顯示影片的 RawImage
		createdRawImageGO = new GameObject("IntroVideo", typeof(RawImage));
		createdRawImageGO.transform.SetParent(canvas.transform, false);
		rawImage = createdRawImageGO.GetComponent<RawImage>();
		RectTransform rt = rawImage.rectTransform;
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;

		// 容器方便一次清掉
		playerRoot = canvas.gameObject;
	}

	private void SetupVideoPlayer()
	{
		if (useExistingVideoPlayer && existingVideoPlayer != null)
		{
			// 使用現有的 VideoPlayer
			Debug.Log("[VideoIntroLoader] 使用場景中現有的 VideoPlayer");
			videoPlayer = existingVideoPlayer;
			isUsingExistingPlayer = true;
			
			// 取得現有的 AudioSource（如果有的話）
			audioSource = videoPlayer.GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = videoPlayer.gameObject.AddComponent<AudioSource>();
			}

			// 如果現有的 VideoPlayer 是 RenderTexture 模式，使用它的 targetTexture
			if (videoPlayer.renderMode == VideoRenderMode.RenderTexture && videoPlayer.targetTexture != null)
			{
				renderTexture = videoPlayer.targetTexture;
				rawImage.texture = renderTexture;
				Debug.Log("[VideoIntroLoader] 使用現有 VideoPlayer 的 RenderTexture");
			}
			else
			{
				// 如果現有的 VideoPlayer 不是 RenderTexture 模式，建立新的 RenderTexture 並設定
				renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
				rawImage.texture = renderTexture;
				videoPlayer.renderMode = VideoRenderMode.RenderTexture;
				videoPlayer.targetTexture = renderTexture;
				Debug.Log("[VideoIntroLoader] 將現有 VideoPlayer 設定為 RenderTexture 模式");
			}

			// 設定影片來源（如果 VideoPlayer 中還沒有設定）
			if (videoClip != null && videoPlayer.clip == null)
			{
				videoPlayer.source = VideoSource.VideoClip;
				videoPlayer.clip = videoClip;
				Debug.Log($"[VideoIntroLoader] 設定現有 VideoPlayer 的 VideoClip: {videoClip.name}");
			}
			else if (!string.IsNullOrEmpty(streamingAssetsFileName) && string.IsNullOrEmpty(videoPlayer.url))
			{
				videoPlayer.source = VideoSource.Url;
				string url = System.IO.Path.Combine(Application.streamingAssetsPath, streamingAssetsFileName);
				videoPlayer.url = url;
				Debug.Log($"[VideoIntroLoader] 設定現有 VideoPlayer 的 URL: {url}");
			}
			else
			{
				Debug.Log("[VideoIntroLoader] 使用現有 VideoPlayer 的原有設定");
			}

			// 確保設定正確
			videoPlayer.playOnAwake = false;
			videoPlayer.isLooping = false;
		}
		else
		{
			// 自動建立新的 VideoPlayer
			Debug.Log("[VideoIntroLoader] 自動建立新的 VideoPlayer");
			GameObject vpGO = new GameObject("VideoPlayer", typeof(VideoPlayer), typeof(AudioSource));
			vpGO.transform.SetParent(playerRoot.transform, false);

			videoPlayer = vpGO.GetComponent<VideoPlayer>();
			audioSource = vpGO.GetComponent<AudioSource>();
			isUsingExistingPlayer = false;

			// RenderTexture 輸出到 UI RawImage
			renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
			rawImage.texture = renderTexture;
			
			// 設定 VideoPlayer 的 RenderMode 為 RenderTexture（重要！）
			videoPlayer.renderMode = VideoRenderMode.RenderTexture;
			videoPlayer.targetTexture = renderTexture;

			// 視訊來源
			if (videoClip != null)
			{
				videoPlayer.source = VideoSource.VideoClip;
				videoPlayer.clip = videoClip;
				Debug.Log($"[VideoIntroLoader] 使用 VideoClip: {videoClip.name}");
			}
			else if (!string.IsNullOrEmpty(streamingAssetsFileName))
			{
				videoPlayer.source = VideoSource.Url;
				string url = System.IO.Path.Combine(Application.streamingAssetsPath, streamingAssetsFileName);
				videoPlayer.url = url;
				Debug.Log($"[VideoIntroLoader] 使用 StreamingAssets URL: {url}");
			}
			else
			{
				Debug.LogError("[VideoIntroLoader] 未設定 VideoClip 或 StreamingAssetsFileName！");
			}

			// 音訊輸出
			videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
			videoPlayer.EnableAudioTrack(0, true);
			videoPlayer.SetTargetAudioSource(0, audioSource);
			audioSource.playOnAwake = false;
			audioSource.loop = false;

			videoPlayer.playOnAwake = false;
			videoPlayer.isLooping = false;
		}
	}

	private void CleanupPlayer()
	{
		if (videoPlayer != null)
		{
			if (videoPlayer.isPlaying) videoPlayer.Stop();
			
			// 如果使用現有的 VideoPlayer，不要清除它的 targetTexture
			if (!isUsingExistingPlayer)
			{
				videoPlayer.targetTexture = null;
			}
		}
		if (audioSource != null && audioSource.isPlaying)
		{
			audioSource.Stop();
		}
		if (renderTexture != null && !isUsingExistingPlayer)
		{
			// 只有自己建立的 RenderTexture 才需要釋放
			RenderTexture.active = null;
			renderTexture.Release();
		}
		// 清理建立的 UI 元素（背景和 RawImage）
		if (createdBackgroundGO != null)
		{
			Destroy(createdBackgroundGO);
		}
		if (createdRawImageGO != null)
		{
			Destroy(createdRawImageGO);
		}
		
		// 只有自己建立的 Canvas 才需要刪除
		if (playerRoot != null && parentCanvas == null && playerRoot.name == "IntroVideoCanvas")
		{
			Destroy(playerRoot);
		}
	}

	private IEnumerator LoadTargetSceneRoutine()
	{
		// 片尾模式：播完直接重置並回主選單
		if (resetGameAndReturnToMenuAfterVideo)
		{
			// 解除任何暫停，避免回選單卡住
			Time.timeScale = 1f;
			try
			{
				SaveSystem.ResetAllData();
			}
			catch (System.Exception e)
			{
				Debug.LogWarning($"[VideoIntroLoader] ResetAllData 失敗（仍會回 Menu）: {e.Message}");
			}

			string menu = SceneLoader.SanitizeGameSceneName(string.IsNullOrWhiteSpace(menuSceneName) ? "Menu" : menuSceneName.Trim());
			if (sceneLoader == null)
				sceneLoader = FindObjectOfType<SceneLoader>();
			if (sceneLoader != null)
				sceneLoader.LoadSceneWithLoading(menu);
			else
				SceneManager.LoadScene(menu);
			yield break;
		}

		string loadScene = _pendingTargetScene ?? targetSceneName;
		_pendingTargetScene = null;

		if (string.IsNullOrWhiteSpace(loadScene))
		{
			Debug.LogError("[VideoIntroLoader] 目標場景名稱為空，無法載入。");
			yield break;
		}

		loadScene = loadScene.Trim();
		loadScene = SceneLoader.SanitizeGameSceneName(loadScene);

		bool skipLoading = afterVideoShowTutorialThenDirectLoad;
		if (skipLoading && tutorialSpriteAfterVideo != null)
			yield return ShowFullscreenTutorialCoroutine(tutorialSpriteAfterVideo);

		if (sceneLoader == null)
			sceneLoader = FindObjectOfType<SceneLoader>();

		bool useLoading = useLoadingScene && !skipLoading;
		if (sceneLoader != null)
		{
			if (useLoading)
				sceneLoader.LoadSceneWithLoading(loadScene);
			else
				sceneLoader.LoadSceneDirectly(loadScene);
		}
		else
			SceneManager.LoadScene(loadScene);
	}

	/// <summary>全螢幕教學圖，任意鍵關閉。</summary>
	private IEnumerator ShowFullscreenTutorialCoroutine(Sprite sprite)
	{
		if (sprite == null) yield break;

		GameObject root = new GameObject("PostVideoTutorialOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		var canvas = root.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 10000;
		var scaler = root.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);

		var bgGo = new GameObject("Backdrop", typeof(Image));
		bgGo.transform.SetParent(root.transform, false);
		var bgImg = bgGo.GetComponent<Image>();
		bgImg.color = Color.black;
		var bgRt = bgGo.GetComponent<RectTransform>();
		bgRt.anchorMin = Vector2.zero;
		bgRt.anchorMax = Vector2.one;
		bgRt.offsetMin = Vector2.zero;
		bgRt.offsetMax = Vector2.zero;

		var imgGo = new GameObject("TutorialImage", typeof(Image));
		imgGo.transform.SetParent(root.transform, false);
		var img = imgGo.GetComponent<Image>();
		img.sprite = sprite;
		img.preserveAspect = true;
		img.color = Color.white;
		var rt = imgGo.GetComponent<RectTransform>();
		rt.anchorMin = new Vector2(0.04f, 0.04f);
		rt.anchorMax = new Vector2(0.96f, 0.96f);
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;

		if (pauseDuringPostVideoTutorial)
			PanelPauseManager.PushPause();

		yield return null;
		yield return null;

		while (!Input.anyKeyDown)
			yield return null;

		if (pauseDuringPostVideoTutorial)
			PanelPauseManager.PopPause();

		Destroy(root);
	}
}


