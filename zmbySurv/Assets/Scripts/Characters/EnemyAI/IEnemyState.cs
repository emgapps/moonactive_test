namespace Characters.EnemyAI
{
    /// <summary>
    /// Defines a behavior state that can be executed by an enemy AI state machine.
    /// </summary>
    public interface IEnemyState
    {
        /// <summary>
        /// Gets a human-readable state name for diagnostics.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Runs when the state becomes active.
        /// </summary>
        void Enter();

        /// <summary>
        /// Runs every frame while the state is active.
        /// </summary>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        void Tick(float deltaTime);

        /// <summary>
        /// Runs before the state is deactivated.
        /// </summary>
        void Exit();
    }
}
