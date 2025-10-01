using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D 물풍선: 다수의 노드(Rigidbody2D) + 막(스프링) + 내부 압력(면적 보존, 반지름 방향)
/// + 최소/최대 반경 클램프(퍼짐/찌그러짐 상한·하한)
/// - 중앙 조인트 없음, 노드끼리 충돌은 기본 무시(월드와는 충돌)
/// - 드리프트 방지: 압력을 반지름 방향으로 적용(합력 ≈ 0), 아이들에서 수평 COM 드리프트 소거
/// </summary>
[DisallowMultipleComponent]
public class WaterBalloon2D : MonoBehaviour
{
    [Header("Nodes")]
    public GameObject nodePrefab;
    [Range(8, 128)] public int nodeCount = 32;
    [Tooltip("프리팹 크기로 초기 반지름 추정(권장)")]
    public bool usePrefabRadius = true;
    [Tooltip("usePrefabRadius=false일 때만 사용")]
    public float radius = 0.5f;

    [Tooltip("노드끼리 충돌 무시 (월드와는 충돌)")]
    public bool ignoreSelfCollision = true;

    [Header("Membrane (edge springs)")]
    public bool addShear = true;
    [Tooltip("가장자리 스프링 주파수 (10~14 젤리 느낌)")]
    public float edgeFrequency = 12f;
    [Range(0f, 1f)] public float edgeDamping = 0.6f;

    [Tooltip("대각(이웃의 이웃) 스프링 주파수")]
    public float shearFrequency = 8f;
    [Range(0f, 1f)] public float shearDamping = 0.6f;

    [Header("Internal Pressure / Area Preserve (radial)")]
    [Tooltip("면적 오차 비율 → 압력 크기(30~60 권장)")]
    public float pressureStiffness = 40f;
    [Tooltip("반경 방향 속도 감쇠 (출렁임 제어)")]
    [Range(0f, 2f)] public float pressureDamping = 0.2f;

    [Header("Clamp (min/max spread)")]
    [Tooltip("중심에서 최소 허용 반경 비율 (0.55~0.65 권장)")]
    [Range(0.1f, 0.95f)] public float minRadiusFactor = 0.6f;

    [Tooltip("중심에서 최대 허용 반경 비율 (1.0=초기크기, 1.3=30% 팽창 허용)")]
    [Range(1.0f, 5f)] public float maxRadiusFactor = 1.25f;

    [Tooltip("최대 반경을 넘을 때 즉시 자르는 대신 부드럽게 되밀기")]
    public bool softMaxClamp = true;
    [Tooltip("소프트 최대 클램프 반발력 계수")]
    public float maxClampStiffness = 60f;
    [Range(0f, 1f)] public float maxClampDamping = 0.3f;

    [Header("Drift Kill (idle only)")]
    [Tooltip("외부 접촉 없고 압력 에러가 작을 때 수평 COM 드리프트 제거")]
    public bool cancelHorizontalDriftWhenIdle = true;
    [Tooltip("드리프트 제거 강도 (1=즉시 제거, 0.2~0.5 권장)")]
    [Range(0f, 1f)] public float driftCancelStrength = 0.35f;
    [Tooltip("아이들 판정: |areaErrorRatio| < 이 값")]
    public float idleAreaErrorEpsilon = 0.02f;

    [Header("Node Rigidbodies")]
    public float nodeMass = 0.05f;
    public float gravityScale = 1f;
    public float linearDrag = 0.1f;
    public float angularDrag = 0.05f;

    // Internals
    private readonly List<Rigidbody2D> rbs = new();
    private readonly List<Collider2D> cols = new();
    private readonly List<NodeSensor> sensors = new();
    private float targetArea;
    private float minRadius;
    private float maxRadius;   // ★ 추가: 최대 퍼짐 반경
    private float initialRadius;

    private class NodeSensor : MonoBehaviour
    {
        public Transform balloonRoot;
        public int externalContacts;

        void OnCollisionEnter2D(Collision2D c)
        { if (c.transform.root != balloonRoot) externalContacts++; }

        void OnCollisionExit2D(Collision2D c)
        { if (c.transform.root != balloonRoot) externalContacts = Mathf.Max(0, externalContacts - 1); }

