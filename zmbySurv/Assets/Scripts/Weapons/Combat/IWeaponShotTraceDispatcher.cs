namespace Weapons.Combat
{
    /// <summary>
    /// Abstraction that consumes traced pellet paths for visual or telemetry systems.
    /// </summary>
    public interface IWeaponShotTraceDispatcher
    {
        /// <summary>
        /// Dispatches one resolved pellet path.
        /// </summary>
        /// <param name="shotTrace">Resolved trace payload for a pellet.</param>
        void DispatchShotTrace(WeaponShotTrace shotTrace);
    }
}
