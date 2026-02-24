using System;
using UnityEngine;
using Weapons.Providers;
using Weapons.Runtime;

namespace Weapons
{
    /// <summary>
    /// Handles player-side weapon input and runtime orchestration.
    /// </summary>
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        private const string DefaultResourcesPath = "Weapons/Weapons";

        #region Serialized Fields

        [Header("Input")]
        [SerializeField]
        private KeyCode m_ShootKey = KeyCode.Space;
        [SerializeField]
        private KeyCode m_ReloadKey = KeyCode.R;
        [SerializeField]
        private bool m_AutoReloadOnEmpty = true;

        [Header("Setup")]
        [SerializeField]
        private string m_ResourcesPath = DefaultResourcesPath;
        [SerializeField]
        private Transform m_MuzzleTransform;

        #endregion

        #region Private Fields

        private WeaponCatalogService m_CatalogService;
        private IWeapon m_ActiveWeapon;
        private WeaponConfigDefinition m_ActiveDefinition;
        private bool m_IsInitialized;

        #endregion

        #region Events

        /// <summary>
        /// Raised when a weapon is equipped for gameplay.
        /// </summary>
        public event Action<WeaponConfigDefinition> OnWeaponEquipped;

        /// <summary>
        /// Raised when ammo values change.
        /// </summary>
        public event Action<int, int> OnAmmoChanged;

        /// <summary>
        /// Raised when reload state/progress changes.
        /// </summary>
        public event Action<bool, float> OnReloadProgressChanged;

        /// <summary>
        /// Raised when a shot request is emitted by weapon runtime.
        /// </summary>
        public event Action<WeaponShotRequest> OnShotRequested;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets whether weapon runtime is ready for gameplay input.
        /// </summary>
        public bool IsInitialized => m_IsInitialized;

        /// <summary>
        /// Gets current weapon identifier.
        /// </summary>
        public string CurrentWeaponId => m_ActiveDefinition != null ? m_ActiveDefinition.WeaponId : string.Empty;

        /// <summary>
        /// Gets current ammo value.
        /// </summary>
        public int CurrentAmmo => m_ActiveWeapon != null ? m_ActiveWeapon.CurrentAmmo : 0;

        /// <summary>
        /// Gets current magazine size.
        /// </summary>
        public int MagazineSize => m_ActiveWeapon != null ? m_ActiveWeapon.MagazineSize : 0;

        /// <summary>
        /// Gets current muzzle world position.
        /// </summary>
        public Vector2 MuzzlePosition => m_MuzzleTransform != null ? m_MuzzleTransform.position : transform.position;

