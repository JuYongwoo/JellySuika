using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

[DisallowMultipleComponent]
public class WaterBalloon : MonoBehaviour
{
    [Header("Nodes")]
    public GameObject nodePrefab;
    [Range(8, 128)] public int nodeCount = 32;
    public bool usePrefabRadius = true;
    public float radius = 0.5f;
    public bool ignoreSelfCollision = true;

    [Header("Membrane (edge springs)")]
    public float edgeFrequency = 12f;
    [Range(0f, 1f)] public float edgeDamping = 0.6f;

    [Header("Internal Pressure / Area Preserve (radial)")]
    public float pressureStiffness = 40f;
    [Range(0f, 2f)] public float pressureDamping = 0.2f;

    [Header("Clamp (min/max spread)")]
    [Range(0.1f, 0.95f)] public float minRadiusFactor = 0.6f;
    [Range(1.0f, 5f)] public float maxRadiusFactor = 1.25f;
    public bool softMaxClamp = true;
    public float maxClampStiffness = 60f;
    [Range(0f, 1f)] public float maxClampDamping = 0.3f;

    [Header("Drift Kill (idle only)")]
    public bool cancelHorizontalDriftWhenIdle = true;
    [Range(0f, 1f)] public float driftCancelStrength = 0.35f;
    public float idleAreaErrorEpsilon = 0.02f;

    [Header("Node Rigidbodies")]
    public float nodeMass = 0.05f;
    public float gravityScale = 1f;
    public float linearDrag = 0.1f;
    public float angularDrag = 0.05f;

    // ---- Internals ----
    private readonly List<Rigidbody2D> rbs = new();
    private readonly List<NodeSensor> sensors = new();
    private float targetArea;
    private float minRadius;
    private float maxRadius;


    public Fruits fruitType;
    public bool isMerging = false;

    private class NodeSensor : MonoBehaviour //자식 노드들
    {
        public Transform balloonRoot;
        //public int externalContacts;

        void OnCollisionEnter2D(Collision2D c)
        {
            if (c.transform.root != balloonRoot)
            {
                WaterBalloon cwb = c.transform.root.GetComponent<WaterBalloon>();
                if (cwb)
                {
                    //externalContacts++;
                    WaterBalloon twb = this.transform.root.GetComponent<WaterBalloon>();
                    if (cwb.fruitType == twb.fruitType)
                    {
                        if (cwb.isMerging || twb.isMerging) return;
                        cwb.isMerging = true;
                        twb.isMerging = true;

                        ManagerObject.instance.actionManager.OnsetScoreText(++ManagerObject.instance.resourceManager.stageDataSO.Result.Score);

                        Vector3 midPoint = (c.transform.position + gameObject.transform.position) * 0.5f;

                        cwb.destroySelf();
                        twb.destroySelf();
                        //서로의 부모 없앤다

                        if ((int)twb.fruitType + 1 < Enum.GetValues(typeof(Fruits)).Length)
                        {
                            ManagerObject.instance.resourceManager.fruitsInfoMap.TryGetValue(twb.fruitType + 1, out var fr);
                            if (fr.Result != null)
                            {
                                ManagerObject.instance.soundManager.PlayAudioClip(ManagerObject.instance.resourceManager.sfxMap[SFX.FruitFusion].Result, 0.2f, false);
                                Instantiate(fr.Result.parentPrefab, midPoint, new Quaternion());
                            }
                        }
                        else
                        {
                            ManagerObject.instance.soundManager.PlayAudioClip(ManagerObject.instance.resourceManager.sfxMap[SFX.ScoreGet].Result, 0.2f, false);
                        }
                        //중간 지점에 type+1의 프리팹(리소스매니저에서) 소환
                        //만약 suika면 소환하지않음
                    }
                }
            }
        }

        //void OnCollisionExit2D(Collision2D c)
        //{ if (c.transform.root != balloonRoot) externalContacts = Mathf.Max(0, externalContacts - 1); }

        //void OnCollisionStay2D(Collision2D c)
        //{ if (c.transform.root != balloonRoot && externalContacts <= 0) externalContacts = 1; }

    }

    void Awake()
    {
        Build();
    }

