namespace Core.Pooling
{
    /// <summary>
    /// Defines lifecycle hooks for components that participate in pooling.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when object is taken from a pool.
        /// </summary>
        void OnTakenFromPool();

        /// <summary>
        /// Called when object is returned to a pool.
        /// </summary>
        void OnReturnedToPool();
    }
}
