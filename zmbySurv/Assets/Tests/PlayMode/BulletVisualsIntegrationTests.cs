using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Weapons;
using Weapons.Combat;
using Weapons.Runtime;

namespace Weapons.Tests.PlayMode
{
    /// <summary>
    /// Play mode integration tests for bullet visual despawn behavior.
    /// </summary>
    public sealed class BulletVisualsIntegrationTests
    {
        private const float BulletWaitTimeoutSeconds = 2f;
        private readonly List<GameObject> m_CreatedObjects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int index = 0; index < m_CreatedObjects.Count; index++)
            {
                if (m_CreatedObjects[index] != null)
                {
                    UnityEngine.Object.Destroy(m_CreatedObjects[index]);
                }
            }

            m_CreatedObjects.Clear();
            WeaponSelectionSession.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerWeaponController_BulletVisualDespawns_OnEnemyImpact()
        {
            PrepareSelection(damage: 4, range: 10f);
            PlayerWeaponController controller = CreatePlayerControllerWithBulletSpawner(out BulletSpawner bulletSpawner);
            EnemyDamageable enemy = CreateEnemy(position: new Vector2(3f, 0f), health: 10);

            controller.InitializeWeaponRuntime();
            bool fired = controller.TryShoot(0f);
            Bullet bullet = GetOnlyActiveBullet(bulletSpawner);

            Assert.That(fired, Is.True);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(6));

            yield return WaitForBulletDespawn(bulletSpawner, BulletWaitTimeoutSeconds);
            Assert.That(bullet.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator PlayerWeaponController_BulletVisualDespawns_OnWallImpact()
        {
            PrepareSelection(damage: 4, range: 10f);
            PlayerWeaponController controller = CreatePlayerControllerWithBulletSpawner(out BulletSpawner bulletSpawner);
            CreateWall(position: new Vector2(2f, 0f));
            EnemyDamageable enemyBehindWall = CreateEnemy(position: new Vector2(5f, 0f), health: 10);

            controller.InitializeWeaponRuntime();
            bool fired = controller.TryShoot(0f);
            Bullet bullet = GetOnlyActiveBullet(bulletSpawner);

            Assert.That(fired, Is.True);
            Assert.That(enemyBehindWall.CurrentHealth, Is.EqualTo(10));

            yield return WaitForBulletDespawn(bulletSpawner, BulletWaitTimeoutSeconds);
            Assert.That(bullet.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator PlayerWeaponController_BulletVisualDespawns_AtRangeEndWithoutHit()
        {
            const float noHitRange = 2f;
            PrepareSelection(damage: 4, range: noHitRange);
            PlayerWeaponController controller = CreatePlayerControllerWithBulletSpawner(out BulletSpawner bulletSpawner);

            controller.InitializeWeaponRuntime();
            bool fired = controller.TryShoot(0f);
            Bullet bullet = GetOnlyActiveBullet(bulletSpawner);

            Assert.That(fired, Is.True);

            yield return WaitForBulletDespawn(bulletSpawner, BulletWaitTimeoutSeconds);
            Assert.That(bullet.gameObject.activeSelf, Is.False);
            Assert.That(bullet.transform.position.x, Is.EqualTo(noHitRange).Within(0.2f));
            Assert.That(bullet.transform.position.y, Is.EqualTo(0f).Within(0.2f));
        }

        private PlayerWeaponController CreatePlayerControllerWithBulletSpawner(out BulletSpawner bulletSpawner)
        {
            GameObject playerObject = new GameObject("BulletVisualsPlayer");
            m_CreatedObjects.Add(playerObject);
            playerObject.transform.position = Vector3.zero;

            GameObject spawnerObject = new GameObject("BulletSpawner");
            m_CreatedObjects.Add(spawnerObject);
            spawnerObject.transform.SetParent(playerObject.transform, false);
            bulletSpawner = spawnerObject.AddComponent<BulletSpawner>();

            GameObject bulletPrefabObject = new GameObject("BulletPrefab");
            m_CreatedObjects.Add(bulletPrefabObject);
            Bullet bulletPrefab = bulletPrefabObject.AddComponent<Bullet>();

            SetPrivateField(bulletSpawner, "m_BulletPrefab", bulletPrefab);
            SetPrivateField(bulletSpawner, "m_BulletSpeedUnitsPerSecond", 30f);
            SetPrivateField(bulletSpawner, "m_InitialPoolCapacity", 1);

            PlayerWeaponController controller = playerObject.AddComponent<PlayerWeaponController>();
            SetPrivateField(controller, "m_BulletSpawner", bulletSpawner);
            return controller;
        }

        private EnemyDamageable CreateEnemy(Vector2 position, int health)
        {
            GameObject enemyObject = new GameObject("BulletVisualsEnemy");
            m_CreatedObjects.Add(enemyObject);
            enemyObject.transform.position = position;
            enemyObject.layer = LayerMask.NameToLayer("Default");
            enemyObject.AddComponent<BoxCollider2D>();

            EnemyDamageable damageable = enemyObject.AddComponent<EnemyDamageable>();
            damageable.ConfigureHealth(health);
            return damageable;
        }

        private void CreateWall(Vector2 position)
        {
            GameObject wallObject = new GameObject("BulletVisualsWall");
            m_CreatedObjects.Add(wallObject);
            wallObject.transform.position = position;
            wallObject.layer = LayerMask.NameToLayer("Default");
            wallObject.AddComponent<BoxCollider2D>();
        }

        private static Bullet GetOnlyActiveBullet(BulletSpawner bulletSpawner)
        {
            foreach (Bullet activeBullet in bulletSpawner.ActiveBullets)
            {
                return activeBullet;
            }

            Assert.Fail("Expected an active bullet instance.");
            return null;
        }

        private static IEnumerator WaitForBulletDespawn(BulletSpawner bulletSpawner, float timeoutSeconds)
        {
            float elapsed = 0f;
            while (elapsed < timeoutSeconds)
            {
                if (bulletSpawner.ActiveBulletCount == 0)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Fail($"Timed out waiting for bullet despawn after {timeoutSeconds:0.00}s.");
        }

        private static void PrepareSelection(int damage, float range)
        {
            WeaponConfigDefinition pistol = new WeaponConfigDefinition(
                weaponId: "pistol",
                displayName: "Pistol",
                weaponType: WeaponType.Pistol,
                damage: damage,
                magazineSize: 12,
                fireRateSeconds: 0.2f,
                reloadTimeSeconds: 1f,
                range: range,
                pelletCount: 1,
                spreadAngleDegrees: 0f);

            WeaponConfigCatalog catalog = new WeaponConfigCatalog(
                defaultWeaponId: "pistol",
                weapons: new[] { pistol });

            WeaponSelectionSession.SetCatalog(catalog);
            WeaponSelectionSession.TrySelectWeapon("pistol");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Missing private field '{fieldName}'.");
            fieldInfo.SetValue(target, value);
        }
    }
}
