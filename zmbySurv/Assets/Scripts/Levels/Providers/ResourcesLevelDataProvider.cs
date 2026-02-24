using System;
using Level.Data;
using UnityEngine;

namespace Level.Providers
{
    /// <summary>
    /// Loads level configuration from Unity Resources as JSON.
    /// </summary>
    public sealed class ResourcesLevelDataProvider : ILevelDataProvider
    {
        private readonly string m_ResourcesPath;

        /// <summary>
        /// Creates a provider for the configured resources path.
        /// </summary>
        /// <param name="resourcesPath">Resource path without file extension.</param>
        public ResourcesLevelDataProvider(string resourcesPath)
        {
            m_ResourcesPath = string.IsNullOrWhiteSpace(resourcesPath) ? "Levels/Levels" : resourcesPath;
        }

        /// <summary>
        /// Loads level data and invokes callbacks with parse/validation result.
        /// </summary>
        /// <param name="onSuccess">Success callback with loaded level collection DTO.</param>
        /// <param name="onError">Error callback with actionable message.</param>
        public void LoadLevelsData(Action<object> onSuccess, Action<string> onError)
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
                TextAsset levelJsonAsset = Resources.Load<TextAsset>(m_ResourcesPath);
                if (levelJsonAsset == null)
                {
                    onError.Invoke($"[LevelDataProvider] LoadFailed | reason=missing_resource path={m_ResourcesPath}");
                    return;
                }

                LevelCollectionDto levelCollection = JsonUtility.FromJson<LevelCollectionDto>(levelJsonAsset.text);
                if (!LevelDataValidation.TryValidateCollection(levelCollection, out string errorMessage))
                {
                    onError.Invoke($"[LevelDataProvider] ValidationFailed | path={m_ResourcesPath} error={errorMessage}");
                    return;
                }

                onSuccess.Invoke(levelCollection);
            }
            catch (Exception exception)
            {
                onError.Invoke(
                    $"[LevelDataProvider] LoadFailed | reason=exception type={exception.GetType().Name} message={exception.Message}");
            }
        }
    }
}
