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
            if (m_SelectionRoot != null)
            {
                m_SelectionRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
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
            m_OnSelectionConfirmed = onConfirmed;
            m_OnSelectionFailed = onFailed;

            m_CatalogService ??= new WeaponCatalogService(new Providers.ResourcesWeaponConfigProvider(m_ResourcesPath));

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

            if (!IsWindowInteractive())
            {
                AutoSelectDefaultAndContinue();
                return;
            }

            m_IsSelecting = true;
            m_CurrentIndex = ResolveInitialIndex();
            m_SelectionRoot.SetActive(true);
            RefreshView();

            Debug.Log($"[Weapons] SelectionOpened | options={m_Catalog.Weapons.Count}");
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
                return;
            }

            m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, m_Catalog.Weapons.Count - 1);
            WeaponConfigDefinition currentWeapon = m_Catalog.Weapons[m_CurrentIndex];

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
        }

        private void HandleNextClicked()
        {
            if (!m_IsSelecting || m_Catalog == null || m_Catalog.Weapons.Count == 0)
            {
                return;
            }

            m_CurrentIndex = (m_CurrentIndex + 1) % m_Catalog.Weapons.Count;
            RefreshView();
        }

        private void HandleConfirmClicked()
        {
            if (!m_IsSelecting || m_Catalog == null || m_Catalog.Weapons.Count == 0)
            {
                return;
            }

            WeaponConfigDefinition selectedWeapon = m_Catalog.Weapons[m_CurrentIndex];
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

            Debug.Log($"[Weapons] SelectionConfirmed | weaponId={selectedWeapon.WeaponId}");
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

            Debug.Log($"[Weapons] SelectionAutoConfirmed | weaponId={m_Catalog.DefaultWeaponId}");
            CompleteSelection();
        }

        private void CompleteSelection()
        {
            m_IsSelecting = false;

            if (m_SelectionRoot != null)
            {
                m_SelectionRoot.SetActive(false);
            }

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

            Action<string> failureCallback = m_OnSelectionFailed;
            m_OnSelectionConfirmed = null;
            m_OnSelectionFailed = null;
            failureCallback?.Invoke(error);
        }

        #endregion
    }
}
