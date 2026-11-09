using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 將這個腳本掛載到包含音量 Slider 的設定介面上，
/// 接著把對應的 UI 滑桿拖入即可自動與 AudioManager 綁定！
/// </summary>
public class VolumeSettingsUI : MonoBehaviour
{
    [Header("音量滑桿 (UI Sliders)")]
    [Tooltip("請確保 Slider 的 Min Value = 0.0001, Max Value = 1")]
    public Slider masterSlider;
    [Tooltip("請確保 Slider 的 Min Value = 0.0001, Max Value = 1")]
    public Slider bgmSlider;
    [Tooltip("請確保 Slider 的 Min Value = 0.0001, Max Value = 1")]
    public Slider sfxSlider;

    private void Start()
    {
        // 1. 初始化主音量 Slider
        if (masterSlider != null)
        {
            masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        // 2. 初始化 BGM Slider
        if (bgmSlider != null)
        {
            // BGM 由 AudioManager 鎖定為 0.5（固定值），UI 只顯示不允許調整
            bgmSlider.value = 0.5f;
            bgmSlider.interactable = false;
        }

        // 3. 初始化 SFX Slider
        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }

    private void OnDestroy()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }

    public void SetMasterVolume(float value)
    {
        if (AudioManager.GetInstance() != null)
        {
            AudioManager.GetInstance().SetMasterVolume(value);
        }
    }

    public void SetBGMVolume(float value)
    {
        if (AudioManager.GetInstance() != null)
        {
            AudioManager.GetInstance().SetBGMVolume(value);
        }
    }

    public void SetSFXVolume(float value)
    {
        if (AudioManager.GetInstance() != null)
        {
            AudioManager.GetInstance().SetSFXVolume(value);
        }
    }
}
