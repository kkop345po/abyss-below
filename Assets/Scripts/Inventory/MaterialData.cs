using UnityEngine;

[CreateAssetMenu(menuName = "Data/MaterialData")]
public class MaterialData : ScriptableObject
{
    public string materialId;      // 例如 "mat_1"
    public string materialName;    // 例如 "海膽冷盤"
    public Sprite icon;            // 對應圖示
    [TextArea]
    public string description;     // 說明文字
} 