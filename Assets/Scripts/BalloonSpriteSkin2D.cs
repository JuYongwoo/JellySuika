using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[DefaultExecutionOrder(100)]
public class BalloonSpriteSkin2D : MonoBehaviour
{
    [Header("Sprite & Material")]
    public Sprite sprite;
    public bool autoCreateMaterial = true;
    public string shaderName = "Sprites/Default";
    public string sortingLayer = "";
    public int sortingOrder = 0;

    [Header("UV")]
    [Range(0f, 0.2f)] public float uvPadding = 0.02f;
    public bool useSpriteRect = true;

    [Header("Nodes & Build")]
    public bool autoFindNodes = true;
    public bool rebuildEveryFrame = true;
    public int findNodesMaxWaitFrames = 60;

    [Header("Triangulation")]
    [Tooltip("외곽을 Convex Hull로 만든 뒤 삼각 팬으로 채웁니다.")]
    public bool forceConvex = true;

    [Header("Edge Inflate")]
    [Tooltip("노드별 Collider/Sprite 크기(월드)를 자동 감지하여 그 반지름만큼 외곽을 확장합니다.")]
    public bool inflateFromChildrenSize = true;
    [Tooltip("자동 감지 실패 시 사용할 지름(월드 단위). 0이면 미사용.")]
    public float fallbackChildSize = 0f;
    [Tooltip("추가로 더/덜 확장하고 싶을 때 더하는 값(월드 단위, 반경 기준에 더해짐).")]
    public float extraInflate = 0f;

    MeshFilter mf;
    MeshRenderer mr;
    Mesh mesh;

    readonly List<Transform> nodeTs = new();
    readonly List<Vector3> vWorld = new();
    readonly List<Vector3> vWorldInflated = new();
    readonly List<Vector3> vLocal = new();
    readonly List<Vector2> uvs = new();
    readonly List<int> tris = new();

    Sprite _lastSprite;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh { name = "BalloonSkinMesh" };
            mesh.MarkDynamic();
            mf.sharedMesh = mesh; // 에디터/프리팹에서도 안전
        }
        EnsureMaterial();
        ApplySpriteToMaterial();
        ApplySorting();
    }

    void Start()
    {
        if (autoFindNodes) StartCoroutine(WaitNodesThenBuild());
        else { RefreshNodes(silent: true); if (nodeTs.Count >= 3) BuildMesh(); }
    }

    void LateUpdate()
    {
        if (_lastSprite != sprite) { ApplySpriteToMaterial(); ApplySorting(); if (nodeTs.Count >= 3) BuildMesh(); }
        if (rebuildEveryFrame && nodeTs.Count >= 3) BuildMesh();
    }

    void OnValidate()
    {
        fallbackChildSize = Mathf.Max(0f, fallbackChildSize);

        if (!mf) mf = GetComponent<MeshFilter>();
        if (!mr) mr = GetComponent<MeshRenderer>();
        EnsureMaterial();
        ApplySpriteToMaterial();
        ApplySorting();
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                if (autoFindNodes) RefreshNodes(silent: true);
                if (nodeTs.Count >= 3 && mf && mf.sharedMesh) BuildMesh();
            };
#endif
    }

    System.Collections.IEnumerator WaitNodesThenBuild()
    {
        for (int i = 0; i < findNodesMaxWaitFrames; i++)
        {
            RefreshNodes(silent: true);
            if (nodeTs.Count >= 3) { BuildMesh(); yield break; }
            yield return null;
        }
        Debug.LogWarning("[BalloonSpriteSkin2D] 노드를 찾지 못했습니다.");
    }

    public void RefreshNodes(bool silent = false)
    {
        nodeTs.Clear();
        var rbs = GetComponentsInChildren<Rigidbody2D>();
        foreach (var rb in rbs)
            if (rb && rb.transform != transform)
                nodeTs.Add(rb.transform);

        if (!silent && nodeTs.Count < 3)
            Debug.LogWarning("[BalloonSpriteSkin2D] 노드가 3개 미만입니다.");
    }

#if UNITY_EDITOR
    static bool IsPrefabAsset(GameObject go)
    {
        if (PrefabUtility.IsPartOfPrefabAsset(go)) return true;
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && go.scene == stage.scene) return true;
        return false;
    }
