using UnityEngine;
using UnityEngine.SceneManagement;

public class pauseMeun : MonoBehaviour
{
    public static bool GameIsPaused = false;

    static pauseMeun s_activeInstance;

    public GameObject pauseMeunUI;
    
    [Header("ESC 優先關閉的其他 UI 面板")]
    [Tooltip("如果這些面板有任何一個是開著的，ESC 只會關掉它們（由各自腳本處理），不會同時打開/關閉暫停選單。")]
    public GameObject[] otherUIPanels;

    void Awake()
    {
        TryResolveReferences();

        if (pauseMeunUI == null)
        {
            Debug.LogWarning($"[pauseMeun] {name} 未指定 pauseMeunUI，已停用此元件。", this);
            enabled = false;
            return;
        }

        if (s_activeInstance != null && s_activeInstance != this)
        {
            if (GetInstancePriority(this) > GetInstancePriority(s_activeInstance))
            {
                s_activeInstance.enabled = false;
                s_activeInstance = this;
            }
            else
            {
                enabled = false;
            }
            return;
        }

        s_activeInstance = this;
    }

    static int GetInstancePriority(pauseMeun candidate)
    {
        int score = 0;
        if (candidate.saveSlotPanel != null)
            score += 2;
        if (candidate.otherUIPanels != null && candidate.otherUIPanels.Length > 0)
            score += 1;
        if (!candidate.name.Contains("(1)"))
            score += 1;
        return score;
    }

    void OnDestroy()
    {
        if (s_activeInstance == this)
            s_activeInstance = null;
    }

    void TryResolveReferences()
    {
        if (pauseMeunUI != null)
            return;

        Transform child = transform.Find("PauseMenu");
        if (child != null)
        {
            pauseMeunUI = child.gameObject;
            return;
        }

        GameObject found = GameObject.Find("PauseMenu");
        if (found != null)
            pauseMeunUI = found;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 若有其他 UI 面板是開著的，優先讓它們處理 ESC（例如背包、商店、成就列表等）
            if (IsOtherUIPanelOpen())
            {
                return;
            }

            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    private bool IsOtherUIPanelOpen()
    {
        if (otherUIPanels == null) return false;
        foreach (var go in otherUIPanels)
        {
            if (go != null && go.activeSelf)
                return true;
        }
        return false;
    }

    public void Resume()
    {
        if (pauseMeunUI == null)
            return;

        pauseMeunUI.SetActive(false);
        PanelPauseManager.PopPause();
        GameIsPaused = false;
    }

    public void Pause()
    {
        if (pauseMeunUI == null)
            return;

        pauseMeunUI.SetActive(true);
        PanelPauseManager.PushPause();
        GameIsPaused = true;
    }

    public void MainMeun()
    {
        GameIsPaused = false;
        PanelPauseManager.ForceResumeAll();
        SceneManager.LoadScene("Menu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public GameObject saveSlotPanel; 

    public void OpenSaveSlotPanel()
    {
        // 關閉暫停面板，只切換 UI，不改動暫停狀態
        if (pauseMeunUI != null && pauseMeunUI.activeSelf)
        {
            pauseMeunUI.SetActive(false);
        }

        if (saveSlotPanel != null)
        {
            saveSlotPanel.SetActive(true);
        }
    }

    public void CloseSaveSlotPanel()
    {
        if (saveSlotPanel != null)
            saveSlotPanel.SetActive(false);
    }
}

