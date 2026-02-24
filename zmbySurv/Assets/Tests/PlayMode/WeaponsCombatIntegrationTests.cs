using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Weapons;
using Weapons.Combat;
using Weapons.Runtime;

namespace Weapons.Tests.PlayMode
{
    /// <summary>
    /// Play mode integration tests for weapon hit resolution and enemy damage flow.
    /// </summary>
    public sealed class WeaponsCombatIntegrationTests
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
            WeaponSelectionSession.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerWeaponController_ShotDamagesEnemyInRange()
        {
            PrepareSelectionCatalog(damage: 4, range: 10f);

            PlayerWeaponController controller = CreatePlayerController(Vector3.zero);
            EnemyDamageable enemy = CreateEnemy(new Vector3(3f, 0f, 0f), health: 10);

            controller.InitializeWeaponRuntime();
            bool fired = controller.TryShoot(0f);
            yield return null;

            Assert.That(fired, Is.True);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(6));
            Assert.That(enemy.IsAlive, Is.True);
        }

        [UnityTest]
        public IEnumerator PlayerWeaponController_ShotDoesNotDamageEnemyOutOfRange()
        {
            PrepareSelectionCatalog(damage: 4, range: 3f);

            PlayerWeaponController controller = CreatePlayerController(Vector3.zero);
            EnemyDamageable enemy = CreateEnemy(new Vector3(10f, 0f, 0f), health: 10);

            controller.InitializeWeaponRuntime();
            bool fired = controller.TryShoot(0f);
            yield return null;

            Assert.That(fired, Is.True);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(10));
            Assert.That(enemy.IsAlive, Is.True);
        }

        private PlayerWeaponController CreatePlayerController(Vector3 position)
        {
            GameObject playerObject = new GameObject("WeaponsCombatPlayer");
            m_CreatedObjects.Add(playerObject);
            playerObject.transform.position = position;

            PlayerWeaponController controller = playerObject.AddComponent<PlayerWeaponController>();
            return controller;
        }

        private EnemyDamageable CreateEnemy(Vector3 position, int health)
        {
            GameObject enemyObject = new GameObject("WeaponsCombatEnemy");
            m_CreatedObjects.Add(enemyObject);
            enemyObject.transform.position = position;
            enemyObject.layer = LayerMask.NameToLayer("Default");

            enemyObject.AddComponent<BoxCollider2D>();
            EnemyDamageable damageable = enemyObject.AddComponent<EnemyDamageable>();
            damageable.ConfigureHealth(health);
            return damageable;
        }

        private static void PrepareSelectionCatalog(int damage, float range)
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
    }
}
