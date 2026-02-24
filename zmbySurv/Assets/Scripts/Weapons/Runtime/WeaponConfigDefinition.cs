namespace Weapons.Runtime
{
    /// <summary>
    /// Immutable runtime definition for a weapon configuration.
    /// </summary>
    public sealed class WeaponConfigDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponConfigDefinition"/> class.
        /// </summary>
        /// <param name="weaponId">Stable weapon identifier.</param>
        /// <param name="displayName">Display name used in UI.</param>
        /// <param name="weaponType">Weapon archetype.</param>
        /// <param name="damage">Damage applied per pellet.</param>
        /// <param name="magazineSize">Maximum bullets per magazine.</param>
        /// <param name="fireRateSeconds">Minimum seconds between shots.</param>
        /// <param name="reloadTimeSeconds">Seconds required to reload.</param>
        /// <param name="range">Maximum damage application range.</param>
        /// <param name="pelletCount">Pellets emitted per shot.</param>
        /// <param name="spreadAngleDegrees">Total spread cone angle in degrees.</param>
        public WeaponConfigDefinition(
            string weaponId,
            string displayName,
            WeaponType weaponType,
            int damage,
            int magazineSize,
            float fireRateSeconds,
            float reloadTimeSeconds,
            float range,
            int pelletCount,
            float spreadAngleDegrees)
        {
            WeaponId = weaponId;
            DisplayName = displayName;
            WeaponType = weaponType;
            Damage = damage;
            MagazineSize = magazineSize;
            FireRateSeconds = fireRateSeconds;
            ReloadTimeSeconds = reloadTimeSeconds;
            Range = range;
            PelletCount = pelletCount;
            SpreadAngleDegrees = spreadAngleDegrees;
        }

        /// <summary>
        /// Gets the stable weapon identifier.
        /// </summary>
        public string WeaponId { get; }

        /// <summary>
        /// Gets the localized or display-friendly name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the high-level weapon archetype.
        /// </summary>
        public WeaponType WeaponType { get; }

        /// <summary>
        /// Gets the damage applied by each pellet.
        /// </summary>
        public int Damage { get; }

        /// <summary>
        /// Gets the bullets available per magazine.
        /// </summary>
        public int MagazineSize { get; }

        /// <summary>
        /// Gets the minimum delay between consecutive shots.
        /// </summary>
        public float FireRateSeconds { get; }

        /// <summary>
        /// Gets the required reload duration.
        /// </summary>
        public float ReloadTimeSeconds { get; }

        /// <summary>
        /// Gets the maximum effective range for hit resolution.
        /// </summary>
        public float Range { get; }

        /// <summary>
        /// Gets the pellet count emitted per shot.
        /// </summary>
        public int PelletCount { get; }

        /// <summary>
        /// Gets the spread cone angle in degrees.
        /// </summary>
        public float SpreadAngleDegrees { get; }
    }
}
