using System.Collections.Generic;
using System.Reflection;
using Characters;
using Level;
using NUnit.Framework;
using UnityEngine;
using Weapons.Combat;

namespace Level.Tests.EditMode
{
    /// <summary>
    /// Verifies runtime enemy combat setup performed by <see cref="LevelLoader"/>.
    /// </summary>
    public sealed class LevelLoaderEnemyCombatSetupTests
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
        public void EnsureEnemyCombatCollider_WhenMissing_AddsTriggerCollider()
        {
            LevelLoader loader = CreateLoader(defaultZombieHealth: 20, defaultZombieColliderRadius: 0.5f);
            GameObject enemyObject = CreateEnemyObject();

            InvokePrivateMethod(loader, "EnsureEnemyCombatCollider", enemyObject);

            Collider2D collider = enemyObject.GetComponent<Collider2D>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.isTrigger, Is.True);
        }

        [Test]
        public void EnsureEnemyDamageable_WhenMissing_AddsAndConfiguresHealth()
        {
            const int ExpectedHealth = 37;
            LevelLoader loader = CreateLoader(defaultZombieHealth: ExpectedHealth, defaultZombieColliderRadius: 0.45f);
            GameObject enemyObject = CreateEnemyObject();

            InvokePrivateMethod(loader, "EnsureEnemyDamageable", enemyObject, null);

            EnemyDamageable damageable = enemyObject.GetComponent<EnemyDamageable>();
            Assert.That(damageable, Is.Not.Null);
            Assert.That(damageable.MaxHealth, Is.EqualTo(ExpectedHealth));
            Assert.That(damageable.CurrentHealth, Is.EqualTo(ExpectedHealth));
        }

        [Test]
        public void EnsureEnemyDamageable_WhenControllerProvided_ConfiguresDamageCallback()
        {
            LevelLoader loader = CreateLoader(defaultZombieHealth: 25, defaultZombieColliderRadius: 0.45f);
            GameObject enemyObject = CreateEnemyObject();
            TestEnemyController enemyController = new TestEnemyController();

            InvokePrivateMethod(loader, "EnsureEnemyDamageable", enemyObject, enemyController);

            EnemyDamageable damageable = enemyObject.GetComponent<EnemyDamageable>();
            Assert.That(damageable, Is.Not.Null);

            bool damageApplied = damageable.TryApplyDamage(3, Vector2.zero, "test_weapon");
            Assert.That(damageApplied, Is.True);
            Assert.That(enemyController.OnDamageCallCount, Is.EqualTo(1));
        }

        private LevelLoader CreateLoader(int defaultZombieHealth, float defaultZombieColliderRadius)
        {
            GameObject loaderObject = new GameObject("LevelLoaderTest");
            m_CreatedObjects.Add(loaderObject);

            LevelLoader loader = loaderObject.AddComponent<LevelLoader>();
            SetPrivateField(loader, "m_DefaultZombieHealth", defaultZombieHealth);
            SetPrivateField(loader, "m_DefaultZombieColliderRadius", defaultZombieColliderRadius);
            return loader;
        }

        private GameObject CreateEnemyObject()
        {
            GameObject enemyObject = new GameObject("Enemy");
            m_CreatedObjects.Add(enemyObject);
            enemyObject.layer = LayerMask.NameToLayer("Default");
            return enemyObject;
        }

        private static void InvokePrivateMethod(object instance, string methodName, params object[] parameters)
        {
            MethodInfo methodInfo = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(methodInfo, Is.Not.Null, $"Expected private method '{methodName}' to exist.");
            methodInfo.Invoke(instance, parameters);
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
            fieldInfo.SetValue(instance, value);
        }

        private sealed class TestEnemyController : IEnemyController
        {
            public string ControllerName => "TestEnemyController";

            public IReadOnlyList<Vector2> PatrolPoints => System.Array.Empty<Vector2>();

            public Transform PlayerTarget => null;

            public float PatrolSpeed => 0f;

            public float ChaseSpeed => 0f;

            public Vector2 CurrentPosition => Vector2.zero;

            public int OnDamageCallCount { get; private set; }

            public void ApplyLevelConfiguration(
                float moveSpeed,
                float chaseSpeed,
                float sightRange,
                float attackRange,
                int attackPower,
                List<Vector2> patrolPoints)
            {
            }

            public void MoveTo(Vector2 target)
            {
            }

            public void MoveTo(Vector2 target, float movementSpeed)
            {
            }

            public void StopMovement()
            {
            }

            public void PlayAnimation(string animation)
            {
            }

            public bool IsPlayerVisible()
            {
                return false;
            }

            public bool IsPlayerInAttackRange()
            {
                return false;
            }

            public void Attack()
            {
            }

            public void OnDamage()
            {
                OnDamageCallCount += 1;
            }
        }
    }
}
