using System.Collections.Generic;
using System.IO;
using Characters;
using UnityEngine;

namespace Level
{
    /// <summary>
    /// Loads level configuration from JSON files and applies settings to game objects.
    /// Manages level data parsing and distribution to relevant controllers.
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// Type of data provider to use for loading levels.
        /// </summary>
        public enum DataProviderType
        {
            Resources,
            Server
        }

        #endregion

        #region Serialized Fields

        [Header("Data Provider Configuration")]
        [SerializeField]
        private DataProviderType m_ProviderType = DataProviderType.Resources;
        
        [Header("Resources Configuration")]
        [SerializeField]
        private string m_ResourcesPath = "Levels/Levels";
        
        [Header("Server Configuration")]
        [SerializeField]
        private string m_ServerUrl = "http://localhost:3000/api/levels";
        [SerializeField]
        private int m_ServerTimeoutSeconds = 30;

        [Header("Game References")]
        [SerializeField]
        private PlayerController playerController;
        [SerializeField]
        private GameObject zombiePrefab;
        [SerializeField]
        private Transform zombieParent;
        [SerializeField]
        private Coins.CoinSpawner coinSpawner;
        [SerializeField]
        private HomeManager homeManager;

        #endregion

        #region Private Fields

        private object m_CurrentLevelData;
        private object m_LevelCollection;
        private int m_CurrentLevelIndex = 0;
        private List<EnemyController> m_SpawnedZombies = new List<EnemyController>();
        private ILevelDataProvider m_DataProvider;
        private bool m_IsLoading = false;

        #endregion

        #region Public Properties

        public object CurrentLevelData => m_CurrentLevelData;

        public object CurrentCollection => m_LevelCollection;

        public bool IsLastLevel()
        {
            return true;
        }

        #endregion

        #region Unity Lifecycle Methods

        /// <summary>
        /// Initializes the data provider and loads the first level.
        /// </summary>
        private void Start()
        {
            InitializeDataProvider();
            LoadLevel();
        }

        #endregion

        #region Public API Methods

        public void LoadLevel()
        {
            if (m_IsLoading)
            {
                Debug.LogWarning("LevelLoader: Already loading level data...");
                return;
            }

            if (m_LevelCollection == null)
            {
                LoadLevelsCollection();
                return;
            }

            Debug.Log($"LevelLoader: Loading level {m_CurrentLevelIndex + 1}");

            CleanupLevel();
            ApplyPlayerConfiguration();
            SpawnZombies();
            
            StartCoroutine(SpawnCoinsAfterPlayerPositioned());
        }

        /// <summary>
        /// Spawns coins after a frame delay to ensure player is positioned correctly.
        /// Prevents coins from spawning on player's initial position.
        /// </summary>
        private System.Collections.IEnumerator SpawnCoinsAfterPlayerPositioned()
        {
            // Wait one frame to ensure player's transform.position has been applied
            yield return null;
            
            // Now spawn coins with player in correct position
            BroadcastLevelData();
        }

        public bool LoadNextLevel()
        {
            if (m_LevelCollection == null)
            {
                Debug.LogError("LevelLoader: No levels loaded yet!");
                return false;
            }

            m_CurrentLevelIndex++;
            
            LoadLevel();
            return true;
        }

        public void ReloadCurrentLevel()
        {
            if (m_LevelCollection == null)
            {
                Debug.LogError("LevelLoader: No levels loaded yet!");
                return;
            }
            
            Debug.Log($"LevelLoader: Reloading current level (index {m_CurrentLevelIndex})");
            LoadLevel();
        }

        #endregion

        #region Private Helper Methods

        private void InitializeDataProvider()
        {
            Debug.LogError("LevelLoader: InitializeDataProvider() not implemented!");
        }

        /// <summary>
        /// Loads the levels collection from the configured data provider.
        /// </summary>
        private void LoadLevelsCollection()
        {
            if (m_DataProvider == null)
            {
                Debug.LogError("LevelLoader: No data provider initialized!");
                return;
            }

            m_IsLoading = true;

            m_DataProvider.LoadLevelsData(
                onSuccess: (levelCollection) =>
                {
                    m_IsLoading = false;
                    OnLevelsDataLoaded(levelCollection);
                },
                onError: (errorMessage) =>
                {
                    m_IsLoading = false;
                    Debug.LogError($"LevelLoader: Failed to load levels data: {errorMessage}");
                }
            );
        }

        private void OnLevelsDataLoaded(object levelCollection)
        {
            if (levelCollection == null)
            {
                Debug.LogError("LevelLoader: Invalid level collection - no levels found!");
                return;
            }
            
            m_LevelCollection = levelCollection;
            Debug.Log($"LevelLoader: Successfully loaded levels");

            LoadLevel();
        }

        private void ApplyPlayerConfiguration()
        {
            Debug.LogError("LevelLoader: ApplyPlayerConfiguration() not implemented!");
        }

        private void SpawnZombies()
        {
            Debug.LogError("LevelLoader: SpawnZombies() not implemented!");
        }

        /// <summary>
        /// Cleans up the entire previous level state.
        /// Removes zombies, hides home, clears coins, and resets player.
        /// </summary>
        private void CleanupLevel()
        {
            // Clean up zombies
            foreach (EnemyController zombie in m_SpawnedZombies)
            {
                if (zombie != null)
                {
                    Destroy(zombie.gameObject);
                }
            }
            m_SpawnedZombies.Clear();

            // Hide/destroy home if it exists
            if (homeManager != null)
            {
                homeManager.HideHome();
            }

            // Clear all coins
            if (coinSpawner != null)
            {
                coinSpawner.ClearAllCoins();
            }

            // Reset player currency (will be reset again in ApplyPlayerConfiguration, but good to be explicit)
            if (playerController != null)
            {
                playerController.ResetCurrency();
            }
        }

        /// <summary>
        /// Broadcasts the level data to other systems that need it.
        /// </summary>
        private void BroadcastLevelData()
        {
            // Notify coin spawner about new level
            if (coinSpawner != null)
            {
                coinSpawner.OnLevelLoaded(m_CurrentLevelData);
            }
            else
            {
                Debug.LogWarning("LevelLoader: CoinSpawner reference not set in inspector!");
            }
        }

        #endregion
    }
}