        void OnCollisionStay2D(Collision2D c)
        { if (c.transform.root != balloonRoot && externalContacts <= 0) externalContacts = 1; }
    }

    void Awake()                 // ★여기로 이동
    {
        Build();
    }

    public void Build()
    {
        // cleanup
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        rbs.Clear(); cols.Clear(); sensors.Clear();

        if (nodePrefab == null)
        {
            Debug.LogError("[WaterBalloon2D] nodePrefab missing");
            return;
        }

        float R = usePrefabRadius ? Mathf.Max(0.001f, GetPrefabRadius(nodePrefab))
                                  : Mathf.Max(0.001f, radius);
        initialRadius = R;
        minRadius = R * minRadiusFactor;
        maxRadius = R * Mathf.Max(1.0f, maxRadiusFactor); // ★ 최대 반경 계산

        // place nodes on a perfect circle (CCW)
        for (int i = 0; i < nodeCount; i++)
        {
            float ang = (i / (float)nodeCount) * Mathf.PI * 2f; // 0..2π
            Vector2 local = new(Mathf.Cos(ang) * R, Mathf.Sin(ang) * R);

            var go = Instantiate(nodePrefab, transform);
            go.name = $"node_{i}";
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.identity;

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = go.AddComponent<Rigidbody2D>();
            rb.mass = nodeMass;
            rb.gravityScale = gravityScale;
            rb.linearDamping = linearDrag;   // 프로젝트 버전에 따라 drag 사용
            rb.angularDamping = angularDrag; // (기존 코드와 동일)
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;

            var col = go.GetComponent<Collider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();

            var sensor = go.AddComponent<NodeSensor>();
            sensor.balloonRoot = transform.root;

            rbs.Add(rb);
            cols.Add(col);
            sensors.Add(sensor);
        }

        // ignore self collisions (world with nodes still collides)
        if (ignoreSelfCollision)
        {
            for (int i = 0; i < cols.Count; i++)
                for (int j = i + 1; j < cols.Count; j++)
                    if (cols[i] && cols[j]) Physics2D.IgnoreCollision(cols[i], cols[j], true);
        }

        // membrane springs (edges + optional shear)
        for (int i = 0; i < nodeCount; i++)
        {
            int j = (i + 1) % nodeCount;
            AddSpring(rbs[i], rbs[j], edgeFrequency, edgeDamping);

            if (addShear)
            {
                int k = (i + 2) % nodeCount;
                AddSpring(rbs[i], rbs[k], shearFrequency, shearDamping);
            }
        }

        // target area = initial polygon area (abs)
        targetArea = Mathf.Abs(PolygonAreaWorldAbs());
    }

    void AddSpring(Rigidbody2D a, Rigidbody2D b, float freq, float damp)
    {
        var sj = a.gameObject.AddComponent<SpringJoint2D>();
        sj.connectedBody = b;
        sj.autoConfigureDistance = false;
        sj.distance = Vector2.Distance(a.position, b.position);
        sj.frequency = Mathf.Max(0.01f, freq);
        sj.dampingRatio = Mathf.Clamp01(damp);
        sj.enableCollision = false;
    }

    // Shoelace absolute area
    float PolygonAreaWorldAbs()
    {
        float sum = 0f;
        for (int i = 0; i < rbs.Count; i++)
        {
            Vector2 p = rbs[i].position;
            Vector2 q = rbs[(i + 1) % rbs.Count].position;
            sum += (p.x * q.y - q.x * p.y);
        }
        return Mathf.Abs(sum) * 0.5f;
    }

    Vector2 GetCentroid()
    {
        Vector2 s = Vector2.zero;
        for (int i = 0; i < rbs.Count; i++) s += rbs[i].position;
        return s / rbs.Count;
    }

