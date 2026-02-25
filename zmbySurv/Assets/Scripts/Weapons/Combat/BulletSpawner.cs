using System;
using System.Collections.Generic;
using Core.Pooling;
using UnityEngine;

namespace Weapons.Combat
{
    /// <summary>
    /// Spawns and recycles visual bullets from resolved shot traces.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BulletSpawner : MonoBehaviour, IWeaponShotTraceDispatcher
    {
        private const int MinPoolCapacity = 1;

        [Header("Pool")]
        [SerializeField]
        private Bullet m_BulletPrefab;
        [SerializeField]
        private Transform m_BulletRoot;
        [SerializeField]
        private int m_InitialPoolCapacity = 12;

        [Header("Motion")]
        [SerializeField]
        private float m_BulletSpeedUnitsPerSecond = 20f;

        private readonly HashSet<Bullet> m_ActiveBullets = new HashSet<Bullet>();
        private readonly List<Bullet> m_ReleaseBuffer = new List<Bullet>();

        private GenericObjectPool<Bullet> m_BulletPool;
        private Action<Bullet> m_OnBulletReachedDestination;

        /// <summary>
        /// Gets currently active bullet count.
        /// </summary>
        public int ActiveBulletCount => m_ActiveBullets.Count;

        /// <summary>
        /// Gets currently inactive bullet count.
        /// </summary>
        public int InactiveBulletCount => m_BulletPool != null ? m_BulletPool.InactiveCount : 0;

        /// <summary>
        /// Gets read-only active bullet collection.
        /// </summary>
        public IReadOnlyCollection<Bullet> ActiveBullets => m_ActiveBullets;

        private void Awake()
        {
            EnsurePoolInitialized(logWhenMissingPrefab: false);
        }

        private void OnDestroy()
        {
            ClearActiveBullets();
            m_BulletPool?.Clear();
            m_BulletPool = null;
            m_OnBulletReachedDestination = null;
            m_ReleaseBuffer.Clear();
            m_ActiveBullets.Clear();
        }

        /// <summary>
        /// Spawns one visual bullet for resolved trace payload.
        /// </summary>
        /// <param name="shotTrace">Resolved pellet trace payload.</param>
        public void DispatchShotTrace(WeaponShotTrace shotTrace)
        {
            EnsurePoolInitialized(logWhenMissingPrefab: true);
            if (m_BulletPool == null)
            {
                Debug.LogWarning("[BulletSpawner] DispatchSkipped | reason=pool_not_initialized");
                return;
            }

            Bullet bullet = m_BulletPool.Get();
            if (bullet == null)
            {
                Debug.LogError("[BulletSpawner] DispatchFailed | reason=pool_returned_null");
                return;
            }

            if (!m_ActiveBullets.Add(bullet))
            {
                Debug.LogWarning($"[BulletSpawner] ActiveDuplicate | bullet={bullet.name}");
            }

            m_OnBulletReachedDestination ??= HandleBulletReachedDestination;
            bullet.Launch(shotTrace, m_BulletSpeedUnitsPerSecond, m_OnBulletReachedDestination);
        }

        /// <summary>
        /// Returns an active bullet back to pool.
        /// </summary>
        /// <param name="bullet">Bullet instance to release.</param>
        public void ReleaseBullet(Bullet bullet)
        {
            if (bullet == null)
            {
                Debug.LogWarning("[BulletSpawner] ReleaseSkipped | reason=null_bullet");
                return;
            }

            if (m_BulletPool == null)
            {
                Debug.LogWarning($"[BulletSpawner] ReleaseFallbackDestroy | bullet={bullet.name} reason=missing_pool");
                Destroy(bullet.gameObject);
                return;
            }

            if (!m_ActiveBullets.Remove(bullet))
            {
                Debug.LogWarning($"[BulletSpawner] ReleaseRejected | bullet={bullet.name} reason=not_active");
                return;
            }

            m_BulletPool.Release(bullet);
        }

        /// <summary>
        /// Returns all active bullets to pool.
        /// </summary>
        public void ClearActiveBullets()
        {
            if (m_ActiveBullets.Count == 0 || m_BulletPool == null)
            {
                return;
            }

            m_ReleaseBuffer.Clear();
            foreach (Bullet activeBullet in m_ActiveBullets)
            {
                m_ReleaseBuffer.Add(activeBullet);
            }

            for (int index = 0; index < m_ReleaseBuffer.Count; index++)
            {
                Bullet activeBullet = m_ReleaseBuffer[index];
                if (activeBullet != null)
                {
                    m_BulletPool.Release(activeBullet);
                }
            }

            m_ReleaseBuffer.Clear();
            m_ActiveBullets.Clear();
        }

        private void EnsurePoolInitialized(bool logWhenMissingPrefab)
        {
            if (m_BulletPool != null)
            {
                return;
            }

            if (m_BulletPrefab == null)
            {
                if (logWhenMissingPrefab)
                {
                    Debug.LogError("[BulletSpawner] PoolInitializationFailed | reason=missing_bullet_prefab");
                }
                return;
            }

            int initialCapacity = Mathf.Max(MinPoolCapacity, m_InitialPoolCapacity);
            m_BulletPool = new GenericObjectPool<Bullet>(
                createInstance: CreateBulletInstance,
                onGet: HandleBulletTakenFromPool,
                onRelease: HandleBulletReturnedToPool,
                onDestroy: HandleBulletDestroyedFromPool,
                initialCapacity: initialCapacity);

            Debug.Log($"[BulletSpawner] PoolInitialized | initialCapacity={initialCapacity}");
        }

        private Bullet CreateBulletInstance()
        {
            Transform bulletRoot = ResolveBulletRoot();
            Bullet bulletInstance = Instantiate(m_BulletPrefab, bulletRoot);
            bulletInstance.name = $"{m_BulletPrefab.name}_Pooled";
            return bulletInstance;
        }

        private void HandleBulletTakenFromPool(Bullet bullet)
        {
            if (bullet == null)
            {
                return;
            }

            bullet.transform.SetParent(ResolveBulletRoot(), true);
            bullet.gameObject.SetActive(true);
        }

        private void HandleBulletReturnedToPool(Bullet bullet)
        {
            if (bullet == null)
            {
                return;
            }

            bullet.transform.SetParent(ResolveBulletRoot(), true);
            bullet.gameObject.SetActive(false);
        }

        private void HandleBulletDestroyedFromPool(Bullet bullet)
        {
            if (bullet == null)
            {
                return;
            }

            Destroy(bullet.gameObject);
        }

        private void HandleBulletReachedDestination(Bullet bullet)
        {
            ReleaseBullet(bullet);
        }

        private Transform ResolveBulletRoot()
        {
            return m_BulletRoot != null ? m_BulletRoot : transform;
        }
    }
}
