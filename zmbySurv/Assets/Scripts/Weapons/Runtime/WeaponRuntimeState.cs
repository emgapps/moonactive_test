namespace Weapons.Runtime
{
    /// <summary>
    /// Represents runtime state for a weapon instance.
    /// </summary>
    public enum WeaponRuntimeState
    {
        /// <summary>
        /// Weapon is ready for input.
        /// </summary>
        Ready = 0,

        /// <summary>
        /// Weapon is waiting for fire-rate cooldown.
        /// </summary>
        Cooldown = 1,

        /// <summary>
        /// Weapon is currently reloading.
        /// </summary>
        Reloading = 2
    }
}
