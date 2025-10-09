using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterBalloonMesh : MonoBehaviour
{
    // 최소 옵션만 남김
    public string shaderName = "Sprites/Default";
    public bool rebuildEveryFrame = true;

    MeshFilter mf;
    MeshRenderer mr;
    Mesh mesh;

    Sprite sprite;
    Sprite _lastSprite;

    readonly List<Transform> nodes = new();
    readonly List<Vector3> polyWorld = new();
    readonly List<Vector3> polyLocal = new();
    readonly List<Vector2> uvs = new();
    readonly List<int> tris = new();

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();

        mesh = new Mesh { name = "WaterBalloonMesh" };
        mesh.MarkDynamic();
        mf.sharedMesh = mesh; // 에디터/런타임 공통 안전

        // 머티리얼(런타임 인스턴스) 보장
        var sh = Shader.Find(shaderName) ?? Shader.Find("Sprites/Default");
        mr.material = new Material(sh);
    }

    void Start()
    {
        StartCoroutine(SetupSpriteAndBuild());
    }

    IEnumerator SetupSpriteAndBuild()
    {
        // 리소스 준비를 기다렸다가 스프라이트 가져오기
        var wb = GetComponent<WaterBalloon>();
        while (ManagerObject.instance == null ||
               ManagerObject.instance.resourceManager == null ||
               wb == null ||
               !ManagerObject.instance.resourceManager.fruitsInfoMap.ContainsKey(wb.fruitType) ||
               ManagerObject.instance.resourceManager.fruitsInfoMap[wb.fruitType].Result == null)
            yield return null;

        sprite = ManagerObject.instance.resourceManager
                    .fruitsInfoMap[wb.fruitType].Result.fruitSprite;

        ApplySpriteToMaterial();
        RefreshNodes();
        BuildMesh();
    }

    void LateUpdate()
    {
        if (_lastSprite != sprite)
        {
            ApplySpriteToMaterial();
            BuildMesh();
        }

        if (rebuildEveryFrame)
        {
            // 노드가 움직이거나 추가/삭제될 수 있으므로 간단히 매 프레임 빌드
            BuildMesh();
        }
    }

    // === 최소 기능만 구현 ===
    void RefreshNodes()
    {
        nodes.Clear();
        var rbs = GetComponentsInChildren<Rigidbody2D>();
        foreach (var rb in rbs)
            if (rb && rb.transform != transform)
                nodes.Add(rb.transform);
    }

    void ApplySpriteToMaterial()
    {
        _lastSprite = sprite;
        var mat = mr.material; // 반드시 런타임 인스턴스 사용
        mat.mainTexture = sprite ? sprite.texture : null;
    }

    void BuildMesh()
    {
        if (nodes.Count < 3 || mesh == null) return;

        // 1) 월드 폴리곤 구성 (자식들을 선으로 이어서 다각형)
        polyWorld.Clear();
        Vector2 c = Vector2.zero;
        for (int i = 0; i < nodes.Count; i++)
        {
            var p = nodes[i].position;
            polyWorld.Add(p);
            c += (Vector2)p;
        }
        c /= Mathf.Max(1, nodes.Count);

        // 각도 기준 정렬 (간단, 안정적)
        polyWorld.Sort((a, b) =>
        {
            Vector2 da = (Vector2)a - c;
            Vector2 db = (Vector2)b - c;
            return Mathf.Atan2(da.y, da.x).CompareTo(Mathf.Atan2(db.y, db.x));
        });

        // BuildMesh() 안, 중심 c 계산(c /= ...)과 각도 정렬(polyWorld.Sort(...)) 직후에 추가

    for (int i = 0; i < polyWorld.Count; i++)
    {
        Vector2 p = polyWorld[i];
        Vector2 dir = p - c;
        float len = dir.magnitude;
        dir = (len > 1e-6f) ? (dir / len) : Vector2.up;

        // 각 자식 오브젝트 위치에서 중심 방향으로 radius 만큼 더 외곽으로
        polyWorld[i] = p + dir * nodes[0].transform.localScale.x/2;
    }


        // 2) 로컬 변환 + AABB
        polyLocal.Clear();
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < polyWorld.Count; i++)
        {
            Vector3 lp = transform.InverseTransformPoint(polyWorld[i]);
            polyLocal.Add(lp);
            Vector2 w = polyWorld[i];
            if (w.x < min.x) min.x = w.x; if (w.y < min.y) min.y = w.y;
            if (w.x > max.x) max.x = w.x; if (w.y > max.y) max.y = w.y;
        }
        Vector2 size = max - min;
        if (size.x <= 1e-6f) size.x = 1e-6f;
        if (size.y <= 1e-6f) size.y = 1e-6f;

        // 3) UV (스프라이트의 textureRect를 폴리곤 AABB에 맞춰 매핑)
        uvs.Clear();
        if (sprite)
        {
            Rect tr = sprite.textureRect;
            Vector2 texSize = new(sprite.texture.width, sprite.texture.height);
            Vector2 uvMin = new(tr.xMin / texSize.x, tr.yMin / texSize.y);
            Vector2 uvMax = new(tr.xMax / texSize.x, tr.yMax / texSize.y);

            for (int i = 0; i < polyWorld.Count; i++)
            {
                Vector2 w = polyWorld[i];
                float u01 = (w.x - min.x) / size.x;
                float v01 = (w.y - min.y) / size.y;
                float u = Mathf.Lerp(uvMin.x, uvMax.x, Mathf.Clamp01(u01));
                float v = Mathf.Lerp(uvMin.y, uvMax.y, Mathf.Clamp01(v01));
                uvs.Add(new Vector2(u, v));
            }
        }
        else
        {
            for (int i = 0; i < polyWorld.Count; i++)
            {
                Vector2 w = polyWorld[i];
                float u = (w.x - min.x) / size.x;
                float v = (w.y - min.y) / size.y;
                uvs.Add(new Vector2(u, v));
            }
        }

        // 4) 삼각형 (단순 팬)
        tris.Clear();
        for (int i = 1; i < polyLocal.Count - 1; i++)
        {
            tris.Add(0);
            tris.Add(i);
            tris.Add(i + 1);
        }

        // 5) 제출
        mesh.Clear();
        mesh.SetVertices(polyLocal);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0, true);
        mesh.RecalculateBounds();
    }
}