    private void Start()
    {

        StartCoroutine(spread()); //소환할때 원점으로부터 0.1f 거리에 모아놓고 넓힌다(다른 과일들을 밀어내기 위해)
    }

    private IEnumerator spread()
    {
        setNodeDistance(false);
        yield return new WaitForSeconds(0.1f);
        setNodeDistance(true);
    }



    public void destroySelf()
    {
        Destroy(gameObject);
    }

    public void setGravity(bool isOn)
    {
        if (isOn)
        {
            for (int i = 0; i < rbs.Count; i++)
            {
                rbs[i].gravityScale = 1f;
            }
        }
        else
        {
            for (int i = 0; i < rbs.Count; i++)
            {
                rbs[i].gravityScale = 0;
            }
        }
    }



    private void setNodeDistance(bool isOn) //내부 스프링조인트들이 중앙과의 최대거리 (과일이 소환되면 처음엔 0.1f, 이후에 원래 반지름으로 하여 다른 과일들을 밀어낼 수 있도록 조정해야)
    {
        if (isOn)
        {
            for (int i = 0; i < nodeCount; i++)
            {
                rbs[i].GetComponent<SpringJoint2D>().distance = Vector3.Distance(rbs[i].position, rbs[(i + 1) % nodeCount].position);
            }

        }
        else
        {
            for (int i = 0; i < nodeCount; i++)
            {
                rbs[i].GetComponent<SpringJoint2D>().distance = 0.1f;
            }
        }
    }





    /// <summary>
    /// 아래부터 스프링조인트 컨트롤 영역
    /// </summary>

    public void Build()
    {
        // cleanup
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        rbs.Clear(); sensors.Clear();

        if (nodePrefab == null)
        {
            Debug.LogError("[WaterBalloon2D] nodePrefab missing");
            return;
        }

        float R = usePrefabRadius ? Mathf.Max(0.001f, GetPrefabRadius(nodePrefab))
                                  : Mathf.Max(0.001f, radius);

        minRadius = R * minRadiusFactor;
        maxRadius = R * Mathf.Max(1.0f, maxRadiusFactor);

        var cols = new List<Collider2D>(nodeCount);


        for (int i = 0; i < nodeCount; i++) //자식 오브젝트 생성
        {
            float ang = (i / (float)nodeCount) * Mathf.PI * 2f;
            Vector2 local = new(Mathf.Cos(ang) * R, Mathf.Sin(ang) * R);

            var go = Instantiate(nodePrefab, transform);
            go.name = $"node_{i}";
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.identity;

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = go.AddComponent<Rigidbody2D>();
            rb.mass = nodeMass;
            rb.gravityScale = gravityScale;
            rb.linearDamping = linearDrag;
            rb.angularDamping = angularDrag;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;

            var col = go.GetComponent<Collider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();

            var sensor = go.AddComponent<NodeSensor>();
            sensor.balloonRoot = transform.root;

            rbs.Add(rb);
            sensors.Add(sensor);
            cols.Add(col);
        }

        // 서로의 콜리전은 무시한다.
        if (ignoreSelfCollision)
        {
            for (int i = 0; i < cols.Count; i++)
                for (int j = i + 1; j < cols.Count; j++)
                    Physics2D.IgnoreCollision(cols[i], cols[j], true);
        }

        // SpringJoint2D 추가
        for (int i = 0; i < nodeCount; i++)
        {
            AddSpring(rbs[i], rbs[(i + 1) % nodeCount], edgeFrequency, edgeDamping);
        }

        // target area
        targetArea = Mathf.Abs(PolygonAreaSigned());
    }


    void AddSpring(Rigidbody2D a, Rigidbody2D b, float freq, float damp)
    {
        var sj = a.gameObject.AddComponent<SpringJoint2D>();
        sj.connectedBody = b;
        sj.autoConfigureDistance = false;
        sj.distance = Vector2.Distance(a.position, b.position); //시작할때는 0.1로 해야할 수도
        sj.frequency = Mathf.Max(0.01f, freq);
        sj.dampingRatio = Mathf.Clamp01(damp);
        sj.enableCollision = false;
    }

