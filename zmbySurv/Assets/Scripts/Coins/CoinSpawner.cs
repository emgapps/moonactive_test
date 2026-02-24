using System;
using Characters;
using Coins;
using Level.Data;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace Coins
{
    /// <summary>
    /// Spawns and maintains collectible coins based on level configuration.
    /// </summary>
    public class CoinSpawner : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Configuration")]
        [SerializeField]
        private Coin coinPrefab;
        [SerializeField]
        private int maxCoinsOnBoard = 5;

        [Header("Environment")]
        [SerializeField]
        private Tilemap boundsTilemap;

        [Header("References")]
        [SerializeField]
        private PlayerController m_PlayerController;

        #endregion

        #region Private Fields

        private Bounds m_TilemapBounds;
        private int m_CoinAmount = 0;
        private bool m_Spawn = true;

        #endregion

        #region Unity Lifecycle Methods

        private void Awake()
        {
        }

        private void Start()
        {
            if (boundsTilemap == null)
            {
                Debug.LogError("CoinSpawner: 'boundsTilemap' is not assigned!");
                return;
            }

            boundsTilemap.CompressBounds();
            m_TilemapBounds = boundsTilemap.localBounds;

            if (m_PlayerController != null)
            {
                m_PlayerController.OnCurrencyChanged += HandleCoinCollected;
            }
        }

        private void OnDestroy()
        {
            if (m_PlayerController != null)
            {
                m_PlayerController.OnCurrencyChanged -= HandleCoinCollected;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enables or disables coin spawning.
        /// </summary>
        /// <param name="state">True to allow spawning, false to stop spawning.</param>
        public void SetSpawnerState(bool state)
        {
            m_Spawn = state;
        }

        /// <summary>
        /// Called when a new level is loaded.
        /// </summary>
        /// <param name="levelData">Loaded level data.</param>
        public void OnLevelLoaded(LevelDataDto levelData)
        {
            if (levelData == null)
            {
                Debug.LogError("[CoinSpawner] LevelLoadFailed | reason=null_level_data");
                return;
            }

            if (levelData.maxCoinsOnBoard <= 0)
            {
                Debug.LogWarning(
                    $"[CoinSpawner] InvalidLevelConfig | levelId={levelData.levelId} maxCoinsOnBoard={levelData.maxCoinsOnBoard} fallback=1");
                maxCoinsOnBoard = 1;
            }
            else
            {
                maxCoinsOnBoard = levelData.maxCoinsOnBoard;
            }

            m_CoinAmount = 0;
            m_Spawn = true;

            Debug.Log($"[CoinSpawner] LevelLoaded | levelId={levelData.levelId} maxCoinsOnBoard={maxCoinsOnBoard}");

            SpawnInitialCoins();
        }

        /// <summary>
        /// Clears all active coins and disables spawning until next level data arrives.
        /// </summary>
        public void ClearAllCoins()
        {
            for (int childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
            {
                Destroy(transform.GetChild(childIndex).gameObject);
            }

            m_CoinAmount = 0;
            m_Spawn = false;
            
            Debug.Log("[CoinSpawner] CoinsCleared | reason=level_transition");
        }

        private void SpawnInitialCoins()
        {
            int coinsToSpawn = maxCoinsOnBoard;
            
            for (int i = 0; i < coinsToSpawn; i++)
            {
                if (CanSpawnMoreCoins())
                {
                    SpawnNewCoin();
                }
            }
            
            Debug.Log($"[CoinSpawner] InitialSpawnCompleted | spawned={m_CoinAmount}");
        }

        #endregion

        #region Private Helper Methods

        private void SpawnNewCoin()
        {
            Coin newCoin = Instantiate(coinPrefab, transform);
            newCoin.SetSpawner(this);

            Vector3 spawnPosition = GetRandomPositionInBounds();
            newCoin.transform.position = spawnPosition;

            m_CoinAmount++;
        }

        private Vector3 GetRandomPositionInBounds()
        {
            float randomX = Random.Range(m_TilemapBounds.min.x, m_TilemapBounds.max.x);
            float randomY = Random.Range(m_TilemapBounds.min.y, m_TilemapBounds.max.y);
            return new Vector3(randomX, randomY, 0);
        }

        private void HandleCoinCollected(int newAmount)
        {
            m_CoinAmount--;

            if (m_CoinAmount < maxCoinsOnBoard && m_Spawn && CanSpawnMoreCoins())
            {
                SpawnNewCoin();
            }
        }

        private bool CanSpawnMoreCoins()
        {
            if (m_PlayerController == null)
            {
                return true;
            }

            int currentCurrency = m_PlayerController.CurrentCurrency;
            int targetCurrency = m_PlayerController.TargetCurrency;
            int totalAvailable = currentCurrency + m_CoinAmount;

            bool canSpawn = totalAvailable < targetCurrency;
            
            if (!canSpawn)
            {
                Debug.Log($"CoinSpawner: Not spawning more coins. Current: {currentCurrency}, On screen: {m_CoinAmount}, Target: {targetCurrency}");
            }
            
            return canSpawn;
        }

        #endregion
    }
}