        /// <summary>
        /// Gets current muzzle forward direction.
        /// </summary>
        public Vector2 MuzzleDirection => (m_MuzzleTransform != null ? m_MuzzleTransform.right : transform.right).normalized;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            InitializeWeaponRuntime();
        }

        private void Update()
        {
            if (!m_IsInitialized || m_ActiveWeapon == null)
            {
                return;
            }

            float currentTime = Time.time;
            m_ActiveWeapon.Tick(currentTime);
            PublishReloadProgress();

            if (Input.GetKeyDown(m_ReloadKey))
            {
                TryReload(currentTime);
            }

            if (Input.GetKey(m_ShootKey))
            {
                TryShoot(currentTime);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Initializes weapon runtime from current session selection.
        /// </summary>
        public void InitializeWeaponRuntime()
        {
            if (!TryResolveSelectedWeapon(out WeaponConfigDefinition definition, out string errorMessage))
            {
                Debug.LogError($"[Weapons] InitializationFailed | error={errorMessage}");
                m_IsInitialized = false;
                return;
            }

            m_ActiveDefinition = definition;
            m_ActiveWeapon = new WeaponRuntime(definition);
            m_IsInitialized = true;

            PublishAmmoChanged();
            PublishReloadProgress();
            OnWeaponEquipped?.Invoke(definition);

            Debug.Log(
                $"[Weapons] Equipped | weaponId={definition.WeaponId} type={definition.WeaponType} damage={definition.Damage} magazine={definition.MagazineSize}");
        }

        /// <summary>
        /// Attempts to fire weapon at current frame time.
        /// </summary>
        /// <param name="currentTimeSeconds">Current time in seconds.</param>
        /// <returns>True when shot was fired; otherwise false.</returns>
        public bool TryShoot(float currentTimeSeconds)
        {
            if (!m_IsInitialized || m_ActiveWeapon == null)
            {
                return false;
            }

            bool fired = m_ActiveWeapon.TryShoot(currentTimeSeconds, out WeaponShotRequest shot, out WeaponShootResult result);
            PublishReloadProgress();

            if (!fired)
            {
                if (result == WeaponShootResult.BlockedNoAmmo && m_AutoReloadOnEmpty)
                {
                    TryReload(currentTimeSeconds);
                }

                return false;
            }

            PublishAmmoChanged();
            OnShotRequested?.Invoke(shot);

            Debug.Log(
                $"[Weapons] ShotRequested | weaponId={shot.WeaponId} ammo={m_ActiveWeapon.CurrentAmmo}/{m_ActiveWeapon.MagazineSize} pellets={shot.PelletCount} range={shot.Range:0.0}");

            return true;
        }

        /// <summary>
        /// Attempts to start manual reload at current frame time.
        /// </summary>
        /// <param name="currentTimeSeconds">Current time in seconds.</param>
        /// <returns>True when reload started; otherwise false.</returns>
        public bool TryReload(float currentTimeSeconds)
        {
            if (!m_IsInitialized || m_ActiveWeapon == null)
            {
                return false;
            }

            bool reloadStarted = m_ActiveWeapon.TryStartReload(currentTimeSeconds);
            PublishReloadProgress();

            if (reloadStarted)
            {
                Debug.Log(
                    $"[Weapons] ReloadStarted | weaponId={m_ActiveDefinition.WeaponId} duration={m_ActiveDefinition.ReloadTimeSeconds:0.00}");
            }

            return reloadStarted;
        }

        /// <summary>
        /// Resets weapon runtime for a new level start.
        /// </summary>
        public void ResetForLevelStart()
        {
            if (!m_IsInitialized || m_ActiveWeapon == null)
            {
                InitializeWeaponRuntime();
                return;
            }

            m_ActiveWeapon.ResetState();
            PublishAmmoChanged();
            PublishReloadProgress();

            Debug.Log($"[Weapons] ResetForLevelStart | weaponId={m_ActiveDefinition.WeaponId}");
        }

        #endregion

        #region Private Helpers

        private bool TryResolveSelectedWeapon(out WeaponConfigDefinition definition, out string errorMessage)
        {
            definition = null;

            if (!WeaponSelectionSession.HasCatalog)
            {
                m_CatalogService ??= new WeaponCatalogService(new ResourcesWeaponConfigProvider(m_ResourcesPath));

                bool loadSucceeded = false;
                string loadError = string.Empty;

                m_CatalogService.LoadCatalog(
                    onSuccess: catalog =>
                    {
                        WeaponSelectionSession.SetCatalog(catalog);
                        loadSucceeded = true;
                    },
                    onError: error =>
                    {
                        loadError = error;
                    });

                if (!loadSucceeded)
                {
                    errorMessage = loadError;
                    return false;
                }
            }

            if (!WeaponSelectionSession.HasSelection)
            {
                WeaponConfigCatalog catalog = WeaponSelectionSession.CurrentCatalog;
                if (catalog == null)
                {
                    errorMessage = "selection_catalog_missing";
                    return false;
                }

                if (!WeaponSelectionSession.TrySelectWeapon(catalog.DefaultWeaponId))
                {
                    errorMessage = $"invalid_default_weapon id={catalog.DefaultWeaponId}";
                    return false;
                }
            }

            if (!WeaponSelectionSession.TryGetSelectedWeapon(out definition))
            {
                errorMessage = "selected_weapon_missing";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private void PublishAmmoChanged()
        {
            if (m_ActiveWeapon == null)
            {
                return;
            }

            OnAmmoChanged?.Invoke(m_ActiveWeapon.CurrentAmmo, m_ActiveWeapon.MagazineSize);
        }

        private void PublishReloadProgress()
        {
            if (m_ActiveWeapon == null)
            {
                return;
            }

            bool isReloading = m_ActiveWeapon.State == WeaponRuntimeState.Reloading;
            OnReloadProgressChanged?.Invoke(isReloading, m_ActiveWeapon.ReloadProgress01);

            if (!isReloading)
            {
                return;
            }

            if (Mathf.Abs(m_ActiveWeapon.ReloadProgress01 - 1f) < 0.0001f)
            {
                PublishAmmoChanged();
            }
        }

        #endregion
    }
}
