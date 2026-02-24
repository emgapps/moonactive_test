using System;
using System.Collections.Generic;
using Weapons.Providers;

namespace Weapons.Runtime
{
    /// <summary>
    /// Loads and caches weapon catalog data for runtime usage.
    /// </summary>
    public sealed class WeaponCatalogService
    {
        private const string DefaultResourcesPath = "Weapons/Weapons";

        private readonly IWeaponConfigProvider m_ConfigProvider;

        private WeaponConfigCatalog m_CachedCatalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponCatalogService"/> class.
        /// </summary>
        /// <param name="configProvider">Provider used to load catalog configuration.</param>
        public WeaponCatalogService(IWeaponConfigProvider configProvider)
        {
            m_ConfigProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        /// <summary>
        /// Creates service instance using the default resources provider.
        /// </summary>
        /// <returns>Ready-to-use catalog service.</returns>
        public static WeaponCatalogService CreateDefault()
        {
            return new WeaponCatalogService(new ResourcesWeaponConfigProvider(DefaultResourcesPath));
        }

        /// <summary>
        /// Loads catalog from provider, using cached data when available.
        /// </summary>
        /// <param name="onSuccess">Success callback with loaded catalog.</param>
        /// <param name="onError">Error callback with actionable message.</param>
        public void LoadCatalog(Action<WeaponConfigCatalog> onSuccess, Action<string> onError)
        {
            if (onSuccess == null)
            {
                throw new ArgumentNullException(nameof(onSuccess));
            }

            if (onError == null)
            {
                throw new ArgumentNullException(nameof(onError));
            }

            if (m_CachedCatalog != null)
            {
                onSuccess.Invoke(m_CachedCatalog);
                return;
            }

            m_ConfigProvider.LoadWeaponCatalog(
                onSuccess: catalog =>
                {
                    m_CachedCatalog = catalog;
                    onSuccess.Invoke(catalog);
                },
                onError: onError);
        }

        /// <summary>
        /// Tries to resolve a weapon definition by id from currently cached catalog.
        /// </summary>
        /// <param name="weaponId">Weapon identifier to resolve.</param>
        /// <param name="definition">Resolved definition when found.</param>
        /// <returns>True when weapon exists; otherwise false.</returns>
        public bool TryGetWeaponDefinition(string weaponId, out WeaponConfigDefinition definition)
        {
            definition = null;
            if (m_CachedCatalog == null || string.IsNullOrWhiteSpace(weaponId))
            {
                return false;
            }

            IReadOnlyList<WeaponConfigDefinition> definitions = m_CachedCatalog.Weapons;
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
