using Characters;
using PlayerMovements;
using UnityEngine;

namespace PlayerMovements
{
    /// <summary>
    /// Handles keyboard input for player movement.
    /// Uses WASD keys to control the player character.
    /// </summary>
    public class HumanController : IPlayerController
    {
        #region Private Fields

        private PlayerController m_PlayerController;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the HumanController class.
        /// </summary>
        /// <param name="playerController">The player controller to send input commands to.</param>
        public HumanController(PlayerController playerController)
        {
            m_PlayerController = playerController;
        }

        #endregion

        #region IPlayerController Implementation

        /// <summary>
        /// Reads keyboard input and sends movement commands to the player controller.
        /// </summary>
        public void UpdateControl()
        {
            Vector2 move = Vector2.zero;

            if (Input.GetKey(KeyCode.W)) move += Vector2.up;
            if (Input.GetKey(KeyCode.S)) move += Vector2.down;
            if (Input.GetKey(KeyCode.A)) move += Vector2.left;
            if (Input.GetKey(KeyCode.D)) move += Vector2.right;

            m_PlayerController.SetMoveDirection(move);
        }

        #endregion
    }
}