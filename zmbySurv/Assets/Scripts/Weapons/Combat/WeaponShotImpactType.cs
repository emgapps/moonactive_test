namespace Weapons.Combat
{
    /// <summary>
    /// Identifies what ended a traced pellet path.
    /// </summary>
    public enum WeaponShotImpactType
    {
        /// <summary>
        /// Pellet reached max range without a collider impact.
        /// </summary>
        None = 0,

        /// <summary>
        /// Pellet impacted an enemy damageable target.
        /// </summary>
        Enemy = 1,

        /// <summary>
        /// Pellet impacted a blocking world collider.
        /// </summary>
        BlockingCollider = 2
    }
}
