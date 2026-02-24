using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Weapons;
using Weapons.Combat;
using Weapons.Runtime;
using Weapons.UI;

namespace Weapons.Tests.PlayMode
{
    /// <summary>
    /// Play mode integration test for selection, combat, and transition reset flow.
    /// </summary>
    public sealed class WeaponsFlowIntegrationTests
    {
        private readonly List<GameObject> m_CreatedObjects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int index = 0; index < m_CreatedObjects.Count; index++)
            {
                if (m_CreatedObjects[index] != null)
                {
                    Object.Destroy(m_CreatedObjects[index]);
                }
            }

            m_CreatedObjects.Clear();
            WeaponSelectionSession.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Selection_Combat_AndLevelResetFlow_WorksEndToEnd()
        {
            WeaponSelectionWindow window = CreateSelectionWindow();

            bool selectionConfirmed = false;
            string selectionError = null;
            window.BeginSelection(() => selectionConfirmed = true, error => selectionError = error);

            Button nextButton = GetPrivateField<Button>(window, "m_NextButton");
            Button confirmButton = GetPrivateField<Button>(window, "m_ConfirmButton");
            nextButton.onClick.Invoke();
            nextButton.onClick.Invoke();
            confirmButton.onClick.Invoke();

            Assert.That(selectionError, Is.Null);
            Assert.That(selectionConfirmed, Is.True);
            Assert.That(WeaponSelectionSession.SelectedWeaponId, Is.EqualTo("machinegun"));

            PlayerWeaponController controller = CreatePlayerController(Vector3.zero);
            EnemyDamageable enemy = CreateEnemy(new Vector3(4f, 0f, 0f), health: 15);

            controller.InitializeWeaponRuntime();
            bool shotFired = controller.TryShoot(0f);
            int ammoAfterShot = controller.CurrentAmmo;

            Assert.That(shotFired, Is.True);
            Assert.That(ammoAfterShot, Is.LessThan(controller.MagazineSize));
            Assert.That(enemy.CurrentHealth, Is.LessThan(15));
            controller.ResetForLevelStart();

            Assert.That(controller.CurrentAmmo, Is.EqualTo(controller.MagazineSize));
            Assert.That(controller.CurrentAmmo, Is.GreaterThan(ammoAfterShot));

            yield return null;
        }

        private WeaponSelectionWindow CreateSelectionWindow()
        {
            GameObject canvasObject = new GameObject("SelectionCanvas");
            canvasObject.AddComponent<Canvas>();
            m_CreatedObjects.Add(canvasObject);

            GameObject windowObject = new GameObject("WeaponSelectionWindow");
            windowObject.transform.SetParent(canvasObject.transform, false);
            windowObject.SetActive(false);
            m_CreatedObjects.Add(windowObject);

            GameObject root = new GameObject("SelectionRoot");
            root.transform.SetParent(windowObject.transform, false);
            m_CreatedObjects.Add(root);

            Text weaponName = CreateText("WeaponName", root.transform);
            Text weaponStats = CreateText("WeaponStats", root.transform);
            Text pageText = CreateText("PageText", root.transform);
            Text errorText = CreateText("ErrorText", root.transform);

            Button previousButton = CreateButton("PreviousButton", root.transform);
            Button nextButton = CreateButton("NextButton", root.transform);
            Button confirmButton = CreateButton("ConfirmButton", root.transform);

            WeaponSelectionWindow window = windowObject.AddComponent<WeaponSelectionWindow>();
            SetPrivateField(window, "m_SelectionRoot", root);
            SetPrivateField(window, "m_WeaponNameText", weaponName);
            SetPrivateField(window, "m_WeaponStatsText", weaponStats);
            SetPrivateField(window, "m_PageText", pageText);
            SetPrivateField(window, "m_ErrorText", errorText);
            SetPrivateField(window, "m_PreviousButton", previousButton);
            SetPrivateField(window, "m_NextButton", nextButton);
            SetPrivateField(window, "m_ConfirmButton", confirmButton);
            windowObject.SetActive(true);

            return window;
        }

        private PlayerWeaponController CreatePlayerController(Vector3 position)
        {
            GameObject playerObject = new GameObject("WeaponsFlowPlayer");
            m_CreatedObjects.Add(playerObject);
            playerObject.transform.position = position;
            return playerObject.AddComponent<PlayerWeaponController>();
        }

        private EnemyDamageable CreateEnemy(Vector3 position, int health)
        {
            GameObject enemyObject = new GameObject("WeaponsFlowEnemy");
            m_CreatedObjects.Add(enemyObject);
            enemyObject.transform.position = position;
            enemyObject.layer = LayerMask.NameToLayer("Default");
            enemyObject.AddComponent<BoxCollider2D>();

            EnemyDamageable damageable = enemyObject.AddComponent<EnemyDamageable>();
            damageable.ConfigureHealth(health);
            return damageable;
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

        private Button CreateButton(string name, Transform parent)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            m_CreatedObjects.Add(buttonObject);
            buttonObject.AddComponent<RectTransform>();
            buttonObject.AddComponent<CanvasRenderer>();
            buttonObject.AddComponent<Image>();
            return buttonObject.AddComponent<Button>();
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Missing private field '{fieldName}'.");
            return (T)fieldInfo.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Missing private field '{fieldName}'.");
            fieldInfo.SetValue(target, value);
        }
    }
}
