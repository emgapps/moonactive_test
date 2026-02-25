using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Weapons.Combat;

namespace Weapons.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="Bullet"/> motion and pool lifecycle behavior.
    /// </summary>
    public sealed class BulletTests
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
        public void Launch_WhenTicked_MovesAndCompletesAtDestination()
        {
            Bullet bullet = CreateBullet("Bullet_Move");
            int completionCount = 0;

            bullet.Launch(
                shotTrace: CreateTrace(origin: Vector2.zero, endPoint: new Vector2(1f, 0f)),
                speedUnitsPerSecond: 2f,
                onReachedDestination: _ => completionCount += 1);

            bullet.Tick(0.2f);
            Assert.That(bullet.IsInFlight, Is.True);
            Assert.That(bullet.transform.position.x, Is.EqualTo(0.4f).Within(0.01f));

            bullet.Tick(0.4f);
            Assert.That(bullet.IsInFlight, Is.False);
            Assert.That(bullet.transform.position.x, Is.EqualTo(1f).Within(0.01f));
            Assert.That(completionCount, Is.EqualTo(1));

            bullet.Tick(0.5f);
            Assert.That(completionCount, Is.EqualTo(1));
        }

        [Test]
        public void Launch_WhenSpeedNonPositive_UsesDefaultSpeedFallback()
        {
            Bullet bullet = CreateBullet("Bullet_DefaultSpeed");

            bullet.Launch(
                shotTrace: CreateTrace(origin: Vector2.zero, endPoint: new Vector2(10f, 0f)),
                speedUnitsPerSecond: 0f);

            Assert.That(bullet.ActiveSpeedUnitsPerSecond, Is.GreaterThan(0f));

            bullet.Tick(0.1f);
            Assert.That(bullet.transform.position.x, Is.GreaterThan(0f));
        }

        [Test]
        public void OnReturnedToPool_WhenBulletInFlight_ResetsFlightState()
        {
            Bullet bullet = CreateBullet("Bullet_Reset");
            int completionCount = 0;

            bullet.Launch(
                shotTrace: CreateTrace(
                    origin: Vector2.zero,
                    endPoint: new Vector2(5f, 0f),
                    impactType: WeaponShotImpactType.BlockingCollider),
                speedUnitsPerSecond: 1f,
                onReachedDestination: _ => completionCount += 1);

            bullet.OnReturnedToPool();
            bullet.Tick(10f);

            Assert.That(bullet.IsInFlight, Is.False);
            Assert.That(bullet.ActiveSpeedUnitsPerSecond, Is.EqualTo(0f));
            Assert.That(bullet.ImpactType, Is.EqualTo(WeaponShotImpactType.None));
            Assert.That(completionCount, Is.EqualTo(0));
        }

        private Bullet CreateBullet(string objectName)
        {
            GameObject bulletObject = new GameObject(objectName);
            m_CreatedObjects.Add(bulletObject);
            return bulletObject.AddComponent<Bullet>();
        }

        private static WeaponShotTrace CreateTrace(
            Vector2 origin,
            Vector2 endPoint,
            WeaponShotImpactType impactType = WeaponShotImpactType.None)
        {
            return new WeaponShotTrace(
                weaponId: "test_weapon",
                pelletIndex: 0,
                pelletCount: 1,
                origin: origin,
                direction: (endPoint - origin).normalized,
                endPoint: endPoint,
                maxRange: Vector2.Distance(origin, endPoint),
                traveledDistance: Vector2.Distance(origin, endPoint),
                impactType: impactType,
                impactCollider: null);
        }
    }
}
