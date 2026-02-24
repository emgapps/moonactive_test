namespace PlayerMovements
{
    /// <summary>
    /// Interface for player input controllers.
    /// Implementations handle different input sources (keyboard, AI, touch, etc.).
    /// </summary>
    public interface IPlayerController
    {
        /// <summary>
        /// Updates the player control logic.
        /// Called every frame to process input and control the player.
        /// </summary>
        void UpdateControl();
    }
}