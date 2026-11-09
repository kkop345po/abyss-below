using UnityEngine;

/// <summary>單次釣魚結果，供結算 UI 與結束面板顯示。</summary>
public struct FishingSessionResult
{
    public bool QteSuccess;
    public bool GotFish;
    public string Title;
    public string Detail;
    public string ItemDisplayName;
    public string ItemId;
    public Sprite ItemIcon;
    public int ItemAmount;
    public int CoinsGranted;
}