    void FixedUpdate()
    {
        if (rbs.Count < 3) return;

        // ---- Internal pressure (radial, momentum-conserving) ----
        float area = PolygonAreaWorldAbs();
        float areaErrorRatio = (targetArea - area) / Mathf.Max(targetArea, 1e-5f); // -1..+1 근처
        float kPressure = areaErrorRatio * pressureStiffness;

        Vector2 c = GetCentroid();

        // apply radial pressure & damping
        for (int i = 0; i < rbs.Count; i++)
        {
            Rigidbody2D rb = rbs[i];
            Vector2 to = rb.position - c;
            float d = to.magnitude;
            Vector2 dir = (d > 1e-6f) ? (to / Mathf.Max(d, 1e-6f)) : Random.insideUnitCircle.normalized;

            // radial pressure (합력≈0)
            rb.AddForce(dir * kPressure, ForceMode2D.Force);

            // radial velocity damping
            if (pressureDamping > 0f)
            {
                float vr = Vector2.Dot(rb.linearVelocity, dir);
                rb.AddForce(-dir * vr * pressureDamping, ForceMode2D.Force);
            }
        }

        // ---- Minimum radius clamp (anti-collapse) ----
        for (int i = 0; i < rbs.Count; i++)
        {
            Rigidbody2D rb = rbs[i];
            Vector2 to = rb.position - c;
            float d = to.magnitude;
            if (d < minRadius)
            {
                Vector2 dir = (d > 1e-6f) ? (to / d) : Random.insideUnitCircle.normalized;
                rb.position = c + dir * minRadius;           // 위치 푸시-아웃
                float inward = Vector2.Dot(rb.linearVelocity, -dir); // 안쪽 성분 제거
                if (inward > 0f) rb.linearVelocity += dir * inward;
            }
        }

        // ---- Maximum radius clamp (limit spread) ★ 추가 ----
        for (int i = 0; i < rbs.Count; i++)
        {
            Rigidbody2D rb = rbs[i];
            Vector2 to = rb.position - c;
            float d = to.magnitude;

            if (d > maxRadius)
            {
                Vector2 dir = (d > 1e-6f) ? (to / d) : Random.insideUnitCircle.normalized;

                if (softMaxClamp)
                {
                    // 소프트: 넘친 양 만큼 안쪽으로 당기는 힘 + 바깥 성분 감쇠
                    float excess = d - maxRadius;
                    rb.AddForce(-dir * (excess * Mathf.Max(0f, maxClampStiffness)), ForceMode2D.Force);

                    float outward = Vector2.Dot(rb.linearVelocity, dir);
                    if (outward > 0f)
                        rb.linearVelocity -= dir * (outward * Mathf.Clamp01(maxClampDamping));
                }
                else
                {
                    // 하드: 바로 잘라내고 바깥 성분 제거
                    rb.position = c + dir * maxRadius;
                    float outward = Vector2.Dot(rb.linearVelocity, dir);
                    if (outward > 0f) rb.linearVelocity -= dir * outward;
                }
            }
        }

        // ---- Cancel horizontal COM

        // ---- Cancel horizontal COM drift when truly idle (no contact, small area error) ----
        if (cancelHorizontalDriftWhenIdle)
        {
            int external = 0;
            for (int i = 0; i < sensors.Count; i++) external += sensors[i].externalContacts;

            if (external == 0 && Mathf.Abs(areaErrorRatio) < idleAreaErrorEpsilon)
            {
                // 평균 수평 속도
                float vxAvg = 0f;
                for (int i = 0; i < rbs.Count; i++) vxAvg += rbs[i].linearVelocity.x;
                vxAvg /= rbs.Count;

                if (Mathf.Abs(vxAvg) > 0.0001f)
                {
                    float corr = Mathf.Clamp01(driftCancelStrength) * vxAvg;
                    for (int i = 0; i < rbs.Count; i++)
                    {
                        var v = rbs[i].linearVelocity;
                        v.x -= corr;            // 수직 낙하는 그대로 두고, 수평 드리프트만 제거
                        rbs[i].linearVelocity = v;
                    }
                }
            }
        }
    }

    float GetPrefabRadius(GameObject prefab)
    {
        var sr = prefab.GetComponent<SpriteRenderer>();
        if (sr) return Mathf.Max(sr.bounds.extents.x, sr.bounds.extents.y);

        var cc = prefab.GetComponent<CircleCollider2D>();
        if (cc) return cc.radius * Mathf.Max(prefab.transform.localScale.x, prefab.transform.localScale.y);

        var bc = prefab.GetComponent<BoxCollider2D>();
        if (bc) return Mathf.Max(bc.size.x * prefab.transform.localScale.x, bc.size.y * prefab.transform.localScale.y) * 0.5f;

        return 0.5f;
    }
}
