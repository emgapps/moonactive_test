using System;
using NUnit.Framework;
using Weapons.Providers;
using Weapons.Runtime;

namespace Weapons.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="WeaponCatalogService"/> loading behavior.
    /// </summary>
    public sealed class WeaponCatalogServiceTests
    {
        [Test]
        public void LoadCatalog_WhenCalledTwice_UsesCachedResult()
        {
            FakeProvider provider = new FakeProvider();
            WeaponCatalogService service = new WeaponCatalogService(provider);

            WeaponConfigCatalog firstCatalog = null;
            WeaponConfigCatalog secondCatalog = null;

            service.LoadCatalog(catalog => firstCatalog = catalog, _ => Assert.Fail("Unexpected error."));
            service.LoadCatalog(catalog => secondCatalog = catalog, _ => Assert.Fail("Unexpected error."));

            Assert.That(provider.LoadCallCount, Is.EqualTo(1));
            Assert.That(firstCatalog, Is.Not.Null);
            Assert.That(secondCatalog, Is.SameAs(firstCatalog));
        }

        private sealed class FakeProvider : IWeaponConfigProvider
        {
            public int LoadCallCount { get; private set; }

            public void LoadWeaponCatalog(Action<WeaponConfigCatalog> onSuccess, Action<string> onError)
            {
                LoadCallCount += 1;

                WeaponConfigCatalog catalog = new WeaponConfigCatalog(
                    defaultWeaponId: "pistol",
                    weapons: new[]
                    {
                        new WeaponConfigDefinition(
                            weaponId: "pistol",
                            displayName: "Pistol",
                            weaponType: WeaponType.Pistol,
                            damage: 8,
                            magazineSize: 12,
                            fireRateSeconds: 0.35f,
                            reloadTimeSeconds: 1.2f,
                            range: 8f,
                            pelletCount: 1,
                            spreadAngleDegrees: 0f)
                    });

                onSuccess.Invoke(catalog);
            }
        }
    }
}
