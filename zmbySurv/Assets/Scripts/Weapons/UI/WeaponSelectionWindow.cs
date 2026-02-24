using System;
using UnityEngine;
using UnityEngine.UI;
using Weapons.Runtime;

namespace Weapons.UI
{
    /// <summary>
    /// Manages pre-level weapon selection UI and confirmation flow.
    /// </summary>
    public sealed class WeaponSelectionWindow : MonoBehaviour
    {
        private const string DefaultResourcesPath = "Weapons/Weapons";

        #region Serialized Fields

        [Header("Catalog")]
        [SerializeField]
        private string m_ResourcesPath = DefaultResourcesPath;

        [Header("Window")]
        [SerializeField]
        private GameObject m_SelectionRoot;
        [SerializeField]
        private Text m_WeaponNameText;
        [SerializeField]
        private Text m_WeaponStatsText;
        [SerializeField]
        private Text m_PageText;
        [SerializeField]
        private Text m_ErrorText;

        [Header("Controls")]
        [SerializeField]
        private Button m_PreviousButton;
        [SerializeField]
        private Button m_NextButton;
        [SerializeField]
        private Button m_ConfirmButton;

        #endregion

        #region Private Fields

        private WeaponCatalogService m_CatalogService;
        private WeaponConfigCatalog m_Catalog;
        private int m_CurrentIndex;
        private bool m_IsSelecting;

        private Action m_OnSelectionConfirmed;
        private Action<string> m_OnSelectionFailed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            LogVerbose(
                $"[Weapons] SelectionWindowAwake | object={name} selfActive={gameObject.activeSelf} hierarchyActive={gameObject.activeInHierarchy} hasRoot={(m_SelectionRoot != null)}");

            if (m_SelectionRoot != null)
            {
                m_SelectionRoot.SetActive(false);
                LogVerbose($"[Weapons] SelectionRootHidden | root={m_SelectionRoot.name}");
            }
        }

        private void OnEnable()
        {
            LogVerbose(
                $"[Weapons] SelectionWindowEnabled | object={name} selfActive={gameObject.activeSelf} hierarchyActive={gameObject.activeInHierarchy}");

            if (m_PreviousButton != null)
            {
                m_PreviousButton.onClick.AddListener(HandlePreviousClicked);
            }

            if (m_NextButton != null)
            {
                m_NextButton.onClick.AddListener(HandleNextClicked);
            }

            if (m_ConfirmButton != null)
            {
                m_ConfirmButton.onClick.AddListener(HandleConfirmClicked);
            }
        }

