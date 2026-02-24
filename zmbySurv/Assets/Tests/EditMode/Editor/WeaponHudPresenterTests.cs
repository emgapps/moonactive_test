using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Weapons.Runtime;
using Weapons.UI;

namespace Weapons.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="WeaponHudPresenter"/> UI update behavior.
    /// </summary>
    public sealed class WeaponHudPresenterTests
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
        }

        [Test]
        public void HandleAmmoChanged_UpdatesAmmoText()
        {
            WeaponHudPresenter presenter = CreatePresenter(out Text _, out Text ammoText, out _, out _, out _, out _);

            InvokePrivateMethod(presenter, "HandleAmmoChanged", 3, 12);

            Assert.That(ammoText.text, Is.EqualTo("3/12"));
        }

        [Test]
        public void HandleReloadProgressChanged_TogglesAndUpdatesIndicators()
        {
            WeaponHudPresenter presenter = CreatePresenter(
                out Text _,
                out Text _,
                out GameObject reloadRoot,
                out Image reloadFillImage,
                out Slider reloadSlider,
                out Text reloadText);

            InvokePrivateMethod(presenter, "HandleReloadProgressChanged", true, 0.5f);

            Assert.That(reloadRoot.activeSelf, Is.True);
            Assert.That(reloadFillImage.fillAmount, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(reloadSlider.value, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(reloadText.text, Does.Contain("50"));

            InvokePrivateMethod(presenter, "HandleReloadProgressChanged", false, 1f);

            Assert.That(reloadRoot.activeSelf, Is.False);
            Assert.That(reloadFillImage.fillAmount, Is.EqualTo(0f));
            Assert.That(reloadSlider.value, Is.EqualTo(0f));
            Assert.That(reloadText.text, Is.EqualTo(string.Empty));
        }

        [Test]
        public void HandleWeaponEquipped_UpdatesWeaponNameText()
        {
            WeaponHudPresenter presenter = CreatePresenter(out Text weaponNameText, out _, out _, out _, out _, out _);
            WeaponConfigDefinition definition = new WeaponConfigDefinition(
                weaponId: "shotgun",
                displayName: "Shotgun",
                weaponType: WeaponType.Shotgun,
                damage: 5,
                magazineSize: 6,
                fireRateSeconds: 0.9f,
                reloadTimeSeconds: 2.0f,
                range: 6f,
                pelletCount: 5,
                spreadAngleDegrees: 24f);

            InvokePrivateMethod(presenter, "HandleWeaponEquipped", definition);

            Assert.That(weaponNameText.text, Is.EqualTo("Shotgun"));
        }

        private WeaponHudPresenter CreatePresenter(
            out Text weaponNameText,
            out Text ammoText,
            out GameObject reloadRoot,
            out Image reloadFillImage,
            out Slider reloadSlider,
            out Text reloadText)
        {
            GameObject canvasObject = new GameObject("HudCanvas");
            canvasObject.AddComponent<Canvas>();
            m_CreatedObjects.Add(canvasObject);

            GameObject presenterObject = new GameObject("WeaponHudPresenter");
            presenterObject.transform.SetParent(canvasObject.transform, false);
            m_CreatedObjects.Add(presenterObject);

            weaponNameText = CreateText("WeaponName", presenterObject.transform);
            ammoText = CreateText("Ammo", presenterObject.transform);
            reloadRoot = new GameObject("ReloadRoot");
            reloadRoot.transform.SetParent(presenterObject.transform, false);
            m_CreatedObjects.Add(reloadRoot);

            GameObject fillObject = new GameObject("ReloadFill");
            fillObject.transform.SetParent(reloadRoot.transform, false);
            m_CreatedObjects.Add(fillObject);
            reloadFillImage = fillObject.AddComponent<Image>();

            GameObject sliderObject = new GameObject("ReloadSlider");
            sliderObject.transform.SetParent(reloadRoot.transform, false);
            m_CreatedObjects.Add(sliderObject);
            reloadSlider = sliderObject.AddComponent<Slider>();

            reloadText = CreateText("ReloadText", reloadRoot.transform);

            WeaponHudPresenter presenter = presenterObject.AddComponent<WeaponHudPresenter>();
            SetPrivateField(presenter, "m_WeaponNameText", weaponNameText);
            SetPrivateField(presenter, "m_AmmoText", ammoText);
            SetPrivateField(presenter, "m_ReloadIndicatorRoot", reloadRoot);
            SetPrivateField(presenter, "m_ReloadFillImage", reloadFillImage);
            SetPrivateField(presenter, "m_ReloadSlider", reloadSlider);
            SetPrivateField(presenter, "m_ReloadText", reloadText);
            SetPrivateField(presenter, "m_AutoBindPlayerWeaponController", false);

            return presenter;
        }

        private Text CreateText(string name, Transform parent)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            m_CreatedObjects.Add(textObject);

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Missing private field '{fieldName}'.");
            fieldInfo.SetValue(target, value);
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] parameters)
        {
            MethodInfo methodInfo = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(methodInfo, Is.Not.Null, $"Missing private method '{methodName}'.");
            methodInfo.Invoke(target, parameters);
        }
    }
}
