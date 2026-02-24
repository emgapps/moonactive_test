using Characters;
using Core.Pooling;
using UnityEngine;

namespace Coins
{
    /// <summary>
    /// Represents a collectible coin that grants currency to the player.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Coin : MonoBehaviour, IPoolable
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
        private bool m_IsCollected;

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

        /// <summary>
        /// Resets coin state when taken from pool.
        /// </summary>
        public void OnTakenFromPool()
        {
            m_IsCollected = false;
        }

        /// <summary>
        /// Marks coin as collected when returned to pool.
        /// </summary>
        public void OnReturnedToPool()
        {
            m_IsCollected = true;
        }

        #endregion

        #region Unity Collision Methods

        /// <summary>
        /// Handles collision with the player.
        /// </summary>
        /// <param name="other">The collider that triggered the collision.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (m_IsCollected)
            {
                return;
            }

            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                m_IsCollected = true;

                if (m_Spawner != null)
                {
                    m_Spawner.ReturnCoinToPool(this);
                }
                else
                {
                    Debug.LogWarning($"[Coin] ReturnFallbackDestroy | coin={name} reason=missing_spawner");
                    Destroy(gameObject);
                }

                player.AddCurrency(value);
            }
        }

        #endregion
    }
}
