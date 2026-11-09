using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 放在 Project 視窗的「一鍵音效設定按鈕」。
/// 搭配 Editor 腳本可自動掃描資料夾，把 AudioManager 的 BGM/SFX 清單填好。
/// </summary>
[CreateAssetMenu(menuName = "DeepSea/Audio/Auto Setup Asset", fileName = "AudioAutoSetup")]
public class AudioAutoSetupAsset : ScriptableObject
{
    [Header("要掃描的資料夾（Unity 專案路徑）")]
    [Tooltip("例如：Assets/Resources/SFX")]
    public List<string> sfxFolders = new List<string> { "Assets/Resources/SFX" };

    [Tooltip("例如：Assets/Resources/BGM")]
    public List<string> bgmFolders = new List<string> { "Assets/Resources/BGM" };

    [Header("Mixer（可選）")]
    [Tooltip("若留空，Editor 會嘗試自動找名為 MainMixer 的 AudioMixer。")]
    public UnityEngine.Audio.AudioMixer audioMixer;

    [Header("填表預設值")]
    [Range(0f, 1f)] public float defaultSfxVolume = 1f;
    [Range(0f, 1f)] public float defaultBgmVolume = 1f;
    [Range(0.1f, 3f)] public float defaultPitch = 1f;

    [Header("目標 Prefab（可選）")]
    [Tooltip("若指定，會直接更新此 AudioManager prefab；未指定則 Editor 會自動尋找專案內的 AudioManager.prefab")]
    public GameObject audioManagerPrefab;
}

