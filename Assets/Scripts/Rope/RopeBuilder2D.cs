using UnityEngine;

[DisallowMultipleComponent]
public class RopeBuilder2D : MonoBehaviour
{
    [Header("Build Mode")]
    public bool buildFromChildren = false; // 若為 true：改用現有子物件骨架，不自動生成
    public bool useAutoConfigureAnchors = true; // 建骨架模式時，是否交由 Unity 自動配置 Anchor

    [Header("Prefab & Count")]
    public GameObject segmentPrefab;
    [Range(3, 120)] public int segmentCount = 20;
    [Min(0.01f)] public float segmentSpacing = 0.2f;

    [Header("Anchor")]
    public Rigidbody2D topAnchor; // 若未指定，會在 Start 時自動建立一個 Static Anchor

    [Header("Layer & Tags")]
    public string ropeLayerName = "Rope";
    public string ropeTailTag = "RopeTail";
    public string ropeSegmentTag = "RopeSegment";

    [Header("Joint Limits (per segment)")]
    public float segmentLimitMin = -25f;
    public float segmentLimitMax = 25f;

    private Rigidbody2D[] _bodies;

    public Rigidbody2D TailBody => (_bodies != null && _bodies.Length > 0) ? _bodies[_bodies.Length - 1] : null;

    void Start()
    {
        if (buildFromChildren)
        {
            // 使用現有子物件作為繩段，不自動生成也不自動建立錨點
            if (topAnchor == null)
            {
                Debug.LogWarning($"[RopeBuilder2D] buildFromChildren 啟用，但未指定 topAnchor。請在檢視器指定頂端錨點 Rigidbody2D。對應物件：{name}");
            }

            // 蒐集第一層子物件的 Rigidbody2D，依 Hierarchy 順序串接
            var childBodiesList = new System.Collections.Generic.List<Rigidbody2D>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var rbChild = child.GetComponent<Rigidbody2D>();
                if (rbChild != null)
                {
                    childBodiesList.Add(rbChild);
                }
                else
                {
                    Debug.LogWarning($"[RopeBuilder2D] 子物件缺少 Rigidbody2D，已略過：{child.name}");
                }
            }

            if (childBodiesList.Count == 0)
            {
                Debug.LogWarning($"[RopeBuilder2D] 未在子物件找到任何 Rigidbody2D，無法建立繩索：{name}");
                _bodies = new Rigidbody2D[0];
                return;
            }

            _bodies = childBodiesList.ToArray();

            // 逐段掛上/配置 HingeJoint2D，並接到上一個剛體；第一段接到 topAnchor（若有）
            Rigidbody2D previousBody = topAnchor;
            for (int i = 0; i < _bodies.Length; i++)
            {
                var segmentRB = _bodies[i];
                // [Refactor] 強制設定阻力以符合水下物理
                segmentRB.drag = 3f; 
                segmentRB.angularDrag = 1f;

                var segmentGO = segmentRB.gameObject;

                var hj = segmentGO.GetComponent<HingeJoint2D>();
                if (hj == null) hj = segmentGO.AddComponent<HingeJoint2D>();

                hj.connectedBody = previousBody;
                hj.autoConfigureConnectedAnchor = useAutoConfigureAnchors;
                if (!useAutoConfigureAnchors)
                {
                    hj.connectedAnchor = Vector2.zero;
                    hj.anchor = new Vector2(0f, +segmentSpacing * 0.5f);
                }

                hj.useLimits = true;
                var limits = hj.limits;
                limits.min = segmentLimitMin;
                limits.max = segmentLimitMax;
                hj.limits = limits;

                // 設定尾段/中段的 Tag（若未在專案中註冊則忽略）
                try
                {
                    segmentGO.tag = (i == _bodies.Length - 1) ? ropeTailTag : ropeSegmentTag;
                }
                catch { }

                previousBody = segmentRB;
            }
        }

        
        // [Refactor] 移除自動生成代碼，僅支援 buildFromChildren
    }
}