#endif

    void EnsureMaterial()
    {
        if (!mr) return;
        var shader = Shader.Find(shaderName) ?? Shader.Find("Sprites/Default");

        bool useShared =
#if UNITY_EDITOR
            !Application.isPlaying || IsPrefabAsset(gameObject);
#else
            !Application.isPlaying;
#endif
        if (useShared)
        {
            if (mr.sharedMaterial == null || mr.sharedMaterial.shader == null)
            {
                var m = new Material(shader);
#if UNITY_EDITOR
                m.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
#endif
                mr.sharedMaterial = m;
            }
        }
        else
        {
            if (mr.material == null || mr.material.shader == null)
                mr.material = new Material(shader);
        }
    }

    void ApplySpriteToMaterial()
    {
        _lastSprite = sprite;
        if (!mr) return;

        bool useShared =
#if UNITY_EDITOR
            !Application.isPlaying || IsPrefabAsset(gameObject);
#else
            !Application.isPlaying;
#endif
        var mat = useShared ? mr.sharedMaterial : mr.material;
        if (mat != null) mat.mainTexture = sprite ? sprite.texture : null;
    }

    void ApplySorting()
    {
        if (!mr) return;
        mr.sortingOrder = sortingOrder;
        if (!string.IsNullOrEmpty(sortingLayer))
            mr.sortingLayerName = sortingLayer;
    }

    // ==================== Mesh Build ====================
    void BuildMesh()
    {
        if (nodeTs.Count < 3 || mf == null) return;

        // 1) 월드 좌표 + centroid
        vWorld.Clear();
        Vector2 c = Vector2.zero;
        for (int i = 0; i < nodeTs.Count; i++)
        {
            Vector3 p = nodeTs[i].position;
            vWorld.Add(p);
            c += (Vector2)p;
        }
        c /= Mathf.Max(1, nodeTs.Count);

        // 2) 각도 정렬 (centroid 기준)
        vWorld.Sort((a, b) =>
        {
            Vector2 da = (Vector2)a - c;
            Vector2 db = (Vector2)b - c;
            return Mathf.Atan2(da.y, da.x).CompareTo(Mathf.Atan2(db.y, db.x));
        });

        // 3) 노드별 크기로 외곽 확장
        vWorldInflated.Clear();
        for (int i = 0; i < vWorld.Count; i++)
        {
            Transform t = nodeTs[i]; // nodeTs와 vWorld는 같은 순서가 아닐 수 있으므로 안전하게 다시 찾기
            // 각도 정렬 후 index가 뒤섞였을 수 있으니, 가장 가까운 노드를 맵핑
            t = FindClosestNodeTransform(vWorld[i]);

            float inflateR = 0f;
            if (inflateFromChildrenSize)
                inflateR = GetNodeRadiusWorld(t);

            if (inflateR <= 0f && fallbackChildSize > 0f)
                inflateR = fallbackChildSize * 0.5f;

            inflateR += extraInflate;

            Vector2 to = (Vector2)vWorld[i] - c;
            float d = to.magnitude;
            Vector2 dir = (d > 1e-8f) ? (to / d) : Vector2.up;

            vWorldInflated.Add((Vector2)vWorld[i] + dir * Mathf.Max(0f, inflateR));
        }

        // 4) 외곽 안정화
        List<Vector3> poly = forceConvex ? ComputeConvexHull(vWorldInflated)
                                         : CleanPolygon(vWorldInflated, 1e-5f, 1e-6f);
        if (poly.Count < 3) return;

        // 5) 로컬/UV
        vLocal.Clear(); uvs.Clear();
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 p = poly[i];
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
            vLocal.Add(transform.InverseTransformPoint(p));
        }

        float pad = Mathf.Clamp01(uvPadding);
        if (sprite && useSpriteRect)
        {
            Rect tr = sprite.textureRect;
            Vector2 texSize = new(sprite.texture.width, sprite.texture.height);
            Vector2 uvMin = new(tr.xMin / texSize.x, tr.yMin / texSize.y);
            Vector2 uvMax = new(tr.xMax / texSize.x, tr.yMax / texSize.y);

            Vector2 size = max - min;
            size.x = Mathf.Max(size.x, 1e-6f);
            size.y = Mathf.Max(size.y, 1e-6f);

            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 pw = poly[i];
                float u01 = (pw.x - min.x) / size.x;
                float v01 = (pw.y - min.y) / size.y;
                float u = Mathf.Lerp(uvMin.x, uvMax.x, Mathf.Lerp(pad, 1f - pad, Mathf.Clamp01(u01)));
                float v = Mathf.Lerp(uvMin.y, uvMax.y, Mathf.Lerp(pad, 1f - pad, Mathf.Clamp01(v01)));
                uvs.Add(new Vector2(u, v));
            }
        }
        else
        {
            Vector2 size = max - min;
            size.x = Mathf.Max(size.x, 1e-6f);
            size.y = Mathf.Max(size.y, 1e-6f);
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 pw = poly[i];
                float u = Mathf.Lerp(pad, 1f - pad, Mathf.Clamp01((pw.x - min.x) / size.x));
                float v = Mathf.Lerp(pad, 1f - pad, Mathf.Clamp01((pw.y - min.y) / size.y));
                uvs.Add(new Vector2(u, v));
            }
        }

        // 6) 삼각형
        tris.Clear();
        if (forceConvex) TriangulateConvexFan(poly.Count, tris);
        else TriangulateEarClipRobust(poly, tris);

        // 7) 제출
        mesh.Clear();
        mesh.SetVertices(vLocal);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0, true);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    // ==================== Helpers ====================
    Transform FindClosestNodeTransform(Vector3 worldPos)
    {
        Transform best = nodeTs[0];
        float bestSqr = float.PositiveInfinity;
        for (int i = 0; i < nodeTs.Count; i++)
        {
            float d = ((Vector2)(nodeTs[i].position) - (Vector2)worldPos).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = nodeTs[i]; }
        }
        return best;
    }

    // 노드의 "월드 반지름" 추정
    float GetNodeRadiusWorld(Transform t)
    {
        if (t == null) return 0f;

        // 1) 원형 콜라이더
        var cc = t.GetComponent<CircleCollider2D>();
        if (cc)
        {
            float s = MaxAbs2D(t.lossyScale);
            return Mathf.Abs(cc.radius * s);
        }

        // 2) 박스 콜라이더
        var bc = t.GetComponent<BoxCollider2D>();
        if (bc)
        {
            Vector3 s = Abs2D(t.lossyScale);
            float w = Mathf.Abs(bc.size.x * s.x);
            float h = Mathf.Abs(bc.size.y * s.y);
            return 0.5f * Mathf.Max(w, h);
        }

        // 3) 캡슐 콜라이더
        var cap = t.GetComponent<CapsuleCollider2D>();
        if (cap)
        {
            Vector3 s = Abs2D(t.lossyScale);
            float w = Mathf.Abs(cap.size.x * s.x);
            float h = Mathf.Abs(cap.size.y * s.y);
            return 0.5f * Mathf.Max(w, h);
        }

        // 4) 폴리곤/엣지 콜라이더: Bounds 사용
        var pc = t.GetComponent<Collider2D>();
        if (pc)
        {
            var e = pc.bounds.extents; // 이미 월드
            return Mathf.Max(e.x, e.y);
        }

        // 5) 스프라이트 렌더러
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr && sr.sprite != null)
        {
            var e = sr.bounds.extents; // 월드
            return Mathf.Max(e.x, e.y);
        }

        // 6) 실패 시 0
        return 0f;
    }

    static float MaxAbs2D(Vector3 v) => Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y));
    static Vector3 Abs2D(Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), 1f);

    static List<Vector3> ComputeConvexHull(List<Vector3> pts)
    {
        var p = new List<Vector2>(pts.Count);
        for (int i = 0; i < pts.Count; i++) p.Add(pts[i]);

        p.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
        var lower = new List<Vector2>();
        foreach (var v in p)
        {
            while (lower.Count >= 2 && Cross(lower[^2], lower[^1], v) <= 0f) lower.RemoveAt(lower.Count - 1);
            lower.Add(v);
        }
        var upper = new List<Vector2>();
        for (int i = p.Count - 1; i >= 0; i--)
        {
            var v = p[i];
            while (upper.Count >= 2 && Cross(upper[^2], upper[^1], v) <= 0f) upper.RemoveAt(upper.Count - 1);
            upper.Add(v);
        }
        if (lower.Count > 0) lower.RemoveAt(lower.Count - 1);
        if (upper.Count > 0) upper.RemoveAt(upper.Count - 1);

        var hull = new List<Vector3>(lower.Count + upper.Count);
        foreach (var v in lower) hull.Add(v);
        foreach (var v in upper) hull.Add(v);
        return hull;
    }

    static List<Vector3> CleanPolygon(List<Vector3> src, float epsDist, float epsColinear)
    {
        var tmp = new List<Vector3>(src.Count);
        for (int i = 0; i < src.Count; i++)
        {
            Vector3 p = src[i];
            Vector3 q = src[(i + 1) % src.Count];
            if (((Vector2)(p - q)).sqrMagnitude > epsDist * epsDist)
                tmp.Add(p);
        }
        if (tmp.Count < 3) return tmp;

        var outV = new List<Vector3>(tmp.Count);
        for (int i = 0; i < tmp.Count; i++)
        {
            Vector2 a = tmp[(i + tmp.Count - 1) % tmp.Count];
            Vector2 b = tmp[i];
            Vector2 c = tmp[(i + 1) % tmp.Count];
            float area2 = Mathf.Abs(Cross(a, b, c));
            if (area2 > epsColinear) outV.Add(tmp[i]);
        }
        return outV.Count >= 3 ? outV : tmp;
    }

    static float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 ab = b - a;
        Vector2 ac = c - a;
        return ab.x * ac.y - ab.y * ac.x;
    }

    static float SignedArea(List<Vector3> v)
    {
        float s = 0f;
        for (int i = 0; i < v.Count; i++)
        {
            Vector2 p = v[i];
            Vector2 q = v[(i + 1) % v.Count];
            s += p.x * q.y - q.x * p.y;
        }
        return 0.5f * s;
    }

    static bool PointInTriangleInclusive(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float c1 = Cross(a, b, p);
        float c2 = Cross(b, c, p);
        float c3 = Cross(c, a, p);
        bool hasNeg = (c1 < -1e-8f) || (c2 < -1e-8f) || (c3 < -1e-8f);
        bool hasPos = (c1 > 1e-8f) || (c2 > 1e-8f) || (c3 > 1e-8f);
        return !(hasNeg && hasPos);
    }

    static void TriangulateConvexFan(int n, List<int> outTris)
    {
        outTris.Clear();
        if (n < 3) return;
        for (int i = 1; i < n - 1; i++)
        {
            outTris.Add(0);
            outTris.Add(i);
            outTris.Add(i + 1);
        }
    }

    static void TriangulateEarClipRobust(List<Vector3> vWorld, List<int> outIndices)
    {
        outIndices.Clear();
        int n = vWorld.Count; if (n < 3) return;

        var V = new List<int>(n);
        for (int i = 0; i < n; i++) V.Add(i);

        if (SignedArea(vWorld) < 0f) V.Reverse();

        int guard = 0;
        while (V.Count > 2 && guard++ < 20000)
        {
            bool earFound = false;

            for (int i = 0; i < V.Count; i++)
            {
                int i0 = V[(i + V.Count - 1) % V.Count];
                int i1 = V[i];
                int i2 = V[(i + 1) % V.Count];

                Vector2 a = vWorld[i0];
                Vector2 b = vWorld[i1];
                Vector2 c = vWorld[i2];

                float area2 = Mathf.Abs(Cross(a, b, c));
                if (area2 < 1e-8f) continue;
                if (Cross(a, b, c) <= 0f) continue;

                bool hasInside = false;
                for (int j = 0; j < V.Count; j++)
                {
                    int k = V[j];
                    if (k == i0 || k == i1 || k == i2) continue;
                    if (PointInTriangleInclusive((Vector2)vWorld[k], a, b, c)) { hasInside = true; break; }
                }
                if (hasInside) continue;

                outIndices.Add(i0); outIndices.Add(i1); outIndices.Add(i2);
                V.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
            {
                TriangulateConvexFan(V.Count, outIndices);
                break;
            }
        }
    }
}
