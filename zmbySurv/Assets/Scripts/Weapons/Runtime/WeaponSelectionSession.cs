using System;
using System.Collections.Generic;

namespace Weapons.Runtime
{
    /// <summary>
    /// Stores active weapon catalog and selected weapon for the current runtime session.
    /// </summary>
    public static class WeaponSelectionSession
    {
        private static WeaponConfigCatalog s_CurrentCatalog;
        private static string s_SelectedWeaponId;

        /// <summary>
        /// Gets whether a weapon catalog is currently loaded.
        /// </summary>
        public static bool HasCatalog => s_CurrentCatalog != null;

        /// <summary>
        /// Gets whether a weapon has been selected for the active session.
        /// </summary>
        public static bool HasSelection => !string.IsNullOrWhiteSpace(s_SelectedWeaponId);

        /// <summary>
        /// Gets currently selected weapon identifier.
        /// </summary>
        public static string SelectedWeaponId => s_SelectedWeaponId;

        /// <summary>
        /// Gets current loaded weapon catalog.
        /// </summary>
        public static WeaponConfigCatalog CurrentCatalog => s_CurrentCatalog;

        /// <summary>
        /// Sets current catalog and ensures selection is valid.
        /// </summary>
        /// <param name="catalog">Catalog instance to assign.</param>
        public static void SetCatalog(WeaponConfigCatalog catalog)
        {
            s_CurrentCatalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

            if (TryGetWeaponDefinition(s_SelectedWeaponId, out _))
            {
                return;
            }

            s_SelectedWeaponId = catalog.DefaultWeaponId;
        }

        /// <summary>
        /// Attempts to select weapon by identifier.
        /// </summary>
        /// <param name="weaponId">Weapon identifier to select.</param>
        /// <returns>True when weapon exists in current catalog; otherwise false.</returns>
        public static bool TrySelectWeapon(string weaponId)
        {
            if (!TryGetWeaponDefinition(weaponId, out _))
            {
                return false;
            }

            s_SelectedWeaponId = weaponId;
            return true;
        }

        /// <summary>
        /// Attempts to resolve currently selected weapon definition.
        /// </summary>
        /// <param name="definition">Selected weapon definition when available.</param>
        /// <returns>True when selected weapon can be resolved; otherwise false.</returns>
        public static bool TryGetSelectedWeapon(out WeaponConfigDefinition definition)
        {
            return TryGetWeaponDefinition(s_SelectedWeaponId, out definition);
        }

        /// <summary>
        /// Clears catalog and selection state.
        /// </summary>
        public static void Clear()
        {
            s_CurrentCatalog = null;
            s_SelectedWeaponId = string.Empty;
        }

        private static bool TryGetWeaponDefinition(string weaponId, out WeaponConfigDefinition definition)
        {
            definition = null;

            if (s_CurrentCatalog == null || string.IsNullOrWhiteSpace(weaponId))
            {
                return false;
            }

            IReadOnlyList<WeaponConfigDefinition> definitions = s_CurrentCatalog.Weapons;
            for (int index = 0; index < definitions.Count; index++)
            {
                WeaponConfigDefinition candidate = definitions[index];
                if (!string.Equals(candidate.WeaponId, weaponId, StringComparison.Ordinal))
                {
                    continue;
                }

                definition = candidate;
                return true;
            }

            return false;
        }
    }
}
