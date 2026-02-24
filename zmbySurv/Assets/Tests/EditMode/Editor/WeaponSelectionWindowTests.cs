using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Weapons.Providers;
using Weapons.Runtime;
using Weapons.UI;

namespace Weapons.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="WeaponSelectionWindow"/> flow behavior.
    /// </summary>
    public sealed class WeaponSelectionWindowTests
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
        public void BeginSelection_WhenUiBindingsMissing_AutoSelectsDefault()
        {
            WeaponSelectionWindow window = CreateSelectionWindow();

            bool confirmed = false;
            string error = null;

            window.BeginSelection(() => confirmed = true, failure => error = failure);

            Assert.That(confirmed, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(WeaponSelectionSession.HasSelection, Is.True);
            Assert.That(WeaponSelectionSession.SelectedWeaponId, Is.EqualTo("pistol"));
        }

        [Test]
        public void BeginSelection_WhenCatalogPathInvalid_InvokesFailureCallback()
        {
            WeaponSelectionWindow window = CreateSelectionWindow();
            SetPrivateField(window, "m_ResourcesPath", "Weapons/UnknownCatalog");

            bool confirmed = false;
            string error = null;

            LogAssert.Expect(LogType.Error, "[Weapons] SelectionLoadFailed | error=[WeaponConfigProvider] LoadFailed | reason=missing_resource path=Weapons/UnknownCatalog");
            window.BeginSelection(() => confirmed = true, failure => error = failure);

            Assert.That(confirmed, Is.False);
            Assert.That(error, Is.Not.Null.And.Contains("missing_resource"));
        }

        [Test]
        public void BeginSelection_WhenWindowInactive_ActivatesGameObject()
        {
            WeaponSelectionWindow window = CreateSelectionWindow();
            window.gameObject.SetActive(false);

            bool confirmed = false;
            string error = null;

            window.BeginSelection(() => confirmed = true, failure => error = failure);

            Assert.That(window.gameObject.activeSelf, Is.True);
            Assert.That(confirmed, Is.True);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void BeginSelection_WhenWeaponImageBound_AssignsSpriteForCurrentWeapon()
        {
            WeaponSelectionWindow window = CreateSelectionWindow();
            Sprite expectedSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));
            m_CreatedAssets.Add(expectedSprite);

            GameObject selectionRoot = new GameObject("SelectionRoot");
            m_CreatedObjects.Add(selectionRoot);

            GameObject nameTextObject = new GameObject("WeaponNameText");
            m_CreatedObjects.Add(nameTextObject);
            Text weaponNameText = nameTextObject.AddComponent<Text>();

            GameObject statsTextObject = new GameObject("WeaponStatsText");
            m_CreatedObjects.Add(statsTextObject);
            Text weaponStatsText = statsTextObject.AddComponent<Text>();

            GameObject confirmButtonObject = new GameObject("ConfirmButton");
            m_CreatedObjects.Add(confirmButtonObject);
            Button confirmButton = confirmButtonObject.AddComponent<Button>();

            GameObject weaponImageObject = new GameObject("WeaponImage");
            m_CreatedObjects.Add(weaponImageObject);
            Image weaponImage = weaponImageObject.AddComponent<Image>();

            SetPrivateField(window, "m_SelectionRoot", selectionRoot);
            SetPrivateField(window, "m_WeaponNameText", weaponNameText);
            SetPrivateField(window, "m_WeaponStatsText", weaponStatsText);
            SetPrivateField(window, "m_ConfirmButton", confirmButton);
            SetPrivateField(window, "m_WeaponImage", weaponImage);
            SetPrivateField(window, "m_WeaponImageProvider", new StubWeaponImageProvider(expectedSprite));

            window.BeginSelection(() => { }, _ => { });

            Assert.That(weaponImage.sprite, Is.EqualTo(expectedSprite));
        }

        private WeaponSelectionWindow CreateSelectionWindow()
        {
            GameObject windowObject = new GameObject("WeaponSelectionWindowTest");
            m_CreatedObjects.Add(windowObject);
            return windowObject.AddComponent<WeaponSelectionWindow>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Missing private field '{fieldName}'.");
            fieldInfo.SetValue(target, value);
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
