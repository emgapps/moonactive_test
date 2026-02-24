using System;
using Level.Data;
using UnityEngine;

namespace Level
{
    /// <summary>
    /// Interface for loading level data from different sources (Resources, Server, etc.).
    /// </summary>
    public interface ILevelDataProvider
    {
        /// <summary>
        /// Loads the level collection asynchronously.
        /// </summary>
        /// <param name="onSuccess">Callback invoked with a level collection DTO when loading succeeds.</param>
        /// <param name="onError">Callback invoked with error message when loading fails.</param>
        void LoadLevelsData(Action<LevelCollectionDto> onSuccess, Action<string> onError);
    }
}
