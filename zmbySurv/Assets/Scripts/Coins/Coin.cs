using Characters;
using UnityEngine;

namespace Coins
{
    /// <summary>
    /// Represents a collectible coin that grants currency to the player.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Coin : MonoBehaviour
    {
        #region Public Fields

        /// <summary>
        /// The amount of currency this coin provides when collected.
        /// </summary>
        private int value = 1;

        #endregion

        #region Private Fields

        /// <summary>
        /// Reference to the spawner for recycling the coin back to the pool.
        /// </summary>
        private CoinSpawner m_Spawner;

        #endregion

        #region Public API Methods

        /// <summary>
        /// Sets the spawner reference for this coin instance.
        /// </summary>
        /// <param name="spawner">The CoinSpawner that manages this coin.</param>
        public void SetSpawner(CoinSpawner spawner)
        {
            m_Spawner = spawner;
        }

        #endregion

        #region Unity Collision Methods

        /// <summary>
        /// Handles collision with the player.
        /// </summary>
        /// <param name="other">The collider that triggered the collision.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                player.AddCurrency(value);
                Destroy(gameObject);
            }
        }

        #endregion
    }
}