namespace Weapons.Runtime
{
    /// <summary>
    /// Indicates outcome for a shoot attempt.
    /// </summary>
    public enum WeaponShootResult
    {
        /// <summary>
        /// Shot executed successfully.
        /// </summary>
        Fired = 0,

        /// <summary>
        /// Shot blocked because weapon is reloading.
        /// </summary>
        BlockedReloading = 1,

        /// <summary>
        /// Shot blocked because fire-rate cooldown is still active.
        /// </summary>
        BlockedCooldown = 2,

        /// <summary>
        /// Shot blocked because magazine is empty.
        /// </summary>
        BlockedNoAmmo = 3
    }
}
