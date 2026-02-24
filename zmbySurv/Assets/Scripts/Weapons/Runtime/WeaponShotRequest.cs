namespace Weapons.Runtime
{
    /// <summary>
    /// Describes a resolved shot request from weapon runtime logic.
    /// </summary>
    public struct WeaponShotRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponShotRequest"/> struct.
        /// </summary>
        /// <param name="weaponId">Identifier for the weapon that fired.</param>
        /// <param name="damagePerPellet">Damage applied by each pellet.</param>
        /// <param name="range">Maximum shot range.</param>
        /// <param name="pelletCount">Number of pellets emitted for this shot.</param>
        /// <param name="spreadAngleDegrees">Total spread cone angle.</param>
        public WeaponShotRequest(
            string weaponId,
            int damagePerPellet,
            float range,
            int pelletCount,
            float spreadAngleDegrees)
        {
            WeaponId = weaponId;
            DamagePerPellet = damagePerPellet;
            Range = range;
            PelletCount = pelletCount;
            SpreadAngleDegrees = spreadAngleDegrees;
        }

        /// <summary>
        /// Gets weapon identifier associated with this shot.
        /// </summary>
        public string WeaponId { get; }

        /// <summary>
        /// Gets damage applied by each pellet.
        /// </summary>
        public int DamagePerPellet { get; }

        /// <summary>
        /// Gets maximum shot range.
        /// </summary>
        public float Range { get; }

        /// <summary>
        /// Gets number of pellets emitted by this shot.
        /// </summary>
        public int PelletCount { get; }

        /// <summary>
        /// Gets total spread angle in degrees.
        /// </summary>
        public float SpreadAngleDegrees { get; }
    }
}
