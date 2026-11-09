using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Data/MaterialDatabase")]
public class MaterialDatabase : ScriptableObject
{
    public List<MaterialData> materials;

    private Dictionary<string, MaterialData> _lookup;

    public MaterialData GetMaterial(string id)
    {
        if (_lookup == null)
        {
            _lookup = new Dictionary<string, MaterialData>();
            foreach (var mat in materials)
                if (mat != null && !string.IsNullOrEmpty(mat.materialId))
                    _lookup[mat.materialId] = mat;
        }
        _lookup.TryGetValue(id, out var data);
        return data;
    }
} 