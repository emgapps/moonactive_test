using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Characters;
using Level;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Weapons.Combat;

namespace Weapons.Tests.PlayMode
{
    /// <summary>
    /// Integration tests for enemy damage callback wiring and visual feedback debounce.
    /// </summary>
    public sealed class EnemyDamageCallbacksIntegrationTests
    {
        private readonly List<GameObject> m_CreatedObjects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int index = 0; index < m_CreatedObjects.Count; index++)
            {
                if (m_CreatedObjects[index] != null)
                {
                    Object.Destroy(m_CreatedObjects[index]);
                }
            }

            m_CreatedObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnsureEnemyDamageable_WhenConfigured_DamageTriggersControllerFeedback()
        {
            const int ExpectedHealth = 20;
            Color baseColor = new Color(0.15f, 0.75f, 0.25f, 1f);

            LevelLoader loader = CreateLoader(defaultZombieHealth: ExpectedHealth);
            EnemyController enemyController = CreateEnemyController(baseColor);
            InvokeEnsureEnemyDamageable(loader, enemyController.gameObject, enemyController);

            EnemyDamageable damageable = enemyController.GetComponent<EnemyDamageable>();
            Assert.That(damageable, Is.Not.Null);
            Assert.That(damageable.CurrentHealth, Is.EqualTo(ExpectedHealth));

            SpriteRenderer spriteRenderer = enemyController.GetComponent<SpriteRenderer>();
            Assert.That(spriteRenderer, Is.Not.Null);
            Assert.That(spriteRenderer.color, Is.EqualTo(baseColor));

            bool damageApplied = damageable.TryApplyDamage(1, Vector2.zero, "pistol");
            Assert.That(damageApplied, Is.True);
            Assert.That(spriteRenderer.color, Is.EqualTo(Color.red));

            yield return new WaitForSeconds(0.15f);

            Assert.That(spriteRenderer.color, Is.EqualTo(baseColor));
        }

        [UnityTest]
        public IEnumerator EnemyController_OnDamage_DebouncesHighlightWhileFlashIsActive()
        {
            Color baseColor = new Color(0.35f, 0.45f, 0.85f, 1f);

            LevelLoader loader = CreateLoader(defaultZombieHealth: 30);
            EnemyController enemyController = CreateEnemyController(baseColor);
            InvokeEnsureEnemyDamageable(loader, enemyController.gameObject, enemyController);

            EnemyDamageable damageable = enemyController.GetComponent<EnemyDamageable>();
            SpriteRenderer spriteRenderer = enemyController.GetComponent<SpriteRenderer>();

            bool firstDamageApplied = damageable.TryApplyDamage(1, Vector2.zero, "pistol");
            Assert.That(firstDamageApplied, Is.True);
            Assert.That(spriteRenderer.color, Is.EqualTo(Color.red));

            yield return new WaitForSeconds(0.05f);

            bool secondDamageApplied = damageable.TryApplyDamage(1, Vector2.zero, "pistol");
            Assert.That(secondDamageApplied, Is.True);
            Assert.That(spriteRenderer.color, Is.EqualTo(Color.red));

            yield return new WaitForSeconds(0.08f);

            Assert.That(spriteRenderer.color, Is.EqualTo(baseColor));
        }

        private LevelLoader CreateLoader(int defaultZombieHealth)
        {
            GameObject loaderObject = new GameObject("LevelLoaderCallbacksTest");
            m_CreatedObjects.Add(loaderObject);

            LevelLoader loader = loaderObject.AddComponent<LevelLoader>();
            SetPrivateField(loader, "m_DefaultZombieHealth", defaultZombieHealth);
            SetPrivateField(loader, "m_DefaultZombieColliderRadius", 0.45f);
            return loader;
        }

        private EnemyController CreateEnemyController(Color baseColor)
        {
            GameObject enemyObject = new GameObject("EnemyCallbacksTest");
            m_CreatedObjects.Add(enemyObject);

            enemyObject.AddComponent<Rigidbody2D>().gravityScale = 0f;
            enemyObject.AddComponent<Animator>();
            SpriteRenderer spriteRenderer = enemyObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = baseColor;

            EnemyController enemyController = enemyObject.AddComponent<EnemyController>();
            return enemyController;
        }

        private static void InvokeEnsureEnemyDamageable(
            LevelLoader loader,
            GameObject enemyObject,
            IEnemyController enemyController)
        {
            MethodInfo methodInfo = typeof(LevelLoader).GetMethod(
                "EnsureEnemyDamageable",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(GameObject), typeof(IEnemyController) },
                modifiers: null);

            Assert.That(methodInfo, Is.Not.Null, "Expected EnsureEnemyDamageable overload to exist.");
            methodInfo.Invoke(loader, new object[] { enemyObject, enemyController });
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
            fieldInfo.SetValue(instance, value);
        }
    }
}