    // signed area (CCW > 0)
    float PolygonAreaSigned()
    {
        float sum = 0f;
        for (int i = 0; i < rbs.Count; i++)
        {
            Vector2 p = rbs[i].position;
            Vector2 q = rbs[(i + 1) % rbs.Count].position;
            sum += (p.x * q.y - q.x * p.y);
        }
        return sum * 0.5f;
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

        float signedArea = PolygonAreaSigned();
        float area = Mathf.Abs(signedArea);
        float areaErrorRatio = (targetArea - area) / Mathf.Max(targetArea, 1e-5f);
        float kPressure = areaErrorRatio * pressureStiffness;
        float orient = Mathf.Sign(signedArea); // CCW:+1, CW:-1 (뒤집혀도 바깥 법선 유지)

        Vector2 c = GetCentroid();

        // --- Edge-normal pressure (끊김 방지 핵심) ---
        // 각 변 길이에 비례해 바깥 법선으로 압력 분배(두 끝점에 반씩)
        for (int i = 0; i < rbs.Count; i++)
        {
            int j = (i + 1) % rbs.Count;

            Vector2 p = rbs[i].position;
            Vector2 q = rbs[j].position;

            Vector2 e = q - p;
            float len = e.magnitude;
            if (len < 1e-6f) continue;

            // outward normal (orientation 보정)
            Vector2 n = orient * new Vector2(e.y, -e.x) / len;

            // pressure * edge length -> force; 양 끝점에 1/2씩
            Vector2 F = n * (kPressure * len);
            rbs[i].AddForce(F * 0.5f, ForceMode2D.Force);
            rbs[j].AddForce(F * 0.5f, ForceMode2D.Force);

            // normal-direction damping (출렁임 억제)
            if (pressureDamping > 0f)
            {
                float vni = Vector2.Dot(rbs[i].linearVelocity, n);
                float vnj = Vector2.Dot(rbs[j].linearVelocity, n);
                rbs[i].AddForce(-n * vni * pressureDamping, ForceMode2D.Force);
                rbs[j].AddForce(-n * vnj * pressureDamping, ForceMode2D.Force);
            }
        }

        // --- Min / Max radius clamps (원 코드 유지, 단일 루프) ---
        for (int i = 0; i < rbs.Count; i++)
        {
            Rigidbody2D rb = rbs[i];
            Vector2 to = rb.position - c;
            float d = to.magnitude;
            if (d < 1e-6f) continue;
            Vector2 dir = to / d;

            // min clamp
            if (d < minRadius)
            {
                rb.position = c + dir * minRadius;
                float inward = Vector2.Dot(rb.linearVelocity, -dir);
                if (inward > 0f) rb.linearVelocity += dir * inward;
                continue;
            }

            // max clamp
            if (d > maxRadius)
            {
                if (softMaxClamp)
                {
                    float excess = d - maxRadius;
                    rb.AddForce(-dir * (excess * Mathf.Max(0f, maxClampStiffness)), ForceMode2D.Force);
                    float outward = Vector2.Dot(rb.linearVelocity, dir);
                    if (outward > 0f) rb.linearVelocity -= dir * (outward * Mathf.Clamp01(maxClampDamping));
                }
                else
                {
                    rb.position = c + dir * maxRadius;
                    float outward = Vector2.Dot(rb.linearVelocity, dir);
                    if (outward > 0f) rb.linearVelocity -= dir * outward;
                }
            }
        }

        //// --- idle일 때 수평 COM 드리프트 제거(원 코드 유지) ---
        //if (cancelHorizontalDriftWhenIdle && Mathf.Abs(areaErrorRatio) < idleAreaErrorEpsilon)
        //{
        //    int external = 0;
        //    for (int i = 0; i < sensors.Count; i++) external += sensors[i].externalContacts;

        //    if (external == 0)
        //    {
        //        float vxAvg = 0f;
        //        for (int i = 0; i < rbs.Count; i++) vxAvg += rbs[i].linearVelocity.x;
        //        vxAvg /= rbs.Count;

        //        if (Mathf.Abs(vxAvg) > 0.0001f)
        //        {
        //            float corr = Mathf.Clamp01(driftCancelStrength) * vxAvg;
        //            for (int i = 0; i < rbs.Count; i++)
        //            {
        //                var v = rbs[i].linearVelocity;
        //                v.x -= corr;
        //                rbs[i].linearVelocity = v;
        //            }
        //        }
        //    }
        //}
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
