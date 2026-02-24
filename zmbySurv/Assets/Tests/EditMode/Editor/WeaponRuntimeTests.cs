using NUnit.Framework;
using Weapons.Runtime;

namespace Weapons.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="WeaponRuntime"/> state transitions.
    /// </summary>
    public sealed class WeaponRuntimeTests
    {
        [TearDown]
        public void TearDown()
        {
            WeaponSelectionSession.Clear();
        }

        [Test]
        public void TryShoot_WhenReady_ConsumesAmmoAndReturnsShotRequest()
        {
            WeaponRuntime runtime = CreateRuntime(WeaponType.Pistol);

            bool shotFired = runtime.TryShoot(1f, out WeaponShotRequest shotRequest, out WeaponShootResult shootResult);

            Assert.That(shotFired, Is.True);
            Assert.That(shootResult, Is.EqualTo(WeaponShootResult.Fired));
            Assert.That(runtime.CurrentAmmo, Is.EqualTo(runtime.MagazineSize - 1));
            Assert.That(shotRequest.WeaponId, Is.EqualTo("test_pistol"));
            Assert.That(shotRequest.PelletCount, Is.EqualTo(1));
        }

        [Test]
        public void TryShoot_WhenCooldownActive_IsBlocked()
        {
            WeaponRuntime runtime = CreateRuntime(WeaponType.Machinegun);

            runtime.TryShoot(2f, out _, out _);
            bool secondShot = runtime.TryShoot(2.01f, out _, out WeaponShootResult secondResult);

            Assert.That(secondShot, Is.False);
            Assert.That(secondResult, Is.EqualTo(WeaponShootResult.BlockedCooldown));
            Assert.That(runtime.State, Is.EqualTo(WeaponRuntimeState.Cooldown));
        }

        [Test]
        public void ReloadFlow_RefillsMagazineAfterReloadDuration()
        {
            WeaponRuntime runtime = CreateRuntime(WeaponType.Shotgun);

            runtime.TryShoot(0f, out _, out _);
            runtime.TryShoot(1f, out _, out _);
            Assert.That(runtime.CurrentAmmo, Is.EqualTo(runtime.MagazineSize - 2));

            bool reloadStarted = runtime.TryStartReload(1.1f);
            runtime.Tick(1.9f);
            float inProgress = runtime.ReloadProgress01;
            runtime.Tick(3.5f);

            Assert.That(reloadStarted, Is.True);
            Assert.That(inProgress, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(runtime.CurrentAmmo, Is.EqualTo(runtime.MagazineSize));
            Assert.That(runtime.State, Is.EqualTo(WeaponRuntimeState.Ready));
        }

        [Test]
        public void TryShoot_WhenMagazineEmpty_ReturnsBlockedNoAmmo()
        {
            WeaponRuntime runtime = CreateRuntime(WeaponType.Pistol, magazineSizeOverride: 1);

            runtime.TryShoot(0f, out _, out _);
            bool secondAttempt = runtime.TryShoot(1f, out _, out WeaponShootResult result);

            Assert.That(secondAttempt, Is.False);
            Assert.That(result, Is.EqualTo(WeaponShootResult.BlockedNoAmmo));
            Assert.That(runtime.CurrentAmmo, Is.EqualTo(0));
        }

        [Test]
        public void TryShoot_WhenShotgunConfigured_UsesMultiplePellets()
        {
            WeaponRuntime runtime = CreateRuntime(WeaponType.Shotgun);

            bool shotFired = runtime.TryShoot(5f, out WeaponShotRequest shotRequest, out WeaponShootResult result);

            Assert.That(shotFired, Is.True);
            Assert.That(result, Is.EqualTo(WeaponShootResult.Fired));
            Assert.That(shotRequest.PelletCount, Is.EqualTo(6));
            Assert.That(shotRequest.SpreadAngleDegrees, Is.EqualTo(24f));
        }

        [Test]
        public void SelectionSession_WhenCatalogSet_AssignsDefaultWeapon()
        {
            WeaponConfigCatalog catalog = new WeaponConfigCatalog(
                defaultWeaponId: "machinegun",
                weapons: new[]
                {
                    CreateDefinition("pistol", WeaponType.Pistol),
                    CreateDefinition("machinegun", WeaponType.Machinegun)
                });

            WeaponSelectionSession.SetCatalog(catalog);
            bool selectedResolved = WeaponSelectionSession.TryGetSelectedWeapon(out WeaponConfigDefinition selectedWeapon);

            Assert.That(WeaponSelectionSession.HasCatalog, Is.True);
            Assert.That(WeaponSelectionSession.SelectedWeaponId, Is.EqualTo("machinegun"));
            Assert.That(selectedResolved, Is.True);
            Assert.That(selectedWeapon.WeaponType, Is.EqualTo(WeaponType.Machinegun));
        }

        private static WeaponRuntime CreateRuntime(WeaponType weaponType, int magazineSizeOverride = -1)
        {
            WeaponConfigDefinition definition = weaponType switch
            {
                WeaponType.Shotgun => new WeaponConfigDefinition(
                    weaponId: "test_shotgun",
                    displayName: "Test Shotgun",
                    weaponType: WeaponType.Shotgun,
                    damage: 6,
                    magazineSize: magazineSizeOverride > 0 ? magazineSizeOverride : 6,
                    fireRateSeconds: 0.9f,
                    reloadTimeSeconds: 2f,
                    range: 6f,
                    pelletCount: 6,
                    spreadAngleDegrees: 24f),
                WeaponType.Machinegun => new WeaponConfigDefinition(
                    weaponId: "test_machinegun",
                    displayName: "Test Machinegun",
                    weaponType: WeaponType.Machinegun,
                    damage: 2,
                    magazineSize: magazineSizeOverride > 0 ? magazineSizeOverride : 30,
                    fireRateSeconds: 0.1f,
                    reloadTimeSeconds: 1.5f,
                    range: 9f,
                    pelletCount: 1,
                    spreadAngleDegrees: 2f),
                _ => new WeaponConfigDefinition(
                    weaponId: "test_pistol",
                    displayName: "Test Pistol",
                    weaponType: WeaponType.Pistol,
                    damage: 8,
                    magazineSize: magazineSizeOverride > 0 ? magazineSizeOverride : 12,
                    fireRateSeconds: 0.35f,
                    reloadTimeSeconds: 1.1f,
                    range: 8f,
                    pelletCount: 1,
                    spreadAngleDegrees: 0f)
            };

            return new WeaponRuntime(definition);
        }

        private static WeaponConfigDefinition CreateDefinition(string weaponId, WeaponType weaponType)
        {
            return new WeaponConfigDefinition(
                weaponId: weaponId,
                displayName: weaponId,
                weaponType: weaponType,
                damage: 1,
                magazineSize: 1,
                fireRateSeconds: 1f,
                reloadTimeSeconds: 1f,
                range: 1f,
                pelletCount: 1,
                spreadAngleDegrees: 0f);
        }
    }
}
