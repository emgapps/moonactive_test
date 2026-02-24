using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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
    }
}
