#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public class TMPFontBatchReplacer : EditorWindow
{
    public TMP_FontAsset newFont;

    [MenuItem("Tools/批次更換TMP字體")]
    public static void ShowWindow()
    {
        GetWindow<TMPFontBatchReplacer>("批次更換TMP字體");
    }

    void OnGUI()
    {
        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("新字體", newFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("替換場景中所有TMP字體"))
        {
            if (newFont == null)
            {
                Debug.LogWarning("請先指定新字體！");
                return;
            }

            var tmps = FindObjectsOfType<TextMeshProUGUI>(true);
            int count = 0;
            foreach (var tmp in tmps)
            {
                Undo.RecordObject(tmp, "Change TMP Font");
                tmp.font = newFont;
                EditorUtility.SetDirty(tmp);
                count++;
            }
            Debug.Log($"場景中所有TMP字體已更換，共 {count} 個！");
        }
    }
}
#endif