using System.Collections.Generic;

namespace Weapons.Runtime
{
    /// <summary>
    /// Encapsulates the loaded set of weapon configurations and selection defaults.
    /// </summary>
    public sealed class WeaponConfigCatalog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponConfigCatalog"/> class.
        /// </summary>
        /// <param name="defaultWeaponId">Default weapon identifier to preselect.</param>
        /// <param name="weapons">Available weapon definitions.</param>
        public WeaponConfigCatalog(string defaultWeaponId, IReadOnlyList<WeaponConfigDefinition> weapons)
        {
            DefaultWeaponId = defaultWeaponId;
            Weapons = weapons;
        }

        /// <summary>
        /// Gets the default weapon identifier.
        /// </summary>
        public string DefaultWeaponId { get; }

        /// <summary>
        /// Gets available weapon definitions.
        /// </summary>
        public IReadOnlyList<WeaponConfigDefinition> Weapons { get; }
    }
}
