using UnityEngine;
using Weapons.Runtime;

namespace Weapons.Providers
{
    /// <summary>
    /// Resolves weapon image paths and sprites for UI systems.
    /// </summary>
    public interface IWeaponImageProvider
    {
        /// <summary>
        /// Attempts to resolve a resources path for the provided weapon configuration.
        /// </summary>
        /// <param name="weaponDefinition">Weapon configuration containing image metadata.</param>
        /// <param name="imagePath">Resolved resource path without file extension when available.</param>
        /// <returns>True when a valid path was resolved; otherwise false.</returns>
        bool TryResolveImagePath(WeaponConfigDefinition weaponDefinition, out string imagePath);

        /// <summary>
        /// Resolves the weapon sprite for the provided weapon configuration.
        /// </summary>
        /// <param name="weaponDefinition">Weapon configuration containing image metadata.</param>
        /// <returns>Resolved sprite when available; otherwise null.</returns>
        Sprite GetWeaponImage(WeaponConfigDefinition weaponDefinition);
    }
}
