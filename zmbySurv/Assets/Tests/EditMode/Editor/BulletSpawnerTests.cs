using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Weapons.Combat;

namespace Weapons.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="BulletSpawner"/> pool behavior.
    /// </summary>
    public sealed class BulletSpawnerTests
    {
        private readonly List<GameObject> m_CreatedObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < m_CreatedObjects.Count; index++)
            {
                if (m_CreatedObjects[index] != null)
                {
                    Object.DestroyImmediate(m_CreatedObjects[index]);
                }
            }

            m_CreatedObjects.Clear();
        }

        [Test]
        public void DispatchShotTrace_WhenCalled_SpawnsActiveBullet()
        {
            BulletSpawner spawner = CreateSpawnerWithPrefab(initialCapacity: 1);

            spawner.DispatchShotTrace(CreateTrace(Vector2.zero, new Vector2(2f, 0f)));

            Assert.That(spawner.ActiveBulletCount, Is.EqualTo(1));
            Assert.That(spawner.InactiveBulletCount, Is.EqualTo(0));
        }

        [Test]
        public void ReleaseBullet_WhenCalled_ReturnsBulletToPool()
        {
            BulletSpawner spawner = CreateSpawnerWithPrefab(initialCapacity: 1);
            spawner.DispatchShotTrace(CreateTrace(Vector2.zero, new Vector2(2f, 0f)));
            Bullet activeBullet = GetOnlyActiveBullet(spawner);

            spawner.ReleaseBullet(activeBullet);

            Assert.That(spawner.ActiveBulletCount, Is.EqualTo(0));
            Assert.That(spawner.InactiveBulletCount, Is.EqualTo(1));
        }

        [Test]
        public void DispatchShotTrace_AfterRelease_ReusesSameBulletInstance()
        {
            BulletSpawner spawner = CreateSpawnerWithPrefab(initialCapacity: 1);
            spawner.DispatchShotTrace(CreateTrace(Vector2.zero, new Vector2(1f, 0f)));
            Bullet first = GetOnlyActiveBullet(spawner);
            spawner.ReleaseBullet(first);

            spawner.DispatchShotTrace(CreateTrace(Vector2.zero, new Vector2(3f, 0f)));
            Bullet second = GetOnlyActiveBullet(spawner);

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void ClearActiveBullets_WhenMultipleActive_ReturnsAllToPool()
        {
            BulletSpawner spawner = CreateSpawnerWithPrefab(initialCapacity: 1);
            spawner.DispatchShotTrace(CreateTrace(Vector2.zero, new Vector2(2f, 0f)));
            spawner.DispatchShotTrace(CreateTrace(Vector2.zero, new Vector2(3f, 0f)));
            Assert.That(spawner.ActiveBulletCount, Is.EqualTo(2));

            spawner.ClearActiveBullets();

            Assert.That(spawner.ActiveBulletCount, Is.EqualTo(0));
            Assert.That(spawner.InactiveBulletCount, Is.GreaterThanOrEqualTo(2));
        }

        private BulletSpawner CreateSpawnerWithPrefab(int initialCapacity)
        {
            GameObject root = new GameObject("BulletSpawnerRoot");
            m_CreatedObjects.Add(root);
            BulletSpawner spawner = root.AddComponent<BulletSpawner>();

            GameObject prefabObject = new GameObject("BulletPrefab");
            m_CreatedObjects.Add(prefabObject);
            Bullet prefabBullet = prefabObject.AddComponent<Bullet>();

            SetPrivateField(spawner, "m_BulletPrefab", prefabBullet);
            SetPrivateField(spawner, "m_InitialPoolCapacity", initialCapacity);
            return spawner;
        }

        private static Bullet GetOnlyActiveBullet(BulletSpawner spawner)
        {
            foreach (Bullet bullet in spawner.ActiveBullets)
            {
                return bullet;
            }

            Assert.Fail("Expected one active bullet.");
            return null;
        }

        private static WeaponShotTrace CreateTrace(Vector2 origin, Vector2 endPoint)
        {
            Vector2 direction = (endPoint - origin).sqrMagnitude > 0.0001f
                ? (endPoint - origin).normalized
                : Vector2.right;

            float distance = Vector2.Distance(origin, endPoint);
            return new WeaponShotTrace(
                weaponId: "test_weapon",
                pelletIndex: 0,
                pelletCount: 1,
                origin: origin,
                direction: direction,
                endPoint: endPoint,
                maxRange: distance,
                traveledDistance: distance,
                impactType: WeaponShotImpactType.None,
                impactCollider: null);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Missing private field '{fieldName}'.");
            fieldInfo.SetValue(target, value);
        }
    }
}
