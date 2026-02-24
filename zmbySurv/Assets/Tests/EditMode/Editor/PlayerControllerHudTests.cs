using System.Collections.Generic;
using System.Reflection;
using Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Weapons;
using Weapons.Providers;
using Weapons.Runtime;

namespace Weapons.Tests.EditMode
{
    /// <summary>
    /// Unit tests for player HUD bindings driven by weapon runtime events.
    /// </summary>
    public sealed class PlayerControllerHudTests
    {
        private readonly List<GameObject> m_CreatedObjects = new List<GameObject>();
        private readonly List<UnityEngine.Object> m_CreatedAssets = new List<UnityEngine.Object>();

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

            for (int index = 0; index < m_CreatedAssets.Count; index++)
            {
                if (m_CreatedAssets[index] != null)
                {
                    Object.DestroyImmediate(m_CreatedAssets[index]);
                }
            }

            m_CreatedAssets.Clear();
            WeaponSelectionSession.Clear();
        }

        [Test]
        public void InitializeWeaponRuntime_UpdatesBulletsText()
        {
            PrepareSelection(selectedWeaponId: "pistol", magazineSize: 6, imageName: "Pistol");

            PlayerController playerController = CreatePlayerController(out PlayerWeaponController weaponController, out Text bulletsText, out _);
            SetPrivateField(playerController, "m_WeaponImageProvider", new StubWeaponImageProvider(null));

            weaponController.InitializeWeaponRuntime();
            Assert.That(bulletsText.text, Is.EqualTo("6/6"));

            weaponController.TryShoot(0f);
            Assert.That(bulletsText.text, Is.EqualTo("5/6"));
        }

        [Test]
        public void InitializeWeaponRuntime_UsesWeaponImageProviderForHudImage()
        {
            PrepareSelection(selectedWeaponId: "pistol", magazineSize: 6, imageName: "Pistol");

            PlayerController playerController = CreatePlayerController(out PlayerWeaponController weaponController, out _, out Image weaponImage);
            Sprite expectedSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));
            m_CreatedAssets.Add(expectedSprite);

            SetPrivateField(playerController, "m_WeaponImageProvider", new StubWeaponImageProvider(expectedSprite));

            weaponController.InitializeWeaponRuntime();

            Assert.That(weaponImage.sprite, Is.EqualTo(expectedSprite));
        }

        private PlayerController CreatePlayerController(
            out PlayerWeaponController weaponController,
            out Text bulletsText,
            out Image weaponImage)
        {
            GameObject playerObject = new GameObject("PlayerControllerHudTest");
            m_CreatedObjects.Add(playerObject);

            PlayerController playerController = playerObject.AddComponent<PlayerController>();
            InvokePrivateMethod(playerController, "Awake");

            weaponController = playerObject.GetComponent<PlayerWeaponController>();
            Assert.That(weaponController, Is.Not.Null);

            GameObject bulletsTextObject = new GameObject("BulletsText");
            m_CreatedObjects.Add(bulletsTextObject);
            bulletsText = bulletsTextObject.AddComponent<Text>();

            GameObject weaponImageObject = new GameObject("WeaponImage");
            m_CreatedObjects.Add(weaponImageObject);
            weaponImage = weaponImageObject.AddComponent<Image>();

            SetPrivateField(playerController, "m_bulletsText", bulletsText);
            SetPrivateField(playerController, "m_weaponImage", weaponImage);
            InvokePrivateMethod(playerController, "SubscribeWeaponUiEvents");

            return playerController;
        }

        private static void PrepareSelection(string selectedWeaponId, int magazineSize, string imageName)
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
                spreadAngleDegrees: 0f,
                weaponImageName: imageName);

            WeaponConfigCatalog catalog = new WeaponConfigCatalog(
                defaultWeaponId: "pistol",
                weapons: new[] { pistol });

            WeaponSelectionSession.SetCatalog(catalog);
            WeaponSelectionSession.TrySelectWeapon(selectedWeaponId);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Missing private field '{fieldName}'.");
            fieldInfo.SetValue(target, value);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            MethodInfo methodInfo = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(methodInfo, Is.Not.Null, $"Missing private method '{methodName}'.");
            methodInfo.Invoke(target, null);
        }

        private sealed class StubWeaponImageProvider : IWeaponImageProvider
        {
            private readonly Sprite m_Sprite;

            public StubWeaponImageProvider(Sprite sprite)
            {
                m_Sprite = sprite;
            }

            public bool TryResolveImagePath(WeaponConfigDefinition weaponDefinition, out string imagePath)
            {
                imagePath = "ignored/path";
                return weaponDefinition != null;
            }

            public Sprite GetWeaponImage(WeaponConfigDefinition weaponDefinition)
            {
                return weaponDefinition != null ? m_Sprite : null;
            }
        }
    }
}
