using NUnit.Framework;
using UnityEngine;
using Weapons.Providers;
using Weapons.Runtime;

namespace Weapons.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="WeaponImageProvider"/>.
    /// </summary>
    public sealed class WeaponImageProviderTests
    {
        [Test]
        public void TryResolveImagePath_WhenImageNameProvided_ResolvesFromDefaultFolder()
        {
            WeaponImageProvider provider = new WeaponImageProvider();
            WeaponConfigDefinition definition = CreateDefinition("Pistol");

            bool resolved = provider.TryResolveImagePath(definition, out string imagePath);

            Assert.That(resolved, Is.True);
            Assert.That(imagePath, Is.EqualTo("Guns/Pistol"));
        }

        [Test]
        public void TryResolveImagePath_WhenImageNameContainsRelativePath_UsesPathAsIs()
        {
            WeaponImageProvider provider = new WeaponImageProvider();
            WeaponConfigDefinition definition = CreateDefinition("Icons/Weapons/Shotgun.png");

            bool resolved = provider.TryResolveImagePath(definition, out string imagePath);

            Assert.That(resolved, Is.True);
            Assert.That(imagePath, Is.EqualTo("Icons/Weapons/Shotgun"));
        }

        [Test]
        public void GetWeaponImage_WhenAssetExists_ReturnsSprite()
        {
            WeaponImageProvider provider = new WeaponImageProvider();
            WeaponConfigDefinition definition = CreateDefinition("Pistol");

            Sprite weaponSprite = provider.GetWeaponImage(definition);

            Assert.That(weaponSprite, Is.Not.Null);
        }

        [Test]
        public void GetWeaponImage_WhenImageNameMissing_ReturnsNull()
        {
            WeaponImageProvider provider = new WeaponImageProvider();
            WeaponConfigDefinition definition = CreateDefinition(string.Empty);

            Sprite weaponSprite = provider.GetWeaponImage(definition);

            Assert.That(weaponSprite, Is.Null);
        }

        private static WeaponConfigDefinition CreateDefinition(string imageName)
        {
            return new WeaponConfigDefinition(
                weaponId: "pistol",
                displayName: "Pistol",
                weaponType: WeaponType.Pistol,
                damage: 8,
                magazineSize: 12,
                fireRateSeconds: 0.35f,
                reloadTimeSeconds: 1.2f,
                range: 8.5f,
                pelletCount: 1,
                spreadAngleDegrees: 0f,
                weaponImageName: imageName);
        }
    }
}
