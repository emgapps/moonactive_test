using System;
using Weapons.Runtime;

namespace Weapons.Providers
{
    /// <summary>
    /// Provides weapon configuration catalog data from external sources.
    /// </summary>
    public interface IWeaponConfigProvider
    {
        /// <summary>
        /// Loads weapon configuration catalog.
        /// </summary>
        /// <param name="onSuccess">Called when catalog loading succeeds.</param>
        /// <param name="onError">Called when catalog loading fails.</param>
        void LoadWeaponCatalog(Action<WeaponConfigCatalog> onSuccess, Action<string> onError);
    }
}
