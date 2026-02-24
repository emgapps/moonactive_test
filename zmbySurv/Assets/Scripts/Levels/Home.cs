using Characters;
using UnityEngine;

namespace Level
{
    /// <summary>
    /// Represents the home that appears when the player collects enough coins.
    /// Detects when the player reaches the home to complete the level.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Home : MonoBehaviour
    {
        #region Events

        /// <summary>
        /// Delegate for home reached events.
        /// </summary>
        public delegate void HomeReached();

        /// <summary>
        /// Event triggered when the player reaches the home.
        /// Subscribers can use this to trigger level completion logic.
        /// </summary>
        public event HomeReached OnHomeReached;

        #endregion

        #region Unity Collision Methods

        /// <summary>
        /// Handles collision with the player.
        /// Triggers the OnHomeReached event when player enters the home.
        /// </summary>
        /// <param name="other">The collider that triggered the collision.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                Debug.Log("Player reached home! Level Complete!");
                OnHomeReached?.Invoke();
            }
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Shows the home by activating the GameObject.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the home by deactivating the GameObject.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}

