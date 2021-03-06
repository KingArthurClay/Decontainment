using System;
using System.Collections.Generic;
using UnityEngine;

public class Pools : SceneSingleton<Pools>
{
    [Serializable]
    private struct PoolInitData
    {
        public GameObject prefab;
        public int size;
    }

    private struct PoolData
    {
        public GameObject prefab;
        public Stack<GameObject> pool;
        public Transform parent;
    }

    [SerializeField]
    private PoolInitData[] initialPools = null;

    private Dictionary<GameObject, PoolData> pools = new Dictionary<GameObject, PoolData>();
    private Dictionary<GameObject, PoolData> inUseObjToPrefabMap = new Dictionary<GameObject, PoolData>();

    void Awake()
    {
        // Initialize initial pools
        foreach (PoolInitData poolInitData in initialPools) {
            PoolData poolData = AddPool(poolInitData);

            for (int objectNum = 0; objectNum < poolInitData.size; ++objectNum) {
                GameObject go = Instantiate<GameObject>(poolData.prefab, Vector3.zero, Quaternion.identity, poolData.parent);
                go.SetActive(false);
                poolData.pool.Push(go);
            }
        }
    }

    public GameObject Get(GameObject prefab, Vector3 position = new Vector3(), Quaternion rotation = new Quaternion(), Transform parent = null)
    {
        GameObject go;
        if (!pools.TryGetValue(prefab, out PoolData poolData)) {
            poolData = AddPool(new PoolInitData()
            {
                prefab = prefab,
                size = 1,
            });

            go = Instantiate<GameObject>(poolData.prefab, Vector3.zero, Quaternion.identity);
        } else {
            if (poolData.pool.Count == 0) {
                go = Instantiate<GameObject>(poolData.prefab, Vector3.zero, Quaternion.identity);
            } else {
                go = poolData.pool.Pop();
                go.SetActive(true);
                go.transform.SetParent(null);
                go.transform.position = position;
                go.transform.rotation = rotation;
            }
        }
        inUseObjToPrefabMap.Add(go, poolData);
        return go;
    }

    public void Free(GameObject gameObject)
    {
        if (inUseObjToPrefabMap.TryGetValue(gameObject, out PoolData poolData)) {
            gameObject.SetActive(false);
            gameObject.transform.SetParent(poolData.parent);
            poolData.pool.Push(gameObject);
            inUseObjToPrefabMap.Remove(gameObject);
        } else {
            Debug.LogError("Attempted to free non-pooled GameObject " + gameObject.ToString());
        }
    }

    private PoolData AddPool(PoolInitData poolInitData)
    {
        PoolData poolData = new PoolData()
        {
            prefab = poolInitData.prefab,
            pool = new Stack<GameObject>(poolInitData.size),
            parent = new GameObject(poolInitData.prefab.ToString()).transform,
        };
        poolData.parent.SetParent(transform);
        pools.Add(poolInitData.prefab, poolData);
        return poolData;
    }
}