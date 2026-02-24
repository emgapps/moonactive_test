using System.Collections.Generic;
using Weapons.Runtime;

namespace Weapons.Data
{
    /// <summary>
    /// Validation helpers for JSON weapon catalog payloads.
    /// </summary>
    public static class WeaponDataValidation
    {
        /// <summary>
        /// Validates complete weapon catalog payload.
        /// </summary>
        /// <param name="catalog">Catalog payload to validate.</param>
        /// <param name="errorMessage">Validation error when invalid, otherwise empty string.</param>
        /// <returns>True when payload is valid and safe to consume.</returns>
        public static bool TryValidateCatalog(WeaponCatalogDto catalog, out string errorMessage)
        {
            if (catalog == null)
            {
                errorMessage = "Weapon catalog is null.";
                return false;
            }

            if (catalog.weapons == null || catalog.weapons.Count == 0)
            {
                errorMessage = "Weapon catalog has no weapon entries.";
                return false;
            }

            HashSet<string> weaponIds = new HashSet<string>();
            for (int index = 0; index < catalog.weapons.Count; index++)
            {
                if (!TryValidateWeapon(catalog.weapons[index], index, weaponIds, out errorMessage))
                {
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(catalog.defaultWeaponId))
            {
                errorMessage = "Weapon catalog defaultWeaponId is empty.";
                return false;
            }

            if (!weaponIds.Contains(catalog.defaultWeaponId))
            {
                errorMessage =
                    $"Weapon catalog defaultWeaponId='{catalog.defaultWeaponId}' is missing in weapons collection.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Tries to parse weapon type from JSON payload.
        /// </summary>
        /// <param name="rawValue">Raw string value from JSON.</param>
        /// <param name="weaponType">Parsed enum value when valid.</param>
        /// <returns>True when parsed successfully; otherwise false.</returns>
        public static bool TryParseWeaponType(string rawValue, out WeaponType weaponType)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                weaponType = WeaponType.Pistol;
                return false;
            }

            switch (rawValue.Trim().ToLowerInvariant())
            {
                case "pistol":
                    weaponType = WeaponType.Pistol;
                    return true;
                case "shotgun":
                    weaponType = WeaponType.Shotgun;
                    return true;
                case "machinegun":
                    weaponType = WeaponType.Machinegun;
                    return true;
                default:
                    weaponType = WeaponType.Pistol;
                    return false;
            }
        }

        private static bool TryValidateWeapon(
            WeaponConfigDto weapon,
            int index,
            HashSet<string> weaponIds,
            out string errorMessage)
        {
            if (weapon == null)
            {
                errorMessage = $"Weapon entry at index {index} is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(weapon.weaponId))
            {
                errorMessage = $"Weapon entry at index {index} has empty weaponId.";
                return false;
            }

            if (!weaponIds.Add(weapon.weaponId))
            {
                errorMessage = $"Weapon id '{weapon.weaponId}' is duplicated.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(weapon.displayName))
            {
                errorMessage = $"Weapon '{weapon.weaponId}' has empty displayName.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(weapon.weaponImageName))
            {
                errorMessage = $"Weapon '{weapon.weaponId}' has empty weaponImageName.";
                return false;
            }

            if (!TryParseWeaponType(weapon.weaponType, out _))
            {
                errorMessage = $"Weapon '{weapon.weaponId}' has unsupported weaponType='{weapon.weaponType}'.";
                return false;
            }

            if (weapon.damage <= 0)
            {
                errorMessage = $"Weapon '{weapon.weaponId}' has invalid damage={weapon.damage}.";
                return false;
            }

            if (weapon.magazineSize <= 0)
            {
                errorMessage = $"Weapon '{weapon.weaponId}' has invalid magazineSize={weapon.magazineSize}.";
                return false;
            }

            if (weapon.fireRate <= 0f)
            {
                errorMessage = $"Weapon '{weapon.weaponId}' has invalid fireRate={weapon.fireRate}.";
                return false;
            }

            if (weapon.reloadTime <= 0f)
            {
                errorMessage = $"Weapon '{weapon.weaponId}' has invalid reloadTime={weapon.reloadTime}.";
                return false;
            }

            if (weapon.range <= 0f)
            {
                errorMessage = $"Weapon '{weapon.weaponId}' has invalid range={weapon.range}.";
                return false;
            }

            if (weapon.pelletCount <= 0)
            {
                errorMessage = $"Weapon '{weapon.weaponId}' has invalid pelletCount={weapon.pelletCount}.";
                return false;
            }

            if (weapon.spreadAngle < 0f)
            {
                errorMessage = $"Weapon '{weapon.weaponId}' has invalid spreadAngle={weapon.spreadAngle}.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
