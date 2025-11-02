using JYW.JellySuika.Common;
using JYW.JellySuika.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace JYW.JellySuika.Fruit
{

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WaterBalloon : MonoBehaviour, PooledObejct
    {
        private GameObject nodePrefab;
        [Range(8, 128)] public int nodeCount = 32;
        public bool usePrefabRadius = true;

        public float edgeFrequency = 12f;
        [Range(0f, 1f)] public float edgeDamping = 0.6f;

        public float pressureStiffness = 40f;
        [Range(0f, 2f)] public float pressureDamping = 0.2f;

        [Range(0.1f, 0.95f)] public float minRadiusFactor = 0.6f;
        [Range(1.0f, 5f)] public float maxRadiusFactor = 1.25f;
        public bool softMaxClamp = true;
        public float maxClampStiffness = 60f;
        [Range(0f, 1f)] public float maxClampDamping = 0.3f;

        public bool cancelHorizontalDriftWhenIdle = true;
        [Range(0f, 1f)] public float driftCancelStrength = 0.35f;
        public float idleAreaErrorEpsilon = 0.02f;

        public float nodeMass = 0.05f;
        public float gravityScale = 1f;
        public float linearDrag = 0.1f;
        public float angularDrag = 0.05f;

        private readonly List<Rigidbody2D> rbs = new();
        private readonly List<NodeSensor> sensors = new();
        private float targetArea;
        private float minRadius;
        private float maxRadius;

        public Fruits fruitType;
        public bool isMerging = false;

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

        private class NodeSensor : MonoBehaviour
        {
            public Transform balloonRoot;

            void OnCollisionEnter2D(Collision2D c)
            {
                var p = c.transform.parent;
                if (p == null || p == balloonRoot) return;

                var cwb = p.GetComponent<WaterBalloon>(); if (!cwb) return;
                var twb = this.transform.parent.GetComponent<WaterBalloon>(); if (!twb) return;

                if (cwb.fruitType != twb.fruitType) return;
                if (cwb.isMerging || twb.isMerging) return;

                cwb.isMerging = true;
                twb.isMerging = true;

                ManagerObject.instance.eventManager.OnSetScoreText(++ManagerObject.instance.resourceManager.stageDataSO.Result.Score);

                Vector3 midPoint = (c.transform.position + gameObject.transform.position) * 0.5f;

                ManagerObject.instance.poolManager.DestroyPooled(cwb.gameObject);
                ManagerObject.instance.poolManager.DestroyPooled(twb.gameObject);

                if ((int)twb.fruitType + 1 < Enum.GetValues(typeof(Fruits)).Length)
                {
                    ManagerObject.instance.resourceManager.fruitsInfoMap.TryGetValue(twb.fruitType + 1, out var fr);
                    if (fr.Result != null)
                    {
                        ManagerObject.instance.eventManager.OnPlayAudioClip(ManagerObject.instance.resourceManager.sfxMap[SFX.FruitFusion].Result, 0.2f, false);
                        ManagerObject.instance.poolManager.Spawn(fr.Result.parentPrefab, midPoint, Quaternion.identity);
                    }
                }
                else
                {
                    ManagerObject.instance.eventManager.OnPlayAudioClip(ManagerObject.instance.resourceManager.sfxMap[SFX.ScoreGet].Result, 0.2f, false);
                }
            }
        }

        public void PoolStart()
        {
            isMerging = false;

            Build();
            StartCoroutine(spread());

            mf = GetComponent<MeshFilter>();
            mr = GetComponent<MeshRenderer>();

            mesh = new Mesh { name = "WaterBalloonMesh" };
            mesh.MarkDynamic();
            mf.sharedMesh = mesh;

            var sh = Shader.Find(shaderName) ?? Shader.Find("Sprites/Default");
            mr.material = new Material(sh);

            StartCoroutine(SetupSpriteAndBuild());
        }

        public void PoolDestroy()
        {

            StopAllCoroutines();

            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            rbs.Clear();
            sensors.Clear();
            nodes.Clear();
            polyWorld.Clear();
            polyLocal.Clear();
            uvs.Clear();
            tris.Clear();

            if (mesh != null) mesh.Clear();
            _lastSprite = null;
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
            if (isOn) { for (int i = 0; i < rbs.Count; i++) rbs[i].gravityScale = 1f; }
            else { for (int i = 0; i < rbs.Count; i++) rbs[i].gravityScale = 0; }
        }

        private void setNodeDistance(bool isOn)
        {
            if (isOn)
            {
                for (int i = 0; i < nodeCount; i++)
                {
                    rbs[i].GetComponent<SpringJoint2D>().distance =
                        Vector3.Distance(rbs[i].position, rbs[(i + 1) % nodeCount].position);
                }
            }
            else
            {
                for (int i = 0; i < nodeCount; i++)
                    rbs[i].GetComponent<SpringJoint2D>().distance = 0.1f;
            }
        }

        public void Build()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            rbs.Clear(); sensors.Clear();

            nodePrefab = ManagerObject.instance.resourceManager.fruitsInfoMap[fruitType].Result.childPrefab;

            float R = Mathf.Max(0.001f, nodePrefab.transform.localScale.x);

            minRadius = R * minRadiusFactor;
            maxRadius = R * Mathf.Max(1.0f, maxRadiusFactor);

            var cols = new List<Collider2D>(nodeCount);

            for (int i = 0; i < nodeCount; i++)
            {
                float ang = (i / (float)nodeCount) * Mathf.PI * 2f;
                Vector2 local = new(Mathf.Cos(ang) * R, Mathf.Sin(ang) * R);

                var go = Instantiate(nodePrefab, transform);
                go.name = $"node_{i}";
                go.transform.localPosition = local;
                go.transform.localRotation = Quaternion.identity;

                var rb = go.GetComponent<Rigidbody2D>(); if (rb == null) rb = go.AddComponent<Rigidbody2D>();
                rb.mass = nodeMass;
                rb.gravityScale = gravityScale;
                rb.linearDamping = linearDrag;
                rb.angularDamping = angularDrag;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.freezeRotation = true;

                var col = go.GetComponent<Collider2D>(); if (col == null) col = go.AddComponent<CircleCollider2D>();

                var sensor = go.AddComponent<NodeSensor>();
                sensor.balloonRoot = transform;

                rbs.Add(rb);
                sensors.Add(sensor);
                cols.Add(col);
            }

            for (int i = 0; i < cols.Count; i++)
                for (int j = i + 1; j < cols.Count; j++)
                    Physics2D.IgnoreCollision(cols[i], cols[j], true);

            for (int i = 0; i < nodeCount; i++)
                AddSpring(rbs[i], rbs[(i + 1) % nodeCount], edgeFrequency, edgeDamping);

            targetArea = Mathf.Abs(PolygonAreaSigned());
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
            float orient = Mathf.Sign(signedArea);

            Vector2 c = GetCentroid();

            for (int i = 0; i < rbs.Count; i++)
            {
                int j = (i + 1) % rbs.Count;

                Vector2 p = rbs[i].position;
                Vector2 q = rbs[j].position;

                Vector2 e = q - p;
                float len = e.magnitude;
                if (len < 1e-6f) continue;

                Vector2 n = orient * new Vector2(e.y, -e.x) / len;

                Vector2 F = n * (kPressure * len);
                rbs[i].AddForce(F * 0.5f, ForceMode2D.Force);
                rbs[j].AddForce(F * 0.5f, ForceMode2D.Force);

                if (pressureDamping > 0f)
                {
                    float vni = Vector2.Dot(rbs[i].linearVelocity, n);
                    float vnj = Vector2.Dot(rbs[j].linearVelocity, n);
                    rbs[i].AddForce(-n * vni * pressureDamping, ForceMode2D.Force);
                    rbs[j].AddForce(-n * vnj * pressureDamping, ForceMode2D.Force);
                }
            }

            for (int i = 0; i < rbs.Count; i++)
            {
                Rigidbody2D rb = rbs[i];
                Vector2 to = rb.position - c;
                float d = to.magnitude;
                if (d < 1e-6f) continue;
                Vector2 dir = to / d;

                if (d < minRadius)
                {
                    rb.position = c + dir * minRadius;
                    float inward = Vector2.Dot(rb.linearVelocity, -dir);
                    if (inward > 0f) rb.linearVelocity += dir * inward;
                    continue;
                }

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

        IEnumerator SetupSpriteAndBuild()
        {
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
                BuildMesh();
            }
        }

        void RefreshNodes()
        {
            nodes.Clear();
            var rbArr = GetComponentsInChildren<Rigidbody2D>();
            foreach (var rb in rbArr)
                if (rb && rb.transform != transform)
                    nodes.Add(rb.transform);
        }

        void ApplySpriteToMaterial()
        {
            _lastSprite = sprite;
            var mat = mr.material;
            mat.mainTexture = sprite ? sprite.texture : null;
        }

        void BuildMesh()
        {
            if (nodes.Count < 3 || mesh == null) return;

            polyWorld.Clear();
            Vector2 c = Vector2.zero;
            for (int i = 0; i < nodes.Count; i++)
            {
                var p = nodes[i].position;
                polyWorld.Add(p);
                c += (Vector2)p;
            }
            c /= Mathf.Max(1, nodes.Count);

            polyWorld.Sort((a, b) =>
            {
                Vector2 da = (Vector2)a - c;
                Vector2 db = (Vector2)b - c;
                return Mathf.Atan2(da.y, da.x).CompareTo(Mathf.Atan2(db.y, db.x));
            });

            for (int i = 0; i < polyWorld.Count; i++)
            {
                Vector2 p = polyWorld[i];
                Vector2 dir = p - c;
                float len = dir.magnitude;
                dir = (len > 1e-6f) ? (dir / len) : Vector2.up;
                polyWorld[i] = p + dir * nodes[0].transform.localScale.x / 2f;
            }

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

            tris.Clear();
            for (int i = 1; i < polyLocal.Count - 1; i++)
            {
                tris.Add(0);
                tris.Add(i);
                tris.Add(i + 1);
            }

            mesh.Clear();
            mesh.SetVertices(polyLocal);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0, true);
            mesh.RecalculateBounds();
        }
    }
}