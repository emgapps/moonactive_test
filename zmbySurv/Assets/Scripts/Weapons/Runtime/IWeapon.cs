namespace Weapons.Runtime
{
    /// <summary>
    /// Defines runtime behavior for a single weapon instance.
    /// </summary>
    public interface IWeapon
    {
        /// <summary>
        /// Gets stable weapon identifier.
        /// </summary>
        string WeaponId { get; }

        /// <summary>
        /// Gets human-readable weapon name.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Gets current magazine ammo count.
        /// </summary>
        int CurrentAmmo { get; }

        /// <summary>
        /// Gets configured maximum magazine size.
        /// </summary>
        int MagazineSize { get; }

        /// <summary>
        /// Gets current runtime state.
        /// </summary>
        WeaponRuntimeState State { get; }

        /// <summary>
        /// Gets normalized reload progress in range [0, 1].
        /// </summary>
        float ReloadProgress01 { get; }

        /// <summary>
        /// Attempts to fire the weapon.
        /// </summary>
        /// <param name="currentTimeSeconds">Current time value in seconds.</param>
        /// <param name="shotRequest">Output shot details when fired.</param>
        /// <param name="shootResult">Outcome for this shoot attempt.</param>
        /// <returns>True when shot was fired successfully; otherwise false.</returns>
        bool TryShoot(float currentTimeSeconds, out WeaponShotRequest shotRequest, out WeaponShootResult shootResult);

        /// <summary>
        /// Attempts to start a reload operation.
        /// </summary>
        /// <param name="currentTimeSeconds">Current time value in seconds.</param>
        /// <returns>True when reload started; otherwise false.</returns>
        bool TryStartReload(float currentTimeSeconds);

        /// <summary>
        /// Advances internal runtime timers.
        /// </summary>
        /// <param name="currentTimeSeconds">Current time value in seconds.</param>
        void Tick(float currentTimeSeconds);

        /// <summary>
        /// Resets transient state for a fresh level start.
        /// </summary>
        void ResetState();
    }
}
