using System;
using UnityEngine;
using Weapons.Runtime;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Weapons.Providers
{
    /// <summary>
    /// Resolves weapon images from configured JSON image names.
    /// </summary>
    public sealed class WeaponImageProvider : IWeaponImageProvider
    {
        private const string DefaultResourcesImageFolder = "Guns";
#if UNITY_EDITOR
        private const string DefaultEditorImageFolder = "Assets/Guns";
        private const string EditorAssetsPrefix = "Assets/";
#endif
        private const string PngExtension = ".png";

        private readonly string m_ResourcesImageFolder;
#if UNITY_EDITOR
        private readonly string m_EditorImageFolder;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponImageProvider"/> class.
        /// </summary>
        /// <param name="resourcesImageFolder">Resources folder used for image loading.</param>
        public WeaponImageProvider(string resourcesImageFolder = DefaultResourcesImageFolder)
        {
            m_ResourcesImageFolder = NormalizeRootPath(resourcesImageFolder, DefaultResourcesImageFolder);
#if UNITY_EDITOR
            m_EditorImageFolder = NormalizeRootPath(DefaultEditorImageFolder, DefaultEditorImageFolder);
#endif
        }

        /// <inheritdoc/>
        public bool TryResolveImagePath(WeaponConfigDefinition weaponDefinition, out string imagePath)
        {
            imagePath = string.Empty;

            if (weaponDefinition == null)
            {
                return false;
            }

            string normalizedImageName = NormalizeImageName(weaponDefinition.WeaponImageName);
            if (string.IsNullOrWhiteSpace(normalizedImageName))
            {
                return false;
            }

            if (normalizedImageName.Contains("/"))
            {
                imagePath = normalizedImageName;
                return true;
            }

            imagePath = $"{m_ResourcesImageFolder}/{normalizedImageName}";
            return true;
        }

        /// <inheritdoc/>
        public Sprite GetWeaponImage(WeaponConfigDefinition weaponDefinition)
        {
            if (!TryResolveImagePath(weaponDefinition, out string imagePath))
            {
                return null;
            }

            Sprite sprite = Resources.Load<Sprite>(imagePath);
            if (sprite != null)
            {
                return sprite;
            }

#if UNITY_EDITOR
            string editorAssetPath = ResolveEditorAssetPath(weaponDefinition.WeaponImageName);
            if (string.IsNullOrWhiteSpace(editorAssetPath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(editorAssetPath);
#else
            return null;
#endif
        }

        private static string NormalizeImageName(string imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName))
            {
                return string.Empty;
            }

            string normalized = imageName.Trim().Replace('\\', '/').Trim('/');
            if (normalized.EndsWith(PngExtension, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[..^PngExtension.Length];
            }

            return normalized.Trim('/');
        }

        private static string NormalizeRootPath(string path, string fallback)
        {
            string normalized = string.IsNullOrWhiteSpace(path) ? fallback : path;
            return normalized.Trim().Replace('\\', '/').Trim('/');
        }

#if UNITY_EDITOR
        private string ResolveEditorAssetPath(string imageName)
        {
            string normalizedImageName = NormalizeImageName(imageName);
            if (string.IsNullOrWhiteSpace(normalizedImageName))
            {
                return string.Empty;
            }

            if (normalizedImageName.StartsWith(EditorAssetsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return AppendExtensionIfNeeded(normalizedImageName);
            }

            if (normalizedImageName.Contains("/"))
            {
                return AppendExtensionIfNeeded($"{EditorAssetsPrefix}{normalizedImageName}");
            }

            return $"{m_EditorImageFolder}/{normalizedImageName}{PngExtension}";
        }

        private static string AppendExtensionIfNeeded(string assetPath)
        {
            if (assetPath.EndsWith(PngExtension, StringComparison.OrdinalIgnoreCase))
            {
                return assetPath;
            }

            return $"{assetPath}{PngExtension}";
        }
#endif
    }
}