        private void OnDisable()
        {
            LogVerbose($"[Weapons] SelectionWindowDisabled | object={name}");

            if (m_PreviousButton != null)
            {
                m_PreviousButton.onClick.RemoveListener(HandlePreviousClicked);
            }

            if (m_NextButton != null)
            {
                m_NextButton.onClick.RemoveListener(HandleNextClicked);
            }

            if (m_ConfirmButton != null)
            {
                m_ConfirmButton.onClick.RemoveListener(HandleConfirmClicked);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Starts selection flow and invokes callback once selection is confirmed.
        /// </summary>
        /// <param name="onConfirmed">Callback invoked when selection is confirmed.</param>
        /// <param name="onFailed">Callback invoked when selection failed.</param>
        public void BeginSelection(Action onConfirmed, Action<string> onFailed)
        {
            LogVerbose(
                $"[Weapons] BeginSelectionRequested | object={name} selfActive={gameObject.activeSelf} hierarchyActive={gameObject.activeInHierarchy} resourcesPath={m_ResourcesPath}");

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                LogVerbose($"[Weapons] BeginSelectionActivatedWindow | object={name}");
            }

            m_OnSelectionConfirmed = onConfirmed;
            m_OnSelectionFailed = onFailed;

            m_CatalogService ??= new WeaponCatalogService(new Providers.ResourcesWeaponConfigProvider(m_ResourcesPath));
            LogVerbose("[Weapons] BeginSelectionLoadingCatalog");

            m_CatalogService.LoadCatalog(
                onSuccess: OnCatalogLoaded,
                onError: error =>
                {
                    Debug.LogError($"[Weapons] SelectionLoadFailed | error={error}");
                    NotifySelectionFailed(error);
                });
        }

        #endregion

        #region Private Helpers

        private void OnCatalogLoaded(WeaponConfigCatalog catalog)
        {
            if (catalog == null || catalog.Weapons == null || catalog.Weapons.Count == 0)
            {
                NotifySelectionFailed("[Weapons] SelectionLoadFailed | reason=empty_catalog");
                return;
            }

            WeaponSelectionSession.SetCatalog(catalog);
            m_Catalog = catalog;
            LogVerbose(
                $"[Weapons] CatalogLoaded | weapons={m_Catalog.Weapons.Count} defaultWeaponId={m_Catalog.DefaultWeaponId}");

            if (!IsWindowInteractive())
            {
                Debug.LogWarning(
                    $"[Weapons] SelectionWindowNotInteractive | object={name} hasRoot={(m_SelectionRoot != null)} hasNameText={(m_WeaponNameText != null)} hasStatsText={(m_WeaponStatsText != null)} hasConfirmButton={(m_ConfirmButton != null)}");
                AutoSelectDefaultAndContinue();
                return;
            }

            m_IsSelecting = true;
            m_CurrentIndex = ResolveInitialIndex();
            m_SelectionRoot.SetActive(true);
            RefreshView();
        }

        private bool IsWindowInteractive()
        {
            return m_SelectionRoot != null
                && m_WeaponNameText != null
                && m_WeaponStatsText != null
                && m_ConfirmButton != null;
        }

        private int ResolveInitialIndex()
        {
            if (m_Catalog == null || m_Catalog.Weapons == null)
            {
                return 0;
            }

            string selectedWeaponId = WeaponSelectionSession.SelectedWeaponId;
            for (int index = 0; index < m_Catalog.Weapons.Count; index++)
            {
                if (string.Equals(m_Catalog.Weapons[index].WeaponId, selectedWeaponId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return 0;
        }

        private void RefreshView()
        {
            if (!m_IsSelecting || m_Catalog == null || m_Catalog.Weapons.Count == 0)
            {
                LogVerbose(
                    $"[Weapons] RefreshSkipped | isSelecting={m_IsSelecting} hasCatalog={(m_Catalog != null)} weaponCount={(m_Catalog != null && m_Catalog.Weapons != null ? m_Catalog.Weapons.Count : 0)}");
                return;
            }

            m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, m_Catalog.Weapons.Count - 1);
            WeaponConfigDefinition currentWeapon = m_Catalog.Weapons[m_CurrentIndex];
            LogVerbose(
                $"[Weapons] RefreshView | index={m_CurrentIndex} weaponId={currentWeapon.WeaponId} displayName={currentWeapon.DisplayName}");

            m_WeaponNameText.text = currentWeapon.DisplayName;
            m_WeaponStatsText.text =
                $"Type: {currentWeapon.WeaponType}\nDamage: {currentWeapon.Damage}\nMagazine: {currentWeapon.MagazineSize}\n" +
                $"Fire Rate: {currentWeapon.FireRateSeconds:0.00}s\nReload: {currentWeapon.ReloadTimeSeconds:0.00}s\n" +
                $"Range: {currentWeapon.Range:0.0}";

            if (m_PageText != null)
            {
                m_PageText.text = $"{m_CurrentIndex + 1}/{m_Catalog.Weapons.Count}";
            }

            if (m_ErrorText != null)
            {
                m_ErrorText.text = string.Empty;
            }
        }

        private void HandlePreviousClicked()
        {
            if (!m_IsSelecting || m_Catalog == null || m_Catalog.Weapons.Count == 0)
            {
                return;
            }

            m_CurrentIndex = (m_CurrentIndex - 1 + m_Catalog.Weapons.Count) % m_Catalog.Weapons.Count;
            RefreshView();
            LogVerbose($"[Weapons] SelectionPrevious | newIndex={m_CurrentIndex}");
        }

        private void HandleNextClicked()
        {
            if (!m_IsSelecting || m_Catalog == null || m_Catalog.Weapons.Count == 0)
            {
                return;
            }

            m_CurrentIndex = (m_CurrentIndex + 1) % m_Catalog.Weapons.Count;
            RefreshView();
            LogVerbose($"[Weapons] SelectionNext | newIndex={m_CurrentIndex}");
        }

        private void HandleConfirmClicked()
        {
            if (!m_IsSelecting || m_Catalog == null || m_Catalog.Weapons.Count == 0)
            {
                return;
            }

            WeaponConfigDefinition selectedWeapon = m_Catalog.Weapons[m_CurrentIndex];
            LogVerbose(
                $"[Weapons] ConfirmClicked | index={m_CurrentIndex} weaponId={selectedWeapon.WeaponId} displayName={selectedWeapon.DisplayName}");
            bool selected = WeaponSelectionSession.TrySelectWeapon(selectedWeapon.WeaponId);
            if (!selected)
            {
                string error = $"[Weapons] SelectionFailed | reason=invalid_weapon weaponId={selectedWeapon.WeaponId}";
                if (m_ErrorText != null)
                {
                    m_ErrorText.text = "Selection failed. Please choose again.";
                }

                NotifySelectionFailed(error);
                return;
            }

            CompleteSelection();
        }

        private void AutoSelectDefaultAndContinue()
        {
            if (m_Catalog == null)
            {
                NotifySelectionFailed("[Weapons] SelectionFailed | reason=missing_catalog");
                return;
            }

            if (!WeaponSelectionSession.TrySelectWeapon(m_Catalog.DefaultWeaponId))
            {
                NotifySelectionFailed($"[Weapons] SelectionFailed | reason=invalid_default weaponId={m_Catalog.DefaultWeaponId}");
                return;
            }

            CompleteSelection();
        }

        private void CompleteSelection()
        {
            m_IsSelecting = false;

            if (m_SelectionRoot != null)
            {
                m_SelectionRoot.SetActive(false);
            }

            LogVerbose(
                $"[Weapons] SelectionComplete | selectedWeaponId={WeaponSelectionSession.SelectedWeaponId} callbackAssigned={(m_OnSelectionConfirmed != null)}");

            Action completionCallback = m_OnSelectionConfirmed;
            m_OnSelectionConfirmed = null;
            m_OnSelectionFailed = null;
            completionCallback?.Invoke();
        }

        private void NotifySelectionFailed(string error)
        {
            m_IsSelecting = false;

            if (m_SelectionRoot != null)
            {
                m_SelectionRoot.SetActive(false);
            }

            Debug.LogWarning(
                $"[Weapons] SelectionFailed | error={error} callbackAssigned={(m_OnSelectionFailed != null)}");

            Action<string> failureCallback = m_OnSelectionFailed;
            m_OnSelectionConfirmed = null;
            m_OnSelectionFailed = null;
            failureCallback?.Invoke(error);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void LogVerbose(string message)
        {
            // Intentionally no-op: verbose trace logs were removed to keep only important logs.
        }

        #endregion
    }
}
