using UnityEngine;
using UnityEngine.SceneManagement;

public class HarborTrigger : MonoBehaviour
{
    public Animator transitionAnimator; // 轉場動畫控制器

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (transitionAnimator != null)
            {
                transitionAnimator.SetTrigger("Start");
            }

            // 播放轉場動畫並切換場景
            Invoke(nameof(LoadScene), 1.2f); // 等待轉場動畫 1.2 秒
        }
    }

    private void LoadScene()
    {
        SceneManager.LoadScene("Harbor");
    }
}
