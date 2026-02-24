using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Weapons;
using Weapons.Runtime;

namespace Weapons.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="PlayerWeaponController"/> runtime orchestration.
    /// </summary>
    public sealed class PlayerWeaponControllerTests
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
        public void InitializeWeaponRuntime_EquipsSelectedWeapon()
        {
            PrepareSelection("machinegun", magazineSize: 30);
            PlayerWeaponController controller = CreateController();

            int ammoEventCurrent = -1;
            int ammoEventMax = -1;
            controller.OnAmmoChanged += (current, max) =>
            {
                ammoEventCurrent = current;
                ammoEventMax = max;
            };

            controller.InitializeWeaponRuntime();

            Assert.That(controller.IsInitialized, Is.True);
            Assert.That(controller.CurrentWeaponId, Is.EqualTo("machinegun"));
            Assert.That(controller.CurrentAmmo, Is.EqualTo(30));
            Assert.That(ammoEventCurrent, Is.EqualTo(30));
            Assert.That(ammoEventMax, Is.EqualTo(30));
        }

        [Test]
        public void TryShoot_WhenMagazineEmpty_AutoReloadAllowsFutureShot()
        {
            PrepareSelection("pistol", magazineSize: 1);
            PlayerWeaponController controller = CreateController();
            controller.InitializeWeaponRuntime();

            bool sawReloading = false;
            controller.OnReloadProgressChanged += (isReloading, _) =>
            {
                if (isReloading)
                {
                    sawReloading = true;
                }
            };

            bool firstShot = controller.TryShoot(0f);
            bool blockedShot = controller.TryShoot(1f);
            bool shotAfterReload = controller.TryShoot(2.5f);

            Assert.That(firstShot, Is.True);
            Assert.That(blockedShot, Is.False);
            Assert.That(sawReloading, Is.True);
            Assert.That(shotAfterReload, Is.True);
        }

        [Test]
        public void ResetForLevelStart_RefillsMagazine()
        {
            PrepareSelection("pistol", magazineSize: 5);
            PlayerWeaponController controller = CreateController();
            controller.InitializeWeaponRuntime();

            controller.TryShoot(0f);
            controller.TryShoot(1f);
            Assert.That(controller.CurrentAmmo, Is.EqualTo(3));

            controller.ResetForLevelStart();

            Assert.That(controller.CurrentAmmo, Is.EqualTo(5));
        }

        private PlayerWeaponController CreateController()
        {
            GameObject playerObject = new GameObject("PlayerWeaponControllerTest");
            m_CreatedObjects.Add(playerObject);
            return playerObject.AddComponent<PlayerWeaponController>();
        }

        private static void PrepareSelection(string selectedWeaponId, int magazineSize)
        {
            WeaponConfigDefinition pistol = new WeaponConfigDefinition(
                weaponId: "pistol",
                displayName: "Pistol",
                weaponType: WeaponType.Pistol,
                damage: 8,
                magazineSize: magazineSize,
                fireRateSeconds: 0.3f,
                reloadTimeSeconds: 1.2f,
                range: 8f,
                pelletCount: 1,
                spreadAngleDegrees: 0f);

            WeaponConfigDefinition machinegun = new WeaponConfigDefinition(
                weaponId: "machinegun",
                displayName: "Machinegun",
                weaponType: WeaponType.Machinegun,
                damage: 3,
                magazineSize: magazineSize,
                fireRateSeconds: 0.1f,
                reloadTimeSeconds: 1.4f,
                range: 9f,
                pelletCount: 1,
                spreadAngleDegrees: 2f);

            WeaponConfigCatalog catalog = new WeaponConfigCatalog(
                defaultWeaponId: "pistol",
                weapons: new[] { pistol, machinegun });

            WeaponSelectionSession.SetCatalog(catalog);
            WeaponSelectionSession.TrySelectWeapon(selectedWeaponId);
        }
    }
}
