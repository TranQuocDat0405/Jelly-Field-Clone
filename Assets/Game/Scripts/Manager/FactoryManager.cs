using System.Collections.Generic;
using UnityEngine;
using NFramework;

namespace Game.Manager
{
    public class FactoryManager : SingletonMono<FactoryManager>
    {
        private Dictionary<PooledObject, Pool> _poolDic = new Dictionary<PooledObject, Pool>();

        protected override void Awake()
        {
            base.Awake();
            
            // Get all pool components in children
            var pools = GetComponentsInChildren<Pool>(true);
            foreach (var pool in pools)
            {
                if (pool.ObjectToPool != null)
                {
                    pool.InitializePool();
                    _poolDic[pool.ObjectToPool] = pool;
                }
            }
        }

        public PooledObject GetObjectFromPool(PooledObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[FactoryManager] Prefab is null!");
                return null;
            }

            if (_poolDic.TryGetValue(prefab, out var pool))
            {
                return pool.GetPooledObject();
            }
            else
            {
                // Dynamic pool creation fallback if prefab is requested but not pre-initialized
                var newPool = Pool.CreatePool(true, true, 5, prefab);
                newPool.transform.SetParent(transform);
                _poolDic[prefab] = newPool;
                return newPool.GetPooledObject();
            }
        }
    }
}
