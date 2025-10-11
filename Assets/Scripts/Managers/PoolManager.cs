// Path: Scripts/Pool/PoolManager.cs
using System.Collections.Generic;
using UnityEngine;

public class PoolManager
{
    private Dictionary<GameObject, Queue<GameObject>> _pools = new();
    private Dictionary<GameObject, GameObject> _instanceToPrefab = new();

    private Transform _PooledObjects;
    private Transform PooledObjects
    {
        get
        {
            if (_PooledObjects == null)
            {
                var rootObj = GameObject.Find("PooledObjects");
                if (rootObj == null)
                    rootObj = new GameObject("PooledObjects");
                _PooledObjects = rootObj.transform;
            }
            return _PooledObjects;
        }
    }

    public void CleanPool()
    {
        _pools.Clear();
        _instanceToPrefab.Clear();
    }

    public void CreatePool(GameObject prefab, int initialSize = 1)
    {
        if (prefab == null || _pools.ContainsKey(prefab)) return;

        var q = new Queue<GameObject>();
        for (int i = 0; i < initialSize; i++)
        {
            var go = Object.Instantiate(prefab);
            go.SetActive(false);
            _instanceToPrefab[go] = prefab;
            q.Enqueue(go);
        }
        _pools[prefab] = q;
    }

    public GameObject Spawn(GameObject prefab)
    {
        if (prefab == null) return null;

        if (!_pools.TryGetValue(prefab, out var q))
        {
            CreatePool(prefab, 0);
            q = _pools[prefab];
        }

        GameObject instance = (q.Count > 0) ? q.Dequeue() : Object.Instantiate(prefab);
        _instanceToPrefab[instance] = prefab;
        instance.SetActive(true);
        return instance;
    }

    public GameObject Spawn(GameObject prefab, Transform parent)
    {
        GameObject instance = Spawn(prefab);
        if (instance == null) return null;
        instance.transform.SetParent(parent, false);
        return instance;
    }

    public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        GameObject instance = Spawn(prefab);
        if (instance == null) return null;
        instance.transform.SetPositionAndRotation(pos, rot);
        return instance;
    }

    public void ReturnToPool(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null)
        {
            Object.Destroy(instance);
            return;
        }

        instance.transform.SetParent(PooledObjects, false);
        instance.SetActive(false);

        if (!_pools.TryGetValue(prefab, out var q))
            _pools[prefab] = q = new Queue<GameObject>();

        q.Enqueue(instance);
    }

    public void DestroyPooled(GameObject instance)
    {
        if (instance == null) return;

        if (_instanceToPrefab.TryGetValue(instance, out var prefab))
        {
            ReturnToPool(prefab, instance);
        }
        else
        {
            Object.Destroy(instance);
        }
    }
}