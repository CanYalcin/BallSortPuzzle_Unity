using System.Collections.Generic;
using UnityEngine;

namespace HyperBase.ObjectPool
{
    /// <summary>
    /// Generic prefab-based object pool.
    /// Usage: _pool.Prewarm(prefab, 20); var obj = _pool.Rent(prefab, pos); _pool.Return(obj);
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        private readonly Dictionary<GameObject, Queue<GameObject>> _poolDict  = new();
        private readonly Dictionary<GameObject, GameObject>        _originMap = new();
        private Transform _poolRoot;

        private void Awake()
        {
            _poolRoot = new GameObject("[PoolRoot]").transform;
            _poolRoot.SetParent(transform);
            DontDestroyOnLoad(gameObject);
        }

        public void Prewarm(GameObject prefab, int count)
        {
            for (int i = 0; i < count; i++)
                Return(CreateNew(prefab));
        }

        public GameObject Rent(GameObject prefab, Vector3 position = default, Quaternion rotation = default)
        {
            if (!_poolDict.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>();
                _poolDict[prefab] = queue;
            }
            var obj = queue.Count > 0 ? queue.Dequeue() : CreateNew(prefab);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            if (obj.TryGetComponent<IPoolable>(out var poolable))
                poolable.OnSpawn();
            return obj;
        }

        public T Rent<T>(GameObject prefab, Vector3 position = default, Quaternion rotation = default)
            where T : Component => Rent(prefab, position, rotation).GetComponent<T>();

        public void Return(GameObject obj)
        {
            if (obj == null) return;
            if (obj.TryGetComponent<IPoolable>(out var poolable))
                poolable.OnDespawn();
            obj.SetActive(false);
            obj.transform.SetParent(_poolRoot);
            if (_originMap.TryGetValue(obj, out var origin))
            {
                if (!_poolDict.TryGetValue(origin, out var queue))
                {
                    queue = new Queue<GameObject>();
                    _poolDict[origin] = queue;
                }
                queue.Enqueue(obj);
            }
            else
            {
                Debug.LogWarning($"[ObjectPool] Untracked object '{obj.name}'. Destroying.");
                Object.Destroy(obj);
            }
        }

        public void ClearPool(GameObject prefab)
        {
            if (!_poolDict.TryGetValue(prefab, out var queue)) return;
            foreach (var obj in queue)
            {
                if (obj != null) Object.Destroy(obj);
            }
            queue.Clear();
        }

        public void ClearAll()
        {
            foreach (var queue in _poolDict.Values)
                foreach (var obj in queue)
                    if (obj != null) Object.Destroy(obj);
            _poolDict.Clear();
            _originMap.Clear();
        }

        private GameObject CreateNew(GameObject prefab)
        {
            var obj = Instantiate(prefab, _poolRoot);
            obj.SetActive(false);
            _originMap[obj] = prefab;
            return obj;
        }
    }
}
