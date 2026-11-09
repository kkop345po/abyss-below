using UnityEngine;

/// <summary>
/// 轉場後若仍有「孤兒」循環環境音（例如動畫 Event 播的），清掉並讓場景上的
/// <see cref="SceneBackgroundMusic"/>／<see cref="SceneAudioSettings"/> 再套用一次（不重複 Stop 進場循環）。
/// </summary>
public static class SceneAudioReapply
{
    public static void StopOrphanLoopingSfxAndReapplySceneComponents()
    {
        var am = AudioManager.GetInstance();
        if (am == null) return;

        am.StopAllLoopingSFX();

        var sbms = Object.FindObjectsByType<SceneBackgroundMusic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sbms.Length; i++)
            sbms[i].Apply(false);

        var sases = Object.FindObjectsByType<SceneAudioSettings>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sases.Length; i++)
            sases[i].Apply(false);
    }
}
