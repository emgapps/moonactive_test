using System.Collections.Generic;
using Characters;
using Level.Data;
using Level.Providers;
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

        private LevelDataDto m_CurrentLevelData;
        private LevelCollectionDto m_LevelCollection;
        private int m_CurrentLevelIndex = 0;
        private List<EnemyController> m_SpawnedZombies = new List<EnemyController>();
        private ILevelDataProvider m_DataProvider;
        private bool m_IsLoading = false;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets currently active level data.
        /// </summary>
        public LevelDataDto CurrentLevelData => m_CurrentLevelData;

        /// <summary>
        /// Gets loaded level collection.
        /// </summary>
        public LevelCollectionDto CurrentCollection => m_LevelCollection;

        /// <summary>
        /// Gets whether the currently active level is the last level in the collection.
        /// </summary>
        /// <returns>True when current level is the last configured level.</returns>
        public bool IsLastLevel()
        {
            if (m_LevelCollection == null || m_LevelCollection.levels == null || m_LevelCollection.levels.Count == 0)
            {
                return true;
            }

            return m_CurrentLevelIndex >= m_LevelCollection.levels.Count - 1;
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

            if (!TryResolveCurrentLevelData())
            {
                return;
            }

            Debug.Log(
                $"[LevelLoader] LoadLevel | index={m_CurrentLevelIndex} levelId={m_CurrentLevelData.levelId} levelName={m_CurrentLevelData.levelName}");

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

            if (m_LevelCollection.levels == null || m_LevelCollection.levels.Count == 0)
            {
                Debug.LogError("LevelLoader: Level collection is empty.");
                return false;
            }

            if (m_CurrentLevelIndex >= m_LevelCollection.levels.Count - 1)
            {
                Debug.Log("[LevelLoader] LoadNextLevelSkipped | reason=already_last_level");
                return false;
            }

            m_CurrentLevelIndex += 1;
            
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
            switch (m_ProviderType)
            {
                case DataProviderType.Resources:
                    m_DataProvider = new ResourcesLevelDataProvider(m_ResourcesPath);
                    Debug.Log($"[LevelLoader] DataProviderInitialized | type=resources path={m_ResourcesPath}");
                    break;
                case DataProviderType.Server:
                    Debug.LogWarning(
                        $"[LevelLoader] ServerProviderUnavailable | fallback=resources url={m_ServerUrl} timeout={m_ServerTimeoutSeconds}");
                    m_DataProvider = new ResourcesLevelDataProvider(m_ResourcesPath);
                    break;
                default:
                    Debug.LogWarning($"[LevelLoader] UnknownProviderType | value={m_ProviderType} fallback=resources");
                    m_DataProvider = new ResourcesLevelDataProvider(m_ResourcesPath);
                    break;
            }
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

        private void OnLevelsDataLoaded(LevelCollectionDto levelCollection)
        {
            if (levelCollection == null)
            {
                Debug.LogError("LevelLoader: Invalid level collection - no levels found!");
                return;
            }
            
            m_LevelCollection = levelCollection;
            if (m_CurrentLevelIndex < 0 || m_CurrentLevelIndex >= m_LevelCollection.levels.Count)
            {
                m_CurrentLevelIndex = 0;
            }

            Debug.Log($"[LevelLoader] LevelsLoaded | count={m_LevelCollection.levels.Count}");

            LoadLevel();
        }

        private void ApplyPlayerConfiguration()
        {
            if (m_CurrentLevelData == null)
            {
                Debug.LogError("[LevelLoader] ApplyPlayerConfigurationFailed | reason=missing_current_level");
                return;
            }

            if (playerController == null)
            {
                Debug.LogError("[LevelLoader] ApplyPlayerConfigurationFailed | reason=missing_player_controller");
                return;
            }

            if (m_CurrentLevelData.playerConfig == null || m_CurrentLevelData.playerConfig.spawnPosition == null)
            {
                Debug.LogError(
                    $"[LevelLoader] ApplyPlayerConfigurationFailed | reason=invalid_player_config levelId={m_CurrentLevelData.levelId}");
                return;
            }

            PlayerConfigDto playerConfig = m_CurrentLevelData.playerConfig;
            playerController.transform.position = playerConfig.spawnPosition.ToUnityVector3();
            playerController.ApplyLevelConfiguration(playerConfig.speed, playerConfig.health, m_CurrentLevelData.goalCoins);

            Debug.Log(
                $"[LevelLoader] PlayerConfigured | levelId={m_CurrentLevelData.levelId} speed={playerConfig.speed} health={playerConfig.health} targetCoins={m_CurrentLevelData.goalCoins}");
        }

        private void SpawnZombies()
        {
            if (m_CurrentLevelData == null)
            {
                Debug.LogError("[LevelLoader] SpawnZombiesFailed | reason=missing_current_level");
                return;
            }

            if (zombiePrefab == null)
            {
                Debug.LogError("[LevelLoader] SpawnZombiesFailed | reason=missing_zombie_prefab");
                return;
            }

            if (playerController == null)
            {
                Debug.LogError("[LevelLoader] SpawnZombiesFailed | reason=missing_player_controller");
                return;
            }

            if (m_CurrentLevelData.zombies == null)
            {
                Debug.LogError($"[LevelLoader] SpawnZombiesFailed | reason=null_zombie_collection levelId={m_CurrentLevelData.levelId}");
                return;
            }

            int spawnedCount = 0;
            for (int zombieIndex = 0; zombieIndex < m_CurrentLevelData.zombies.Count; zombieIndex++)
            {
                ZombieConfigDto zombieConfig = m_CurrentLevelData.zombies[zombieIndex];
                if (zombieConfig == null || zombieConfig.spawnPosition == null)
                {
                    Debug.LogWarning(
                        $"[LevelLoader] ZombieSkipped | levelId={m_CurrentLevelData.levelId} index={zombieIndex} reason=invalid_config");
                    continue;
                }

                Transform parentTransform = zombieParent != null ? zombieParent : transform;
                GameObject zombieObject = Instantiate(
                    zombiePrefab,
                    zombieConfig.spawnPosition.ToUnityVector3(),
                    Quaternion.identity,
                    parentTransform);

                EnemyController enemyController = zombieObject.GetComponent<EnemyController>();
                if (enemyController == null)
                {
                    Debug.LogError(
                        $"[LevelLoader] ZombieSpawnFailed | levelId={m_CurrentLevelData.levelId} zombieId={zombieConfig.zombieId} reason=missing_enemy_controller");
                    Destroy(zombieObject);
                    continue;
                }

                enemyController.Player = playerController;
                enemyController.ApplyLevelConfiguration(
                    zombieConfig.moveSpeed,
                    zombieConfig.chaseSpeed,
                    zombieConfig.detectDistance,
                    zombieConfig.attackRange,
                    zombieConfig.attackPower,
                    BuildPatrolPoints(zombieConfig));

                m_SpawnedZombies.Add(enemyController);
                spawnedCount += 1;
            }

            Debug.Log(
                $"[LevelLoader] ZombiesSpawned | levelId={m_CurrentLevelData.levelId} spawned={spawnedCount} configured={m_CurrentLevelData.zombies.Count}");
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

        private bool TryResolveCurrentLevelData()
        {
            if (m_LevelCollection == null || m_LevelCollection.levels == null || m_LevelCollection.levels.Count == 0)
            {
                Debug.LogError("[LevelLoader] ResolveLevelFailed | reason=empty_collection");
                return false;
            }

            if (m_CurrentLevelIndex < 0 || m_CurrentLevelIndex >= m_LevelCollection.levels.Count)
            {
                Debug.LogError(
                    $"[LevelLoader] ResolveLevelFailed | reason=index_out_of_range index={m_CurrentLevelIndex} count={m_LevelCollection.levels.Count}");
                return false;
            }

            m_CurrentLevelData = m_LevelCollection.levels[m_CurrentLevelIndex];
            if (m_CurrentLevelData == null)
            {
                Debug.LogError($"[LevelLoader] ResolveLevelFailed | reason=null_level_data index={m_CurrentLevelIndex}");
                return false;
            }

            return true;
        }

        private List<Vector2> BuildPatrolPoints(ZombieConfigDto zombieConfig)
        {
            List<Vector2> patrolPoints = new List<Vector2>();
            if (zombieConfig.patrolPath != null)
            {
                for (int pathIndex = 0; pathIndex < zombieConfig.patrolPath.Count; pathIndex++)
                {
                    Vector2Dto pathPoint = zombieConfig.patrolPath[pathIndex];
                    if (pathPoint != null)
                    {
                        patrolPoints.Add(pathPoint.ToUnityVector2());
                    }
                }
            }

            if (patrolPoints.Count == 0 && zombieConfig.spawnPosition != null)
            {
                patrolPoints.Add(zombieConfig.spawnPosition.ToUnityVector2());
            }

            return patrolPoints;
        }

        #endregion
    }
}
