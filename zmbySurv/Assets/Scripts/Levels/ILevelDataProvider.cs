using System;
using UnityEngine;

namespace Level
{
    /// <summary>
    /// Interface for loading level data from different sources (Resources, Server, etc.).
    /// TODO: Once you create your data class, replace 'object' with it.
    /// </summary>
    public interface ILevelDataProvider
    {
        /// <summary>
        /// Loads the level collection asynchronously.
        /// </summary>
        /// <param name="onSuccess">Callback invoked with your LevelCollection data class when loading succeeds.</param>
        /// <param name="onError">Callback invoked with error message when loading fails.</param>
        void LoadLevelsData(Action<object> onSuccess, Action<string> onError);
    }
}

