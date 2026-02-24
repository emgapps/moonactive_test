using System;
using UnityEngine;

namespace Weapons.Runtime
{
    /// <summary>
    /// Concrete runtime implementation for weapon state, cooldown, and reload behavior.
    /// </summary>
    public sealed class WeaponRuntime : IWeapon
    {
        private readonly WeaponConfigDefinition m_Config;

        private int m_CurrentAmmo;
        private float m_NextAllowedShotTime;
        private float m_ReloadStartedAt;
        private float m_ReloadCompletesAt;
        private float m_LastKnownTime;
        private bool m_IsReloading;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponRuntime"/> class.
        /// </summary>
        /// <param name="config">Weapon configuration backing this runtime instance.</param>
        public WeaponRuntime(WeaponConfigDefinition config)
        {
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
            ResetState();
        }

        /// <summary>
        /// Gets stable weapon identifier.
        /// </summary>
        public string WeaponId => m_Config.WeaponId;

        /// <summary>
        /// Gets display name for UI.
        /// </summary>
        public string DisplayName => m_Config.DisplayName;

        /// <summary>
        /// Gets current ammo in magazine.
        /// </summary>
        public int CurrentAmmo => m_CurrentAmmo;

        /// <summary>
        /// Gets maximum ammo per magazine.
        /// </summary>
        public int MagazineSize => m_Config.MagazineSize;

        /// <summary>
        /// Gets current runtime state.
        /// </summary>
        public WeaponRuntimeState State
        {
            get
            {
                if (m_IsReloading)
                {
                    return WeaponRuntimeState.Reloading;
                }

                return m_LastKnownTime < m_NextAllowedShotTime
                    ? WeaponRuntimeState.Cooldown
                    : WeaponRuntimeState.Ready;
            }
        }

        /// <summary>
        /// Gets normalized reload progress in [0, 1].
        /// </summary>
        public float ReloadProgress01
        {
            get
            {
                if (!m_IsReloading)
                {
                    return 0f;
                }

                float reloadDuration = Mathf.Max(0.0001f, m_Config.ReloadTimeSeconds);
                float elapsed = Mathf.Max(0f, m_LastKnownTime - m_ReloadStartedAt);
                return Mathf.Clamp01(elapsed / reloadDuration);
            }
        }

        /// <summary>
        /// Attempts to fire the weapon.
        /// </summary>
        /// <param name="currentTimeSeconds">Current time in seconds.</param>
        /// <param name="shotRequest">Output shot details when fired.</param>
        /// <param name="shootResult">Outcome for this shoot attempt.</param>
        /// <returns>True when a shot was fired; otherwise false.</returns>
        public bool TryShoot(float currentTimeSeconds, out WeaponShotRequest shotRequest, out WeaponShootResult shootResult)
        {
            Tick(currentTimeSeconds);

            if (m_IsReloading)
            {
                shootResult = WeaponShootResult.BlockedReloading;
                shotRequest = default;
                return false;
            }

            if (currentTimeSeconds < m_NextAllowedShotTime)
            {
                shootResult = WeaponShootResult.BlockedCooldown;
                shotRequest = default;
                return false;
            }

            if (m_CurrentAmmo <= 0)
            {
                shootResult = WeaponShootResult.BlockedNoAmmo;
                shotRequest = default;
                return false;
            }

            m_CurrentAmmo -= 1;
            m_NextAllowedShotTime = currentTimeSeconds + m_Config.FireRateSeconds;
            m_LastKnownTime = currentTimeSeconds;

            shotRequest = new WeaponShotRequest(
                weaponId: m_Config.WeaponId,
                damagePerPellet: m_Config.Damage,
                range: m_Config.Range,
                pelletCount: m_Config.PelletCount,
                spreadAngleDegrees: m_Config.SpreadAngleDegrees);
            shootResult = WeaponShootResult.Fired;
            return true;
        }

        /// <summary>
        /// Attempts to start reload operation.
        /// </summary>
        /// <param name="currentTimeSeconds">Current time in seconds.</param>
        /// <returns>True when reload started; otherwise false.</returns>
        public bool TryStartReload(float currentTimeSeconds)
        {
            Tick(currentTimeSeconds);

            if (m_IsReloading)
            {
                return false;
            }

            if (m_CurrentAmmo >= m_Config.MagazineSize)
            {
                return false;
            }

            m_IsReloading = true;
            m_ReloadStartedAt = currentTimeSeconds;
            m_ReloadCompletesAt = currentTimeSeconds + m_Config.ReloadTimeSeconds;
            m_LastKnownTime = currentTimeSeconds;
            return true;
        }

        /// <summary>
        /// Advances internal timers and completes reload when needed.
        /// </summary>
        /// <param name="currentTimeSeconds">Current time in seconds.</param>
        public void Tick(float currentTimeSeconds)
        {
            m_LastKnownTime = currentTimeSeconds;

            if (!m_IsReloading)
            {
                return;
            }

            if (currentTimeSeconds < m_ReloadCompletesAt)
            {
                return;
            }

            m_CurrentAmmo = m_Config.MagazineSize;
            m_IsReloading = false;
            m_ReloadStartedAt = 0f;
            m_ReloadCompletesAt = 0f;
        }

        /// <summary>
        /// Resets transient runtime state for a new level start.
        /// </summary>
        public void ResetState()
        {
            m_CurrentAmmo = m_Config.MagazineSize;
            m_NextAllowedShotTime = 0f;
            m_ReloadStartedAt = 0f;
            m_ReloadCompletesAt = 0f;
            m_LastKnownTime = 0f;
            m_IsReloading = false;
        }
    }
}
