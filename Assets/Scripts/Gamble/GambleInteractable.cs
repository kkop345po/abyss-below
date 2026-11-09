using UnityEngine;

/// <summary>
/// 掛在賭博 NPC 碰撞體上：<see cref="PlayerInteract"/> 會與 <see cref="NPCInteractable"/> 一併偵測。
/// </summary>
public class GambleInteractable : MonoBehaviour
{
    [SerializeField] private GambleUIController gambleUI;

    public void Interact()
    {
        if (gambleUI == null) return;
        AudioManager.GetInstance()?.PlayButtonSfx();
        gambleUI.OpenGambleFromNpc();
    }
}
