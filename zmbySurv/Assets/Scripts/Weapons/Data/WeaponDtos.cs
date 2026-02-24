using System;
using System.Collections.Generic;

namespace Weapons.Data
{
    /// <summary>
    /// Root DTO for weapon catalog JSON payload.
    /// </summary>
    [Serializable]
    public sealed class WeaponCatalogDto
    {
        /// <summary>
        /// Default weapon identifier preselected for a session.
        /// </summary>
        public string defaultWeaponId;

        /// <summary>
        /// Weapon configuration entries.
        /// </summary>
        public List<WeaponConfigDto> weapons;
    }

    /// <summary>
    /// DTO for a single weapon entry in JSON payload.
    /// </summary>
    [Serializable]
    public sealed class WeaponConfigDto
    {
        /// <summary>
        /// Stable weapon identifier.
        /// </summary>
        public string weaponId;

        /// <summary>
        /// Display name for UI.
        /// </summary>
        public string displayName;

        /// <summary>
        /// Weapon image name used by UI image providers.
        /// </summary>
        public string weaponImageName;

        /// <summary>
        /// Weapon type string (Pistol, Shotgun, Machinegun).
        /// </summary>
        public string weaponType;

        /// <summary>
        /// Damage applied by each pellet.
        /// </summary>
        public int damage;

        /// <summary>
        /// Maximum bullets per magazine.
        /// </summary>
        public int magazineSize;

        /// <summary>
        /// Seconds between shots.
        /// </summary>
        public float fireRate;

        /// <summary>
        /// Seconds required to reload.
        /// </summary>
        public float reloadTime;

        /// <summary>
        /// Maximum effective range.
        /// </summary>
        public float range;

        /// <summary>
        /// Pellets fired per shot.
        /// </summary>
        public int pelletCount;

        /// <summary>
        /// Spread cone angle in degrees.
        /// </summary>
        public float spreadAngle;
    }
}
