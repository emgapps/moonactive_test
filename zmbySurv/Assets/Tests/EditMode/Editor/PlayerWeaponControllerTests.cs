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
            PrepareSelection("machinegun", pistolMagazineSize: 12, machinegunMagazineSize: 30);
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
            PrepareSelection("pistol", pistolMagazineSize: 1, machinegunMagazineSize: 30);
            PlayerWeaponController controller = CreateController();
            controller.InitializeWeaponRuntime();

            int reloadSignals = 0;
            controller.OnReloadProgressChanged += (isReloading, _) =>
            {
                if (isReloading)
                {
                    reloadSignals += 1;
                }
            };

            bool firstShot = controller.TryShoot(0f);
            int reloadSignalsAfterFirstShot = reloadSignals;
            bool manualReloadImmediatelyAfterLastShot = controller.TryReload(0.01f);
            bool blockedShot = controller.TryShoot(1f);
            bool shotAfterReload = controller.TryShoot(2.5f);

            Assert.That(firstShot, Is.True);
            Assert.That(reloadSignalsAfterFirstShot, Is.GreaterThan(0));
            Assert.That(manualReloadImmediatelyAfterLastShot, Is.False);
            Assert.That(blockedShot, Is.False);
            Assert.That(shotAfterReload, Is.True);
        }

        [Test]
        public void ResetForLevelStart_RefillsMagazine()
        {
            PrepareSelection("pistol", pistolMagazineSize: 5, machinegunMagazineSize: 30);
            PlayerWeaponController controller = CreateController();
            controller.InitializeWeaponRuntime();

            controller.TryShoot(0f);
            controller.TryShoot(1f);
            Assert.That(controller.CurrentAmmo, Is.EqualTo(3));

            controller.ResetForLevelStart();

            Assert.That(controller.CurrentAmmo, Is.EqualTo(5));
        }

        [Test]
        public void ResetForLevelStart_WhenSelectionChanged_SwitchesEquippedWeapon()
        {
            PrepareSelection("pistol", pistolMagazineSize: 5, machinegunMagazineSize: 30);
            PlayerWeaponController controller = CreateController();
            controller.InitializeWeaponRuntime();

            Assert.That(controller.CurrentWeaponId, Is.EqualTo("pistol"));
            Assert.That(controller.MagazineSize, Is.EqualTo(5));

            bool selectionChanged = WeaponSelectionSession.TrySelectWeapon("machinegun");
            Assert.That(selectionChanged, Is.True);

            controller.ResetForLevelStart();

            Assert.That(controller.CurrentWeaponId, Is.EqualTo("machinegun"));
            Assert.That(controller.MagazineSize, Is.EqualTo(30));
            Assert.That(controller.CurrentAmmo, Is.EqualTo(30));
        }

        private PlayerWeaponController CreateController()
        {
            GameObject playerObject = new GameObject("PlayerWeaponControllerTest");
            m_CreatedObjects.Add(playerObject);
            return playerObject.AddComponent<PlayerWeaponController>();
        }

        private static void PrepareSelection(string selectedWeaponId, int pistolMagazineSize, int machinegunMagazineSize)
        {
            WeaponConfigDefinition pistol = new WeaponConfigDefinition(
                weaponId: "pistol",
                displayName: "Pistol",
                weaponType: WeaponType.Pistol,
                damage: 8,
                magazineSize: pistolMagazineSize,
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
                magazineSize: machinegunMagazineSize,
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
