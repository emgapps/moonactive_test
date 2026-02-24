using System;
using System.Collections.Generic;
using Weapons.Data;
using Weapons.Runtime;
using UnityEngine;

namespace Weapons.Providers
{
    /// <summary>
    /// Loads weapon configuration from Unity Resources JSON.
    /// </summary>
    public sealed class ResourcesWeaponConfigProvider : IWeaponConfigProvider
    {
        private const string DefaultResourcesPath = "Weapons/Weapons";

        private readonly string m_ResourcesPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcesWeaponConfigProvider"/> class.
        /// </summary>
        /// <param name="resourcesPath">Resources path without extension.</param>
        public ResourcesWeaponConfigProvider(string resourcesPath)
        {
            m_ResourcesPath = string.IsNullOrWhiteSpace(resourcesPath) ? DefaultResourcesPath : resourcesPath;
        }

        /// <summary>
        /// Loads and validates weapon catalog data.
        /// </summary>
        /// <param name="onSuccess">Success callback for loaded catalog.</param>
        /// <param name="onError">Error callback for actionable failures.</param>
        public void LoadWeaponCatalog(Action<WeaponConfigCatalog> onSuccess, Action<string> onError)
        {
            if (onSuccess == null)
            {
                throw new ArgumentNullException(nameof(onSuccess));
            }

            if (onError == null)
            {
                throw new ArgumentNullException(nameof(onError));
            }

            try
            {
                TextAsset weaponsJsonAsset = Resources.Load<TextAsset>(m_ResourcesPath);
                if (weaponsJsonAsset == null)
                {
                    onError.Invoke($"[WeaponConfigProvider] LoadFailed | reason=missing_resource path={m_ResourcesPath}");
                    return;
                }

                WeaponCatalogDto catalogDto = JsonUtility.FromJson<WeaponCatalogDto>(weaponsJsonAsset.text);
                if (!WeaponDataValidation.TryValidateCatalog(catalogDto, out string validationError))
                {
                    onError.Invoke($"[WeaponConfigProvider] ValidationFailed | path={m_ResourcesPath} error={validationError}");
                    return;
                }

                WeaponConfigCatalog catalog = BuildCatalog(catalogDto);
                onSuccess.Invoke(catalog);
            }
            catch (Exception exception)
            {
                onError.Invoke(
                    $"[WeaponConfigProvider] LoadFailed | reason=exception type={exception.GetType().Name} message={exception.Message}");
            }
        }

        private static WeaponConfigCatalog BuildCatalog(WeaponCatalogDto catalogDto)
        {
            List<WeaponConfigDefinition> definitions = new List<WeaponConfigDefinition>(catalogDto.weapons.Count);
            for (int index = 0; index < catalogDto.weapons.Count; index++)
            {
                WeaponConfigDto weaponDto = catalogDto.weapons[index];
                WeaponDataValidation.TryParseWeaponType(weaponDto.weaponType, out WeaponType weaponType);

                WeaponConfigDefinition definition = new WeaponConfigDefinition(
                    weaponId: weaponDto.weaponId,
                    displayName: weaponDto.displayName,
                    weaponType: weaponType,
                    damage: weaponDto.damage,
                    magazineSize: weaponDto.magazineSize,
                    fireRateSeconds: weaponDto.fireRate,
                    reloadTimeSeconds: weaponDto.reloadTime,
                    range: weaponDto.range,
                    pelletCount: weaponDto.pelletCount,
                    spreadAngleDegrees: weaponDto.spreadAngle,
                    weaponImageName: weaponDto.weaponImageName);

                definitions.Add(definition);
            }

            return new WeaponConfigCatalog(catalogDto.defaultWeaponId, definitions);
        }
    }
}
