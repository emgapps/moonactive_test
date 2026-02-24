using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EnemyAI.Tests.PlayMode
{
    /// <summary>
    /// Integration tests that verify enemy AI transitions in runtime lifecycle.
    /// </summary>
    public sealed class EnemyAIIntegrationTests
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
        public IEnumerator EnemyController_TransitionsFromPatrolToChase_WhenPlayerBecomesVisible()
        {
            PlayerController player = CreatePlayerController("IntegrationPlayer", new Vector2(20f, 0f), 5);
            EnemyController enemy = CreateEnemyController(
                "IntegrationEnemy",
                player,
                enemyPosition: Vector2.zero,
                sightRange: 5f,
                attackRange: 0.1f,
                attackPower: 1,
                patrolPoints: new List<Vector2> { new Vector2(-2f, 0f), new Vector2(2f, 0f) });

            yield return null;
            yield return null;

            Assert.That(enemy.CurrentStateName, Is.EqualTo("Patrol"));

            player.transform.position = new Vector2(1f, 0f);
            yield return WaitForCondition(() => enemy.CurrentStateName == "Chase", 1.5f);

            Assert.That(enemy.CurrentStateName, Is.EqualTo("Chase"));
        }

        [UnityTest]
        public IEnumerator EnemyController_TransitionsToAttack_AndDamagesPlayer_WhenInRange()
        {
            PlayerController player = CreatePlayerController("IntegrationPlayer", new Vector2(0.15f, 0f), 1);
            bool playerDied = false;
            player.OnPlayerDied += () => playerDied = true;

            EnemyController enemy = CreateEnemyController(
                "IntegrationEnemy",
                player,
                enemyPosition: Vector2.zero,
                sightRange: 5f,
                attackRange: 0.5f,
                attackPower: 1,
                patrolPoints: new List<Vector2> { Vector2.zero });

            yield return WaitForCondition(() => enemy.CurrentStateName == "Attack", 2f);
            yield return WaitForCondition(() => playerDied, 2f);

            Assert.That(enemy.CurrentStateName, Is.EqualTo("Attack"));
            Assert.That(playerDied, Is.True);
        }

        [UnityTest]
        public IEnumerator EnemyController_ReturnsToPatrol_WhenPlayerIsLostAfterChase()
        {
            PlayerController player = CreatePlayerController("IntegrationPlayer", new Vector2(1f, 0f), 5);
            EnemyController enemy = CreateEnemyController(
                "IntegrationEnemy",
                player,
                enemyPosition: Vector2.zero,
                sightRange: 5f,
                attackRange: 0.1f,
                attackPower: 1,
                patrolPoints: new List<Vector2> { new Vector2(-1f, 0f), new Vector2(1f, 0f) });

            yield return WaitForCondition(() => enemy.CurrentStateName == "Chase", 1.5f);
            Assert.That(enemy.CurrentStateName, Is.EqualTo("Chase"));

            player.transform.position = new Vector2(30f, 0f);
            yield return WaitForCondition(() => enemy.CurrentStateName == "Patrol", 2f);

            Assert.That(enemy.CurrentStateName, Is.EqualTo("Patrol"));
        }

        private IEnumerator WaitForCondition(System.Func<bool> condition, float timeoutSeconds)
        {
            float endTime = Time.time + timeoutSeconds;
            while (Time.time <= endTime)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }
        }

        private EnemyController CreateEnemyController(
            string name,
            PlayerController player,
            Vector2 enemyPosition,
            float sightRange,
            float attackRange,
            int attackPower,
            List<Vector2> patrolPoints)
        {
            GameObject enemyObject = new GameObject(name);
            m_CreatedObjects.Add(enemyObject);

            Rigidbody2D enemyRb = enemyObject.AddComponent<Rigidbody2D>();
            enemyRb.gravityScale = 0f;
            enemyRb.freezeRotation = true;

            enemyObject.AddComponent<Animator>();
            EnemyController enemy = enemyObject.AddComponent<EnemyController>();
            enemy.transform.position = enemyPosition;
            SetPrivateField(enemy, "rb", enemyRb);
            SetPrivateField(enemy, "m_Animator", enemyObject.GetComponent<Animator>());

            enemy.Player = player;
            enemy.ApplyLevelConfiguration(
                moveSpeed: 2f,
                chaseSpeed: 4f,
                sightRange: sightRange,
                attackRange: attackRange,
                attackPower: attackPower,
                patrolPoints: patrolPoints);

            return enemy;
        }

        private PlayerController CreatePlayerController(string name, Vector2 position, int health)
        {
            GameObject playerObject = new GameObject(name);
            m_CreatedObjects.Add(playerObject);

            Rigidbody2D playerRb = playerObject.AddComponent<Rigidbody2D>();
            playerRb.gravityScale = 0f;
            playerRb.freezeRotation = true;

            playerObject.AddComponent<Animator>();
            PlayerController player = playerObject.AddComponent<PlayerController>();
            player.transform.position = position;
            SetPrivateField(player, "m_Rigidbody2D", playerRb);
            SetPrivateField(player, "m_Animator", playerObject.GetComponent<Animator>());
            player.ApplyLevelConfiguration(5f, health, 1);

            return player;
        }

        private static void SetPrivateField(object target, string fieldName, object fieldValue)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Missing private field '{fieldName}'.");
            fieldInfo.SetValue(target, fieldValue);
        }
    }
}
