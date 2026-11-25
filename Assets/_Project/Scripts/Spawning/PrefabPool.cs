using UnityEngine;
using UnityEngine.Pool;

namespace NinjaRunner
{
    /// <summary>
    /// Small wrapper around Unity's built-in <see cref="ObjectPool{T}"/> for one prefab.
    /// Instead of Instantiate/Destroy every few metres (which causes garbage-collection
    /// stutters on mobile), objects are reused: Get() takes one from the pool and
    /// Release() puts it back.
    /// </summary>
    public class PrefabPool<T> where T : Component
    {
        private readonly ObjectPool<T> pool;

        public PrefabPool(T prefab, Transform parent, int prewarmCount = 0, int maxSize = 200)
        {
            pool = new ObjectPool<T>(
                createFunc: () =>
                {
                    T instance = Object.Instantiate(prefab, parent);
                    instance.gameObject.SetActive(false);
                    return instance;
                },
                actionOnGet: null, // activated in Get() after it has been positioned
                actionOnRelease: instance => instance.gameObject.SetActive(false),
                actionOnDestroy: instance => Object.Destroy(instance.gameObject),
                collectionCheck: false,
                defaultCapacity: Mathf.Max(1, prewarmCount),
                maxSize: maxSize);

            Prewarm(prewarmCount);
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            T instance = pool.Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(T instance) => pool.Release(instance);

        private void Prewarm(int count)
        {
            if (count <= 0) return;
            var temp = new T[count];
            for (int i = 0; i < count; i++) temp[i] = pool.Get();
            for (int i = 0; i < count; i++) pool.Release(temp[i]);
        }
    }
}
