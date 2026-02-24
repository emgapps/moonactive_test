using Characters;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace Level
{
    /// <summary>
    /// Manages the home appearance and positioning.
    /// Subscribes to player currency events to show the home when target is reached.
    /// </summary>
    public class HomeManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField]
        private PlayerController m_PlayerController;
        [SerializeField]
        private Home m_Home;

        [Header("Spawn Settings")]
        [SerializeField]
        private Tilemap boundsTilemap;
        [SerializeField]
        private bool m_SpawnRandomly = true;
        [SerializeField]
        private Vector3 m_FixedSpawnPosition = Vector3.zero;

        #endregion

        #region Private Fields

        private Bounds m_TilemapBounds;
        private bool m_HomeActivated = false;

        #endregion

        #region Unity Lifecycle Methods

        /// <summary>
        /// Initializes the home manager and subscribes to player events.
        /// </summary>
        private void Start()
        {
            // Hide the home initially
            if (m_Home != null)
            {
                m_Home.Hide();
            }

            // Calculate spawn area bounds if spawning randomly
            if (m_SpawnRandomly && boundsTilemap != null)
            {
                boundsTilemap.CompressBounds();
                m_TilemapBounds = boundsTilemap.localBounds;
            }

            // Subscribe to player currency change events
            if (m_PlayerController != null)
            {
                m_PlayerController.OnCurrencyChanged += HandleCurrencyChanged;
                m_PlayerController.OnTargetCurrencyReached += HandleTargetCurrencyReached;
            }
            else
            {
                Debug.LogError("HomeManager: PlayerController is not assigned!");
            }
        }

        /// <summary>
        /// Unsubscribes from events to prevent memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            if (m_PlayerController != null)
            {
                m_PlayerController.OnCurrencyChanged -= HandleCurrencyChanged;
                m_PlayerController.OnTargetCurrencyReached -= HandleTargetCurrencyReached;
            }
        }

        #endregion

        #region Private Event Handlers

        /// <summary>
        /// Event handler for currency changes.
        /// Checks if the target currency has been reached.
        /// </summary>
        /// <param name="newAmount">The player's new currency amount.</param>
        private void HandleCurrencyChanged(int newAmount)
        {
            // This is now handled by OnTargetCurrencyReached event
            // Kept for potential future use
        }

        /// <summary>
        /// Event handler for when target currency is reached.
        /// Spawns and shows the home at the appropriate location.
        /// </summary>
        private void HandleTargetCurrencyReached()
        {
            if (!m_HomeActivated)
            {
                m_HomeActivated = true;
                SpawnHome();
            }
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Hides and destroys the home object if it exists.
        /// Called when transitioning to a new level.
        /// </summary>
        public void HideHome()
        {
            if (m_Home != null)
            {
                m_Home.Hide();
                m_HomeActivated = false;
                Debug.Log("HomeManager: Home hidden for level transition");
            }
        }

        /// <summary>
        /// Manually spawns the home (for testing purposes).
        /// </summary>
        public void ForceSpawnHome()
        {
            if (!m_HomeActivated)
            {
                m_HomeActivated = true;
                SpawnHome();
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Spawns the home at a random or fixed position and shows it.
        /// </summary>
        private void SpawnHome()
        {
            if (m_Home == null)
            {
                Debug.LogError("HomeManager: Home reference is not assigned!");
                return;
            }

            // Set home position
            if (m_SpawnRandomly)
            {
                m_Home.transform.position = GetRandomPositionInBounds();
            }
            else
            {
                m_Home.transform.position = m_FixedSpawnPosition;
            }

            // Show the home
            m_Home.Show();

            Debug.Log($"Home spawned at position: {m_Home.transform.position}");
        }

        /// <summary>
        /// Calculates a random position constrained by the tilemap's world bounds.
        /// </summary>
        /// <returns>A random Vector3 position within the tilemap bounds.</returns>
        private Vector3 GetRandomPositionInBounds()
        {
            float randomX = Random.Range(m_TilemapBounds.min.x, m_TilemapBounds.max.x);
            float randomY = Random.Range(m_TilemapBounds.min.y, m_TilemapBounds.max.y);
            return new Vector3(randomX, randomY, 0);
        }

        #endregion
    }
}

