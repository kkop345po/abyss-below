using UnityEngine;

// 掛在任何需要「開啟即暫停」的面板根物件上
public class PauseOnEnable : MonoBehaviour
{
    [Tooltip("啟用時是否自動暫停 Time.timeScale")] public bool pauseOnEnable = true;

    private void OnEnable()
    {
        if (pauseOnEnable)
            PanelPauseManager.PushPause();
    }

    private void OnDisable()
    {
        if (pauseOnEnable)
            PanelPauseManager.PopPause();
    }
}


