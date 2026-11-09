using UnityEngine;

public static class PanelPauseManager
{
    // 使用計數器處理多面板疊加開啟的情況
    private static int pauseCounter = 0;
    private const float PausedScale = 0f;
    private const float RunningScale = 1f;

    public static bool IsPausedByPanel => pauseCounter > 0;

    public static void PushPause()
    {
        pauseCounter++;
        if (Time.timeScale != PausedScale)
            Time.timeScale = PausedScale;
    }

    public static void PopPause()
    {
        pauseCounter = Mathf.Max(0, pauseCounter - 1);
        if (pauseCounter == 0)
            Time.timeScale = RunningScale;
    }

    // 供非面板（例如ESC暫停選單）統一使用
    public static void ForceResumeAll()
    {
        pauseCounter = 0;
        Time.timeScale = RunningScale;
    }
}


