using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Pooling
{
    /// <summary>
    /// Generic component object pool with lifecycle hooks and ownership validation.
    /// </summary>
    /// <typeparam name="T">Component type managed by this pool.</typeparam>
    public sealed class GenericObjectPool<T> : IObjectPool<T> where T : Component
    {
        private readonly Func<T> m_CreateInstance;
        private readonly Action<T> m_OnGet;
        private readonly Action<T> m_OnRelease;
        private readonly Action<T> m_OnDestroy;
        private readonly Stack<T> m_InactiveObjects;
        private readonly HashSet<T> m_ActiveObjects;

        /// <summary>
        /// Creates a new pool.
        /// </summary>
        /// <param name="createInstance">Factory used when inactive objects are unavailable.</param>
        /// <param name="onGet">Optional callback executed when instance is taken from the pool.</param>
        /// <param name="onRelease">Optional callback executed when instance is returned to the pool.</param>
        /// <param name="onDestroy">Optional callback executed when instance is destroyed during clear.</param>
        /// <param name="initialCapacity">Optional number of instances to prewarm as inactive.</param>
        public GenericObjectPool(
            Func<T> createInstance,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            int initialCapacity = 0)
        {
            m_CreateInstance = createInstance ?? throw new ArgumentNullException(nameof(createInstance));
            m_OnGet = onGet;
            m_OnRelease = onRelease;
            m_OnDestroy = onDestroy;
            m_InactiveObjects = new Stack<T>(Mathf.Max(0, initialCapacity));
            m_ActiveObjects = new HashSet<T>();

            Prewarm(Mathf.Max(0, initialCapacity));
        }

        /// <summary>
        /// Gets count of currently active instances.
        /// </summary>
        public int ActiveCount => m_ActiveObjects.Count;

        /// <summary>
        /// Gets count of currently inactive instances.
        /// </summary>
        public int InactiveCount => m_InactiveObjects.Count;

        /// <summary>
        /// Gets read-only collection of active instances.
        /// </summary>
        public IReadOnlyCollection<T> ActiveObjects => m_ActiveObjects;

        /// <summary>
        /// Gets an active instance from the pool.
        /// </summary>
        /// <returns>An instance ready for use.</returns>
        public T Get()
        {
            T instance = ResolveInactiveInstance();
            if (instance == null)
            {
                instance = m_CreateInstance.Invoke();
                if (instance == null)
                {
                    throw new InvalidOperationException("Pool factory returned null instance.");
                }
            }

            if (!m_ActiveObjects.Add(instance))
            {
                Debug.LogWarning($"[ObjectPool] DuplicateGetIgnored | type={typeof(T).Name} object={instance.name}");
            }

            if (instance is IPoolable poolable)
            {
                poolable.OnTakenFromPool();
            }

            m_OnGet?.Invoke(instance);
            return instance;
        }

        /// <summary>
        /// Returns an instance back to the pool.
        /// </summary>
        /// <param name="instance">Instance to release.</param>
        public void Release(T instance)
        {
            if (instance == null)
            {
                Debug.LogWarning($"[ObjectPool] ReleaseSkipped | type={typeof(T).Name} reason=null_instance");
                return;
            }

            if (!m_ActiveObjects.Remove(instance))
            {
                Debug.LogWarning(
                    $"[ObjectPool] ReleaseRejected | type={typeof(T).Name} object={instance.name} reason=not_owned_or_already_released");
                return;
            }

            if (instance is IPoolable poolable)
            {
                poolable.OnReturnedToPool();
            }

            m_OnRelease?.Invoke(instance);
            m_InactiveObjects.Push(instance);
        }

        /// <summary>
        /// Destroys all managed instances and clears pool state.
        /// </summary>
        public void Clear()
        {
            List<T> activeSnapshot = new List<T>(m_ActiveObjects);
            for (int index = 0; index < activeSnapshot.Count; index++)
            {
                DestroyInstance(activeSnapshot[index]);
            }

            while (m_InactiveObjects.Count > 0)
            {
                DestroyInstance(m_InactiveObjects.Pop());
            }

            m_ActiveObjects.Clear();
            m_InactiveObjects.Clear();
        }

        private void Prewarm(int count)
        {
            for (int index = 0; index < count; index++)
            {
                T instance = m_CreateInstance.Invoke();
                if (instance == null)
                {
                    Debug.LogError($"[ObjectPool] PrewarmFailed | type={typeof(T).Name} reason=null_instance");
                    continue;
                }

                if (instance is IPoolable poolable)
                {
                    poolable.OnReturnedToPool();
                }

                m_OnRelease?.Invoke(instance);
                m_InactiveObjects.Push(instance);
            }
        }

        private T ResolveInactiveInstance()
        {
            while (m_InactiveObjects.Count > 0)
            {
                T candidate = m_InactiveObjects.Pop();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void DestroyInstance(T instance)
        {
            if (instance == null)
            {
                return;
            }

            if (m_OnDestroy != null)
            {
                m_OnDestroy.Invoke(instance);
                return;
            }

            UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}
