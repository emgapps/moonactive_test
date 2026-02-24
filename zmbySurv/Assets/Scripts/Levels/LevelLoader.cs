using System.Collections.Generic;
using Characters;
using Level.Data;
using Level.Providers;
using UnityEngine;
using Weapons.Combat;
using Weapons.Runtime;
using Weapons.UI;

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
        [SerializeField]
        private int m_DefaultZombieHealth = 20;
        [SerializeField]
        private float m_DefaultZombieColliderRadius = 0.45f;

        [Header("Weapon Selection")]
        [SerializeField]
        private bool m_RequireWeaponSelection = true;
        [SerializeField]
        private WeaponSelectionWindow m_WeaponSelectionWindow;

        #endregion

        #region Private Fields

        private LevelDataDto m_CurrentLevelData;
        private LevelCollectionDto m_LevelCollection;
        private int m_CurrentLevelIndex = 0;
        private List<EnemyController> m_SpawnedZombies = new List<EnemyController>();
        private ILevelDataProvider m_DataProvider;
        private bool m_IsLoading = false;
        private bool m_HasInitializedStartupFlow = false;

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
            LogVerbose(
                $"[LevelLoader] Startup | requireWeaponSelection={m_RequireWeaponSelection} hasSessionSelection={WeaponSelectionSession.HasSelection} hasSelectionWindow={(m_WeaponSelectionWindow != null)}");
            BeginStartupFlow();
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
            
            LoadLevel();
        }

        #endregion

        #region Private Helper Methods

        private void BeginStartupFlow()
        {
            if (m_HasInitializedStartupFlow)
            {
                LogVerbose("[LevelLoader] StartupFlowSkipped | reason=already_initialized");
                return;
            }

            m_HasInitializedStartupFlow = true;
            LogVerbose(
                $"[LevelLoader] StartupFlowBegin | requireWeaponSelection={m_RequireWeaponSelection} hasSessionSelection={WeaponSelectionSession.HasSelection} selectionWindow={BuildSelectionWindowState()}");

            if (!m_RequireWeaponSelection || WeaponSelectionSession.HasSelection)
            {
                LogVerbose(
                    $"[LevelLoader] StartupFlowLoadLevelDirectly | reason={(m_RequireWeaponSelection ? "selection_already_present" : "selection_not_required")}");
                LoadLevel();
                return;
            }

            if (m_WeaponSelectionWindow != null)
            {
                m_WeaponSelectionWindow.BeginSelection(
                    onConfirmed: HandleWeaponSelectionConfirmed,
                    onFailed: HandleWeaponSelectionFailed);
                return;
            }

            Debug.LogWarning("[LevelLoader] StartupFlowFallbackAutoSelect | reason=missing_selection_window_reference");
            AutoSelectDefaultWeapon();
            LoadLevel();
        }

        private void InitializeDataProvider()
        {
            switch (m_ProviderType)
            {
                case DataProviderType.Resources:
                    m_DataProvider = new ResourcesLevelDataProvider(m_ResourcesPath);
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

        private void HandleWeaponSelectionConfirmed()
        {
            LoadLevel();
        }

        private void HandleWeaponSelectionFailed(string errorMessage)
        {
            Debug.LogWarning(
                $"[LevelLoader] WeaponSelectionFallback | error={errorMessage} hasSelectionWindow={(m_WeaponSelectionWindow != null)}");
            AutoSelectDefaultWeapon();
            LoadLevel();
        }

        private void AutoSelectDefaultWeapon()
        {
            LogVerbose("[LevelLoader] WeaponAutoSelectBegin");
            WeaponCatalogService catalogService = WeaponCatalogService.CreateDefault();

            catalogService.LoadCatalog(
                onSuccess: catalog =>
                {
                    WeaponSelectionSession.SetCatalog(catalog);
                    if (!WeaponSelectionSession.TrySelectWeapon(catalog.DefaultWeaponId))
                    {
                        Debug.LogError(
                            $"[LevelLoader] WeaponAutoSelectionFailed | reason=invalid_default weaponId={catalog.DefaultWeaponId}");
                        return;
                    }

                },
                onError: error =>
                {
                    Debug.LogError($"[LevelLoader] WeaponAutoSelectionFailed | reason=catalog_load_error error={error}");
                });
        }

        private string BuildSelectionWindowState()
        {
            if (m_WeaponSelectionWindow == null)
            {
                return "null";
            }

            GameObject windowObject = m_WeaponSelectionWindow.gameObject;
            return
                $"name={m_WeaponSelectionWindow.name},selfActive={windowObject.activeSelf},hierarchyActive={windowObject.activeInHierarchy}";
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void LogVerbose(string message)
        {
            // Intentionally no-op: verbose trace logs were removed to keep only important logs.
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
                EnsureEnemyCombatCollider(enemyController.gameObject);
                EnsureEnemyDamageable(enemyController.gameObject);

                m_SpawnedZombies.Add(enemyController);
                spawnedCount += 1;
            }

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

        private void EnsureEnemyDamageable(GameObject enemyObject)
        {
            if (enemyObject == null)
            {
                return;
            }

            EnemyDamageable damageable = enemyObject.GetComponent<EnemyDamageable>();
            if (damageable == null)
            {
                damageable = enemyObject.AddComponent<EnemyDamageable>();
            }

            damageable.ConfigureHealth(m_DefaultZombieHealth);
        }

        private void EnsureEnemyCombatCollider(GameObject enemyObject)
        {
            if (enemyObject == null)
            {
                return;
            }

            Collider2D existingCollider = enemyObject.GetComponent<Collider2D>();
            if (existingCollider != null)
            {
                LogVerbose(
                    $"[LevelLoader] EnemyColliderReady | enemy={enemyObject.name} collider={DescribeCollider(existingCollider)}");
                return;
            }

            CircleCollider2D collider = enemyObject.AddComponent<CircleCollider2D>();
            collider.radius = ResolveEnemyColliderRadius(enemyObject);
            collider.isTrigger = true;
        }

        private float ResolveEnemyColliderRadius(GameObject enemyObject)
        {
            float fallbackRadius = Mathf.Max(0.05f, m_DefaultZombieColliderRadius);
            if (enemyObject == null)
            {
                return fallbackRadius;
            }

            SpriteRenderer spriteRenderer = enemyObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return fallbackRadius;
            }

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            float scaleX = Mathf.Abs(enemyObject.transform.lossyScale.x);
            float scaleY = Mathf.Abs(enemyObject.transform.lossyScale.y);
            float worldHalfWidth = spriteSize.x * scaleX * 0.5f;
            float worldHalfHeight = spriteSize.y * scaleY * 0.5f;
            float spriteRadius = Mathf.Min(worldHalfWidth, worldHalfHeight) * 0.5f;
            if (spriteRadius <= 0.0001f)
            {
                return fallbackRadius;
            }

            return Mathf.Max(0.05f, spriteRadius);
        }

        private static string DescribeCollider(Collider2D collider)
        {
            if (collider == null)
            {
                return "null";
            }

            string layerName = LayerMask.LayerToName(collider.gameObject.layer);
            if (string.IsNullOrWhiteSpace(layerName))
            {
                layerName = $"layer_{collider.gameObject.layer}";
            }

            return $"{collider.GetType().Name}(layer={layerName},trigger={collider.isTrigger})";
        }

        #endregion
    }
}
