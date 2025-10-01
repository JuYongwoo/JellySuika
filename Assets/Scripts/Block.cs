using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NodeRingStable : MonoBehaviour
{
    public enum AnchorMode { ParentDynamic, FixedWorldAnchor }
    public enum CompressionMode { SoftForce, HardSnap }

    [Header("Prefab & Count")]
    public GameObject nodePrefab;
    [Min(3)] public int nodeCount = 32;

    [Header("Anchor & Springs")]
    public AnchorMode anchorMode = AnchorMode.ParentDynamic;
    public bool attachSprings = true;

    [Header("Placement")]
    [Tooltip("center -> node center distance = prefab radius")]
    public bool usePrefabRadiusAsPlacement = true;

    [Header("Spring tuning (stable defaults)")]
    [Tooltip("너무 높으면 진동 발생. 안정성 우선값 권장: edge 30~80")]
    public float edgeFrequency = 40f;
    [Range(0f, 1f)] public float edgeDamping = 0.35f;
    [Tooltip("radial(중심) 스프링 주파수")]
    public float radialFrequency = 30f;
    [Range(0f, 1f)] public float radialDamping = 0.4f;

    [Header("Node physics")]
    public float nodeMass = 0.03f;
    public float nodeGravity = 1f;
    public bool preventSleeping = false;
    [Tooltip("노드끼리 충돌 허용 => 서로 밀리며 겹침 줄임 (비용 증가)")]
    public bool allowNodeToNodeCollision = false;

    [Header("Compression / Clamping")]
    [Range(0f, 0.9f)]
    public float maxCompression = 0.35f;
    public CompressionMode compressionMode = CompressionMode.SoftForce;
    public float compressionRestoreStrength = 0.06f;
    [Range(0f, 1f)]
    public float hardSnapDampenVelocity = 0.6f;

    [Header("Stabilization (new)")]
    [Tooltip("FixedUpdate에서 노드 속도를 곱해 감쇠시키는 계수 (0.9~0.999)")]
    [Range(0.8f, 0.999f)] public float velocityDampingFactor = 0.96f;
    [Tooltip("노드 속도 절대 최대값(클램프)")]
    public float maxNodeSpeed = 12f;
    [Tooltip("중심 안정화: 평균 노드 중심과 transform.position 차이를 보정하는 힘")]
    public bool stabilizeCenter = true;
    [Tooltip("클수록 중심이 빠르게 복원 (권장 1~30)")]
    public float centerStabilizeStrength = 6f;

    [Header("Auto hard-snap behavior")]
    public float autoHardSnapTriggerFactor = 0.8f; // meanRadius < minAllowed * factor => force hard snap
    public float hardSnapCooldown = 0.5f;

    [Header("Misc")]
    public bool reuseExistingChildren = true;
    public float initialImpulse = 0f;

    // internals
    private List<GameObject> nodes = new List<GameObject>();
    private List<Collider2D> nodeColliders = new List<Collider2D>();
    private float desiredRadius = 0.5f;
    private float minAllowedRadius = 0.2f;
    private bool hardSnapLocked = false;

    void Start()
    {
        BuildRing();
        if (initialImpulse != 0f) ApplyInitialImpulse(initialImpulse);
    }

    public void BuildRing()
    {
        if (nodePrefab == null)
        {
            Debug.LogError("[NodeRingStable] nodePrefab is null.");
            return;
        }
        if (nodeCount < 3) nodeCount = 3;

        // 부모 Rigidbody 설정
        Rigidbody2D parentRb = GetComponent<Rigidbody2D>();
        if (anchorMode == AnchorMode.ParentDynamic)
        {
            if (parentRb == null) parentRb = gameObject.AddComponent<Rigidbody2D>();
            parentRb.bodyType = RigidbodyType2D.Dynamic;
            parentRb.gravityScale = nodeGravity;
            parentRb.interpolation = RigidbodyInterpolation2D.Interpolate;
            parentRb.sleepMode = RigidbodySleepMode2D.StartAwake;
        }
        else
        {
            if (parentRb != null)
            {
                parentRb.bodyType = RigidbodyType2D.Kinematic;
                parentRb.gravityScale = 0f;
                parentRb.linearVelocity = Vector2.zero;
                parentRb.angularVelocity = 0f;
            }
        }

        // prefab radius
        float prefabRadius = GetPrefabRadius(nodePrefab);
        if (prefabRadius <= 0f) prefabRadius = 0.5f;
        desiredRadius = usePrefabRadiusAsPlacement ? prefabRadius : prefabRadius;

        minAllowedRadius = desiredRadius * (1f - Mathf.Clamp01(maxCompression));

        // reuse/create
        nodes.Clear();
        nodeColliders.Clear();
        List<GameObject> existingChildren = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++) existingChildren.Add(transform.GetChild(i).gameObject);

        int reuseCount = 0;
        if (reuseExistingChildren)
        {
            reuseCount = Mathf.Min(existingChildren.Count, nodeCount);
            for (int i = 0; i < reuseCount; i++)
            {
                GameObject child = existingChildren[i];
                child.SetActive(true);
                EnsureNodePhysics(child);
                Collider2D col = child.GetComponent<Collider2D>();
                if (col == null) col = child.AddComponent<CircleCollider2D>();
                nodes.Add(child);
                nodeColliders.Add(col);
            }
        }

        for (int i = reuseCount; i < nodeCount; i++)
        {
            GameObject inst = Instantiate(nodePrefab, transform);
            inst.name = nodePrefab.name + "_node_" + i;
            EnsureNodePhysics(inst);
            Collider2D col = inst.GetComponent<Collider2D>();
            if (col == null) col = inst.AddComponent<CircleCollider2D>();
            nodes.Add(inst);
            nodeColliders.Add(col);
        }

        // disable extras
        for (int i = nodeCount; i < existingChildren.Count; i++) existingChildren[i].SetActive(false);

        // position nodes
        double angleStep = 2.0 * Mathf.PI / nodeCount;
        for (int i = 0; i < nodeCount; i++)
        {
            double angle = i * angleStep;
            double x = System.Math.Cos(angle) * desiredRadius;
            double y = System.Math.Sin(angle) * desiredRadius;
            nodes[i].transform.localPosition = new Vector3((float)x, (float)y, 0f);
            nodes[i].transform.localRotation = Quaternion.identity;
        }

        // collisions among nodes
        if (!allowNodeToNodeCollision)
        {
            for (int i = 0; i < nodeColliders.Count; i++)
                for (int j = i + 1; j < nodeColliders.Count; j++)
                    if (nodeColliders[i] != null && nodeColliders[j] != null)
                        Physics2D.IgnoreCollision(nodeColliders[i], nodeColliders[j], true);
        }
        else
        {
            // ensure collisions are not ignored
            for (int i = 0; i < nodeColliders.Count; i++)
                for (int j = i + 1; j < nodeColliders.Count; j++)
                    if (nodeColliders[i] != null && nodeColliders[j] != null)
                        Physics2D.IgnoreCollision(nodeColliders[i], nodeColliders[j], false);
        }

        // remove old springs
        foreach (var n in nodes)
            foreach (var j in n.GetComponents<SpringJoint2D>()) DestroyImmediate(j);

        // attach springs
        if (attachSprings)
        {
            float chord = 2f * desiredRadius * Mathf.Sin(Mathf.PI / nodeCount);
            Vector2 parentWorldPos = transform.position;
            Rigidbody2D parentBody = GetComponent<Rigidbody2D>();

            for (int i = 0; i < nodeCount; i++)
            {
                GameObject a = nodes[i];
                GameObject b = nodes[(i + 1) % nodeCount];

                SpringJoint2D edge = a.AddComponent<SpringJoint2D>();
                edge.autoConfigureDistance = false;
                edge.connectedBody = b.GetComponent<Rigidbody2D>();
                edge.distance = chord;
                edge.frequency = Mathf.Max(0.01f, edgeFrequency);
                edge.dampingRatio = Mathf.Clamp01(edgeDamping);
                edge.enableCollision = false;

                SpringJoint2D radial = a.AddComponent<SpringJoint2D>();
                radial.autoConfigureDistance = false;
                if (anchorMode == AnchorMode.ParentDynamic && parentBody != null)
                {
                    radial.connectedBody = parentBody;
                    radial.distance = desiredRadius;
                }
                else
                {
                    radial.connectedBody = null;
                    radial.connectedAnchor = parentWorldPos;
                    radial.distance = desiredRadius;
                }
                radial.frequency = Mathf.Max(0.01f, radialFrequency);
                radial.dampingRatio = Mathf.Clamp01(radialDamping);
                radial.enableCollision = false;
            }
        }

        Debug.Log($"[NodeRingStable] Built ring. desiredRadius={desiredRadius:F3}, minAllowedRadius={minAllowedRadius:F3}, nodes={nodeCount}");
    }

    void EnsureNodePhysics(GameObject go)
    {
        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = Mathf.Max(0.0001f, nodeMass);
        rb.gravityScale = nodeGravity;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
        rb.angularDamping = 0.05f;
        if (preventSleeping) rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        else rb.sleepMode = RigidbodySleepMode2D.StartAwake;
    }

    void FixedUpdate()
    {
        if (nodes.Count == 0) return;

        Vector2 centerWorld = transform.position;

        // mean radius
        float meanRadius = 0f;
        for (int i = 0; i < nodes.Count; i++)
            meanRadius += Vector2.Distance(centerWorld, nodes[i].transform.position);
        meanRadius /= nodes.Count;

        // auto hard-snap trigger
        if (!hardSnapLocked && meanRadius < minAllowedRadius * autoHardSnapTriggerFactor)
        {
            StartCoroutine(TemporarilyHardSnapAndLock(hardSnapCooldown));
            return;
        }

        // per-node compression handling
        for (int i = 0; i < nodes.Count; i++)
        {
            var go = nodes[i];
            if (go == null) continue;
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) continue;

            Vector2 pos = rb.position;
            Vector2 toNode = pos - centerWorld;
            float dist = toNode.magnitude;
            if (dist <= 1e-6f) toNode = Random.insideUnitCircle.normalized;

            if (dist < minAllowedRadius)
            {
                Vector2 dir = toNode.normalized;
                float deficit = minAllowedRadius - dist;
                if (compressionMode == CompressionMode.SoftForce)
                {
                    float forceMag = compressionRestoreStrength * deficit * (1f + rb.mass);
                    rb.AddForce(dir * forceMag, ForceMode2D.Force);
                }
                else
                {
                    Vector2 target = centerWorld + dir * minAllowedRadius;
                    rb.position = target;
                    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Mathf.Clamp01(hardSnapDampenVelocity));
                }
            }

            // velocity damping & clamp for stability
            if (velocityDampingFactor < 0.999f)
            {
                rb.linearVelocity *= velocityDampingFactor;
            }
            if (rb.linearVelocity.sqrMagnitude > maxNodeSpeed * maxNodeSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxNodeSpeed;
            }
        }

        // center stabilization: gently pull centroid back to transform.position
        if (stabilizeCenter)
        {
            Vector2 centroid = Vector2.zero;
            for (int i = 0; i < nodes.Count; i++) centroid += (Vector2)nodes[i].transform.position;
            centroid /= nodes.Count;
            Vector2 offset = centroid - (Vector2)transform.position;
            // apply small corrective forces to nodes in opposite direction to recentre
            if (offset.sqrMagnitude > 1e-6f)
            {
                Vector2 correct = -offset * centerStabilizeStrength * Time.fixedDeltaTime;
                // distribute correction to nodes (small force)
                for (int i = 0; i < nodes.Count; i++)
                {
                    var rb = nodes[i].GetComponent<Rigidbody2D>();
                    if (rb == null) continue;
                    rb.AddForce(correct, ForceMode2D.Force);
                }
                // also gently nudge parent rigidbody if present
                Rigidbody2D parentRb = GetComponent<Rigidbody2D>();
                if (parentRb != null && anchorMode == AnchorMode.ParentDynamic)
                {
                    parentRb.AddForce(offset * centerStabilizeStrength * 0.25f * Time.fixedDeltaTime, ForceMode2D.Force);
                }
            }
        }
    }

    IEnumerator TemporarilyHardSnapAndLock(float cooldown)
    {
        bool wasHard = (compressionMode == CompressionMode.HardSnap);
        compressionMode = CompressionMode.HardSnap;
        hardSnapLocked = true;

        // immediate snap outward
        Vector2 centerWorld = transform.position;
        for (int i = 0; i < nodes.Count; i++)
        {
            var rb = nodes[i].GetComponent<Rigidbody2D>();
            if (rb == null) continue;
            Vector2 dir = ((Vector2)rb.position - centerWorld).normalized;
            if (dir.sqrMagnitude < 1e-6f) dir = Random.insideUnitCircle.normalized;
            rb.position = centerWorld + dir * minAllowedRadius;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Mathf.Clamp01(hardSnapDampenVelocity));
        }

        yield return new WaitForSecondsRealtime(cooldown);

        compressionMode = wasHard ? CompressionMode.HardSnap : CompressionMode.SoftForce;
        hardSnapLocked = false;
    }

    void ApplyInitialImpulse(float strength)
    {
        if (strength == 0f) return;
        Vector2 center = transform.position;
        foreach (var n in nodes)
        {
            Rigidbody2D rb = n.GetComponent<Rigidbody2D>();
            if (rb == null) continue;
            Vector2 dir = ((Vector2)rb.position - center).normalized;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector2.up;
            rb.AddForce(dir * strength, ForceMode2D.Impulse);
        }
    }

    float GetPrefabRadius(GameObject prefab)
    {
        if (prefab == null) return 0.5f;
        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        if (sr != null) return sr.bounds.extents.x;
        CircleCollider2D cc = prefab.GetComponent<CircleCollider2D>();
        if (cc != null) return cc.radius * prefab.transform.localScale.x;
        BoxCollider2D bc = prefab.GetComponent<BoxCollider2D>();
        if (bc != null) return (bc.size.x * prefab.transform.localScale.x) * 0.5f;
        return prefab.transform.localScale.x * 0.5f;
    }

    // optional cleanup
    public void ClearNodes()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }
        nodes.Clear();
        nodeColliders.Clear();
    }
}
