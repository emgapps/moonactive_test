using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Characters;
using Coins;
using Level.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace ObjectPooling.Tests.PlayMode
{
    /// <summary>
    /// Play mode integration tests for coin pooling flow.
    /// </summary>
    public sealed class CoinPoolingIntegrationTests
    {
        private readonly List<GameObject> m_CreatedObjects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int index = 0; index < m_CreatedObjects.Count; index++)
            {
                if (m_CreatedObjects[index] != null)
                {
                    Object.Destroy(m_CreatedObjects[index]);
                }
            }

            m_CreatedObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoinSpawner_ReusesCoinInstance_AfterCollection()
        {
            CoinSpawner spawner = CreateSpawnerEnvironment(
                maxCoinsOnBoard: 1,
                targetCurrency: 10,
                out BoxCollider2D playerCollider);

            yield return null;

            LevelDataDto levelData = CreateLevelData("reuse-after-collection", 1);
            spawner.OnLevelLoaded(levelData);
            yield return null;

            Coin firstCoin = GetFirstActiveCoin(spawner);
            Assert.That(firstCoin, Is.Not.Null);

            TriggerCoinCollection(firstCoin, playerCollider);
            yield return null;

            Coin secondCoin = GetFirstActiveCoin(spawner);
            Assert.That(secondCoin, Is.Not.Null);
            Assert.That(secondCoin, Is.SameAs(firstCoin));
            Assert.That(CountActiveCoins(spawner), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CoinSpawner_ReusesPooledCoins_AcrossLevelTransition()
        {
            CoinSpawner spawner = CreateSpawnerEnvironment(
                maxCoinsOnBoard: 2,
                targetCurrency: 20,
                out _);

            yield return null;

            LevelDataDto levelData = CreateLevelData("transition-reuse", 2);
            spawner.OnLevelLoaded(levelData);
            yield return null;

            int initialChildCount = spawner.transform.childCount;
            Assert.That(CountActiveCoins(spawner), Is.EqualTo(2));

            spawner.ClearAllCoins();
            yield return null;

            Assert.That(CountActiveCoins(spawner), Is.EqualTo(0));
            Assert.That(spawner.transform.childCount, Is.EqualTo(initialChildCount));

            spawner.OnLevelLoaded(levelData);
            yield return null;

            Assert.That(CountActiveCoins(spawner), Is.EqualTo(2));
            Assert.That(spawner.transform.childCount, Is.EqualTo(initialChildCount));
        }

        private CoinSpawner CreateSpawnerEnvironment(
            int maxCoinsOnBoard,
            int targetCurrency,
            out BoxCollider2D playerCollider)
        {
            PlayerController player = CreatePlayer(targetCurrency, out playerCollider);
            Tilemap boundsTilemap = CreateBoundsTilemap();
            Coin coinPrefab = CreateCoinPrefab();

            GameObject spawnerObject = new GameObject("PoolingSpawner");
            m_CreatedObjects.Add(spawnerObject);
            spawnerObject.SetActive(false);

            CoinSpawner spawner = spawnerObject.AddComponent<CoinSpawner>();
            SetPrivateField(spawner, "coinPrefab", coinPrefab);
            SetPrivateField(spawner, "maxCoinsOnBoard", maxCoinsOnBoard);
            SetPrivateField(spawner, "boundsTilemap", boundsTilemap);
            SetPrivateField(spawner, "m_PlayerController", player);

            spawnerObject.SetActive(true);
            return spawner;
        }

        private PlayerController CreatePlayer(int targetCurrency, out BoxCollider2D playerCollider)
        {
            GameObject playerObject = new GameObject("PoolingPlayer");
            m_CreatedObjects.Add(playerObject);

            Rigidbody2D playerRigidbody = playerObject.AddComponent<Rigidbody2D>();
            playerRigidbody.gravityScale = 0f;
            playerRigidbody.freezeRotation = true;

            Animator playerAnimator = playerObject.AddComponent<Animator>();
            playerCollider = playerObject.AddComponent<BoxCollider2D>();
            playerCollider.isTrigger = false;

            PlayerController player = playerObject.AddComponent<PlayerController>();
            SetPrivateField(player, "m_Rigidbody2D", playerRigidbody);
            SetPrivateField(player, "m_Animator", playerAnimator);
            player.ApplyLevelConfiguration(5f, 20, targetCurrency);

            return player;
        }

        private Tilemap CreateBoundsTilemap()
        {
            GameObject gridObject = new GameObject("PoolingGrid");
            m_CreatedObjects.Add(gridObject);
            gridObject.AddComponent<Grid>();

            GameObject tilemapObject = new GameObject("PoolingBoundsTilemap");
            m_CreatedObjects.Add(tilemapObject);
            tilemapObject.transform.SetParent(gridObject.transform, false);

            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapRenderer>();
            return tilemap;
        }

        private Coin CreateCoinPrefab()
        {
            GameObject coinObject = new GameObject("PoolingCoinPrefab");
            m_CreatedObjects.Add(coinObject);
            coinObject.transform.position = new Vector3(1000f, 1000f, 0f);

            CircleCollider2D coinCollider = coinObject.AddComponent<CircleCollider2D>();
            coinCollider.isTrigger = true;
            Coin coinPrefab = coinObject.AddComponent<Coin>();
            coinObject.SetActive(false);
            return coinPrefab;
        }

        private static LevelDataDto CreateLevelData(string levelId, int maxCoinsOnBoard)
        {
            return new LevelDataDto
            {
                levelId = levelId,
                maxCoinsOnBoard = maxCoinsOnBoard
            };
        }

        private static Coin GetFirstActiveCoin(CoinSpawner spawner)
        {
            for (int childIndex = 0; childIndex < spawner.transform.childCount; childIndex++)
            {
                Transform child = spawner.transform.GetChild(childIndex);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                Coin coin = child.GetComponent<Coin>();
                if (coin != null)
                {
                    return coin;
                }
            }

            return null;
        }

        private static int CountActiveCoins(CoinSpawner spawner)
        {
            int activeCoinCount = 0;
            for (int childIndex = 0; childIndex < spawner.transform.childCount; childIndex++)
            {
                Transform child = spawner.transform.GetChild(childIndex);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                if (child.GetComponent<Coin>() != null)
                {
                    activeCoinCount += 1;
                }
            }

            return activeCoinCount;
        }

        private static void TriggerCoinCollection(Coin coin, Collider2D playerCollider)
        {
            MethodInfo triggerMethod = typeof(Coin).GetMethod(
                "OnTriggerEnter2D",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(triggerMethod, Is.Not.Null, "Coin.OnTriggerEnter2D was not found.");
            triggerMethod.Invoke(coin, new object[] { playerCollider });
        }

        private static void SetPrivateField(object target, string fieldName, object fieldValue)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Missing private field '{fieldName}'.");
            fieldInfo.SetValue(target, fieldValue);
        }
    }
}
