using NUnit.Framework;
using Weapons.Data;
using Weapons.Providers;
using Weapons.Runtime;

namespace Weapons.Tests.EditMode
{
    /// <summary>
    /// Unit tests for weapon catalog validation and resources provider mapping.
    /// </summary>
    public sealed class WeaponsDataProviderTests
    {
        [Test]
        public void TryValidateCatalog_WhenPayloadIsValid_ReturnsTrue()
        {
            WeaponCatalogDto catalog = CreateValidCatalog();

            bool isValid = WeaponDataValidation.TryValidateCatalog(catalog, out string errorMessage);

            Assert.That(isValid, Is.True);
            Assert.That(errorMessage, Is.EqualTo(string.Empty));
        }

        [Test]
        public void TryValidateCatalog_WhenWeaponIdDuplicated_ReturnsFalse()
        {
            WeaponCatalogDto catalog = CreateValidCatalog();
            catalog.weapons[1].weaponId = catalog.weapons[0].weaponId;

            bool isValid = WeaponDataValidation.TryValidateCatalog(catalog, out string errorMessage);

            Assert.That(isValid, Is.False);
            Assert.That(errorMessage, Does.Contain("duplicated"));
        }

        [Test]
        public void TryParseWeaponType_WhenValueIsUnsupported_ReturnsFalse()
        {
            bool parsed = WeaponDataValidation.TryParseWeaponType("laser", out WeaponType weaponType);

            Assert.That(parsed, Is.False);
            Assert.That(weaponType, Is.EqualTo(WeaponType.Pistol));
        }

        [Test]
        public void LoadWeaponCatalog_WhenResourceExists_ReturnsMappedDefinitions()
        {
            ResourcesWeaponConfigProvider provider = new ResourcesWeaponConfigProvider("Weapons/Weapons");

            WeaponConfigCatalog loadedCatalog = null;
            string loadError = null;

            provider.LoadWeaponCatalog(
                onSuccess: catalog => loadedCatalog = catalog,
                onError: error => loadError = error);

            Assert.That(loadError, Is.Null.Or.Empty);
            Assert.That(loadedCatalog, Is.Not.Null);
            Assert.That(loadedCatalog.Weapons.Count, Is.EqualTo(3));
            Assert.That(loadedCatalog.DefaultWeaponId, Is.EqualTo("pistol"));
            Assert.That(loadedCatalog.Weapons[1].WeaponType, Is.EqualTo(WeaponType.Shotgun));
            Assert.That(loadedCatalog.Weapons[2].MagazineSize, Is.EqualTo(30));
        }

        [Test]
        public void LoadWeaponCatalog_WhenResourceMissing_ReturnsActionableError()
        {
            ResourcesWeaponConfigProvider provider = new ResourcesWeaponConfigProvider("Weapons/MissingCatalog");

            WeaponConfigCatalog loadedCatalog = null;
            string loadError = null;

            provider.LoadWeaponCatalog(
                onSuccess: catalog => loadedCatalog = catalog,
                onError: error => loadError = error);

            Assert.That(loadedCatalog, Is.Null);
            Assert.That(loadError, Is.Not.Null.And.Contains("missing_resource"));
        }

        private static WeaponCatalogDto CreateValidCatalog()
        {
            return new WeaponCatalogDto
            {
                defaultWeaponId = "pistol",
                weapons = new System.Collections.Generic.List<WeaponConfigDto>
                {
                    new WeaponConfigDto
                    {
                        weaponId = "pistol",
                        displayName = "Pistol",
                        weaponType = "Pistol",
                        damage = 8,
                        magazineSize = 12,
                        fireRate = 0.35f,
                        reloadTime = 1.2f,
                        range = 8.5f,
                        pelletCount = 1,
                        spreadAngle = 0f
                    },
                    new WeaponConfigDto
                    {
                        weaponId = "shotgun",
                        displayName = "Shotgun",
                        weaponType = "Shotgun",
                        damage = 5,
                        magazineSize = 6,
                        fireRate = 0.9f,
                        reloadTime = 2.1f,
                        range = 6f,
                        pelletCount = 5,
                        spreadAngle = 24f
                    }
                }
            };
        }
    }
}
