using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Weapons;
using Weapons.Combat;
using Weapons.Runtime;

namespace Weapons.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="PlayerWeaponController"/> shot-trace visual dispatch integration.
    /// </summary>
    public sealed class PlayerWeaponControllerTraceDispatchTests
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
            WeaponSelectionSession.Clear();
        }

        [Test]
        public void TryShoot_WhenBulletSpawnerConfigured_DispatchesBulletVisual()
        {
            PrepareSelection();

            BulletSpawner bulletSpawner = CreateBulletSpawner();
            PlayerWeaponController controller = CreateControllerWithSpawner(bulletSpawner);
            controller.InitializeWeaponRuntime();

            bool shotFired = controller.TryShoot(0f);

            Assert.That(shotFired, Is.True);
            Assert.That(bulletSpawner.ActiveBulletCount, Is.EqualTo(1));
        }

        private BulletSpawner CreateBulletSpawner()
        {
            GameObject spawnerObject = new GameObject("BulletSpawner");
            m_CreatedObjects.Add(spawnerObject);
            BulletSpawner bulletSpawner = spawnerObject.AddComponent<BulletSpawner>();

            GameObject bulletPrefabObject = new GameObject("BulletPrefab");
            m_CreatedObjects.Add(bulletPrefabObject);
            Bullet bulletPrefab = bulletPrefabObject.AddComponent<Bullet>();

            SetPrivateField(bulletSpawner, "m_BulletPrefab", bulletPrefab);
            SetPrivateField(bulletSpawner, "m_InitialPoolCapacity", 1);
            return bulletSpawner;
        }

        private PlayerWeaponController CreateControllerWithSpawner(BulletSpawner bulletSpawner)
        {
            GameObject controllerObject = new GameObject("PlayerWeaponControllerTraceDispatchTest");
            m_CreatedObjects.Add(controllerObject);

            if (bulletSpawner != null)
            {
                bulletSpawner.transform.SetParent(controllerObject.transform, false);
            }

            PlayerWeaponController controller = controllerObject.AddComponent<PlayerWeaponController>();
            if (bulletSpawner != null)
            {
                SetPrivateField(controller, "m_BulletSpawner", bulletSpawner);
            }

            return controller;
        }

        private static void PrepareSelection()
        {
            WeaponConfigDefinition pistol = new WeaponConfigDefinition(
                weaponId: "pistol",
                displayName: "Pistol",
                weaponType: WeaponType.Pistol,
                damage: 8,
                magazineSize: 12,
                fireRateSeconds: 0.3f,
                reloadTimeSeconds: 1.2f,
                range: 8f,
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
