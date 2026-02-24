using System.Collections.Generic;
using UnityEngine;

namespace Core.Pooling
{
    /// <summary>
    /// Defines a reusable object pool contract for Unity component instances.
    /// </summary>
    /// <typeparam name="T">Component type stored in the pool.</typeparam>
    public interface IObjectPool<T> where T : Component
    {
        /// <summary>
        /// Gets the number of currently checked-out instances.
        /// </summary>
        int ActiveCount { get; }

        /// <summary>
        /// Gets the number of available inactive instances.
        /// </summary>
        int InactiveCount { get; }

        /// <summary>
        /// Gets a read-only view of active instances.
        /// </summary>
        IReadOnlyCollection<T> ActiveObjects { get; }

        /// <summary>
        /// Gets an instance from the pool.
        /// </summary>
        /// <returns>An active pooled instance.</returns>
        T Get();

        /// <summary>
        /// Returns an instance back to the pool.
        /// </summary>
        /// <param name="instance">Instance to return.</param>
        void Release(T instance);

        /// <summary>
        /// Clears all pooled instances.
        /// </summary>
        void Clear();
    }
}
