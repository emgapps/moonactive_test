using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Weapons.Combat;
using Weapons.Runtime;

namespace Weapons.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="WeaponHitResolver"/> trace dispatch behavior.
    /// </summary>
    public sealed class WeaponHitResolverTraceTests
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
        public void ResolveShot_WhenNoColliderHit_DispatchesMaxRangeTrace()
        {
            WeaponHitResolver resolver = new WeaponHitResolver();
            TraceCollector traceCollector = new TraceCollector();
            WeaponShotRequest shotRequest = CreateShotRequest(range: 5f);

            int appliedHits = resolver.ResolveShot(
                shot: shotRequest,
                origin: Vector2.zero,
                direction: Vector2.right,
                hitMask: Physics2D.DefaultRaycastLayers,
                ownerCollider: null,
                shotTraceDispatcher: traceCollector);

            Assert.That(appliedHits, Is.EqualTo(0));
            Assert.That(traceCollector.Traces.Count, Is.EqualTo(1));

            WeaponShotTrace trace = traceCollector.Traces[0];
            Assert.That(trace.ImpactType, Is.EqualTo(WeaponShotImpactType.None));
            Assert.That(trace.ImpactCollider, Is.Null);
            Assert.That(trace.EndPoint.x, Is.EqualTo(5f).Within(0.05f));
            Assert.That(trace.EndPoint.y, Is.EqualTo(0f).Within(0.05f));
            Assert.That(trace.TraveledDistance, Is.EqualTo(5f).Within(0.05f));
        }

        [Test]
        public void ResolveShot_WhenEnemyIsHit_DispatchesEnemyImpactTraceAndAppliesDamage()
        {
            WeaponHitResolver resolver = new WeaponHitResolver();
            TraceCollector traceCollector = new TraceCollector();
            WeaponShotRequest shotRequest = CreateShotRequest(damage: 3, range: 8f);

            EnemyDamageable enemyDamageable = CreateEnemyDamageable("EnemyTarget", new Vector2(2f, 0f), health: 10, out BoxCollider2D enemyCollider);

            int appliedHits = resolver.ResolveShot(
                shot: shotRequest,
                origin: Vector2.zero,
                direction: Vector2.right,
                hitMask: Physics2D.DefaultRaycastLayers,
                ownerCollider: null,
                shotTraceDispatcher: traceCollector);

            Assert.That(appliedHits, Is.EqualTo(1));
            Assert.That(enemyDamageable.CurrentHealth, Is.EqualTo(7));
            Assert.That(traceCollector.Traces.Count, Is.EqualTo(1));

            WeaponShotTrace trace = traceCollector.Traces[0];
            Assert.That(trace.ImpactType, Is.EqualTo(WeaponShotImpactType.Enemy));
            Assert.That(trace.ImpactCollider, Is.SameAs(enemyCollider));
            Assert.That(trace.EndPoint.x, Is.GreaterThan(1f).And.LessThan(2f));
            Assert.That(trace.EndPoint.y, Is.EqualTo(0f).Within(0.05f));
            Assert.That(trace.TraveledDistance, Is.GreaterThan(1f).And.LessThan(2f));
        }

        [Test]
        public void ResolveShot_WhenBlockingColliderIsCloserThanEnemy_DispatchesBlockingImpactTrace()
        {
            WeaponHitResolver resolver = new WeaponHitResolver();
            TraceCollector traceCollector = new TraceCollector();
            WeaponShotRequest shotRequest = CreateShotRequest(damage: 3, range: 10f);

            BoxCollider2D wallCollider = CreateWallCollider("WallBlocker", new Vector2(1.5f, 0f));
            EnemyDamageable enemyDamageable = CreateEnemyDamageable("EnemyBehindWall", new Vector2(3f, 0f), health: 10, out _);

            int appliedHits = resolver.ResolveShot(
                shot: shotRequest,
                origin: Vector2.zero,
                direction: Vector2.right,
                hitMask: Physics2D.DefaultRaycastLayers,
                ownerCollider: null,
                shotTraceDispatcher: traceCollector);

            Assert.That(appliedHits, Is.EqualTo(0));
            Assert.That(enemyDamageable.CurrentHealth, Is.EqualTo(10));
            Assert.That(traceCollector.Traces.Count, Is.EqualTo(1));

            WeaponShotTrace trace = traceCollector.Traces[0];
            Assert.That(trace.ImpactType, Is.EqualTo(WeaponShotImpactType.BlockingCollider));
            Assert.That(trace.ImpactCollider, Is.SameAs(wallCollider));
            Assert.That(trace.EndPoint.x, Is.GreaterThan(0.9f).And.LessThan(1.6f));
            Assert.That(trace.TraveledDistance, Is.GreaterThan(0.9f).And.LessThan(1.6f));
        }

        private EnemyDamageable CreateEnemyDamageable(string objectName, Vector2 position, int health, out BoxCollider2D collider)
        {
            GameObject enemyObject = new GameObject(objectName);
            enemyObject.transform.position = position;
            enemyObject.layer = LayerMask.NameToLayer("Default");
            m_CreatedObjects.Add(enemyObject);

            collider = enemyObject.AddComponent<BoxCollider2D>();
            EnemyDamageable damageable = enemyObject.AddComponent<EnemyDamageable>();
            damageable.ConfigureHealth(health);
            return damageable;
        }

        private BoxCollider2D CreateWallCollider(string objectName, Vector2 position)
        {
            GameObject wallObject = new GameObject(objectName);
            wallObject.transform.position = position;
            wallObject.layer = LayerMask.NameToLayer("Default");
            m_CreatedObjects.Add(wallObject);
            return wallObject.AddComponent<BoxCollider2D>();
        }

        private static WeaponShotRequest CreateShotRequest(int damage = 4, float range = 8f)
        {
            return new WeaponShotRequest(
                weaponId: "test_weapon",
                damagePerPellet: damage,
                range: range,
                pelletCount: 1,
                spreadAngleDegrees: 0f);
        }

        private sealed class TraceCollector : IWeaponShotTraceDispatcher
        {
            private readonly List<WeaponShotTrace> m_Traces = new List<WeaponShotTrace>();

            public IReadOnlyList<WeaponShotTrace> Traces => m_Traces;

            public void DispatchShotTrace(WeaponShotTrace shotTrace)
            {
                m_Traces.Add(shotTrace);
            }
        }
    }
}
