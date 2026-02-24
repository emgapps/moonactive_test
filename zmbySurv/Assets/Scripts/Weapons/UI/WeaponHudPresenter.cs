using UnityEngine;
using UnityEngine.UI;
using Weapons.Runtime;

namespace Weapons.UI
{
    /// <summary>
    /// Presents weapon ammo and reload state on in-game HUD.
    /// </summary>
    public sealed class WeaponHudPresenter : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField]
        private PlayerWeaponController m_PlayerWeaponController;
        [SerializeField]
        private bool m_AutoBindPlayerWeaponController = true;

        [Header("HUD")]
        [SerializeField]
        private Text m_WeaponNameText;
        [SerializeField]
        private Text m_AmmoText;
        [SerializeField]
        private GameObject m_ReloadIndicatorRoot;
        [SerializeField]
        private Image m_ReloadFillImage;
        [SerializeField]
        private Slider m_ReloadSlider;
        [SerializeField]
        private Text m_ReloadText;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (m_AutoBindPlayerWeaponController && m_PlayerWeaponController == null)
            {
                m_PlayerWeaponController = FindObjectOfType<PlayerWeaponController>();
            }

            SetReloadIndicatorVisible(false);
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshFromCurrentState();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        #endregion

        #region Private Helpers

        private void Subscribe()
        {
            if (m_PlayerWeaponController == null)
            {
                Debug.LogWarning("[WeaponsHUD] BindSkipped | reason=missing_player_weapon_controller");
                return;
            }

            m_PlayerWeaponController.OnWeaponEquipped += HandleWeaponEquipped;
            m_PlayerWeaponController.OnAmmoChanged += HandleAmmoChanged;
            m_PlayerWeaponController.OnReloadProgressChanged += HandleReloadProgressChanged;
        }

        private void Unsubscribe()
        {
            if (m_PlayerWeaponController == null)
            {
                return;
            }

            m_PlayerWeaponController.OnWeaponEquipped -= HandleWeaponEquipped;
            m_PlayerWeaponController.OnAmmoChanged -= HandleAmmoChanged;
            m_PlayerWeaponController.OnReloadProgressChanged -= HandleReloadProgressChanged;
        }

        private void RefreshFromCurrentState()
        {
            if (m_PlayerWeaponController == null || !m_PlayerWeaponController.IsInitialized)
            {
                return;
            }

            if (m_AmmoText != null)
            {
                m_AmmoText.text = $"{m_PlayerWeaponController.CurrentAmmo}/{m_PlayerWeaponController.MagazineSize}";
            }

            if (m_WeaponNameText != null)
            {
                m_WeaponNameText.text = m_PlayerWeaponController.CurrentWeaponId;
            }
        }

        private void HandleWeaponEquipped(WeaponConfigDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            if (m_WeaponNameText != null)
            {
                m_WeaponNameText.text = definition.DisplayName;
            }
        }

        private void HandleAmmoChanged(int currentAmmo, int maxAmmo)
        {
            if (m_AmmoText == null)
            {
                return;
            }

            m_AmmoText.text = $"{currentAmmo}/{maxAmmo}";
        }

        private void HandleReloadProgressChanged(bool isReloading, float progress01)
        {
            SetReloadIndicatorVisible(isReloading);

            if (!isReloading)
            {
                if (m_ReloadFillImage != null)
                {
                    m_ReloadFillImage.fillAmount = 0f;
                }

                if (m_ReloadSlider != null)
                {
                    m_ReloadSlider.value = 0f;
                }

                if (m_ReloadText != null)
                {
                    m_ReloadText.text = string.Empty;
                }

                return;
            }

            float clampedProgress = Mathf.Clamp01(progress01);
            if (m_ReloadFillImage != null)
            {
                m_ReloadFillImage.fillAmount = clampedProgress;
            }

            if (m_ReloadSlider != null)
            {
                m_ReloadSlider.value = clampedProgress;
            }

            if (m_ReloadText != null)
            {
                m_ReloadText.text = $"Reloading {clampedProgress * 100f:0}%";
            }
        }

        private void SetReloadIndicatorVisible(bool visible)
        {
            if (m_ReloadIndicatorRoot != null)
            {
                m_ReloadIndicatorRoot.SetActive(visible);
            }
        }

        #endregion
    }
}
