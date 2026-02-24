using System.Collections.Generic;
using System.Reflection;
using Characters;
using Characters.EnemyAI;
using NUnit.Framework;
using UnityEngine;

namespace EnemyAI.Tests.EditMode
{
    /// <summary>
    /// Unit tests for patrol, chase, and attack state behavior.
    /// </summary>
    public sealed class EnemyStatesTests
    {
        private readonly List<GameObject> m_CreatedObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < m_CreatedObjects.Count; index++)
            {
                if (m_CreatedObjects[index] != null)
                {
                    Object.DestroyImmediate(m_CreatedObjects[index]);
                }
            }

            m_CreatedObjects.Clear();
        }

        [Test]
        public void PatrolState_WhenPlayerVisible_InvokesDetectionCallback()
        {
            EnemyController enemy = CreateConfiguredEnemy(
                enemyPosition: Vector2.zero,
                playerPosition: Vector2.zero,
                sightRange: 5f,
                attackRange: 0.2f,
                attackPower: 1,
                patrolPoints: new List<Vector2> { new Vector2(2f, 0f) });
            EnemyStateContext context = new EnemyStateContext(enemy);
            bool wasDetected = false;
            PatrolState state = new PatrolState(context, () => wasDetected = true);

            state.Enter();
            state.Tick(0.016f);

            Assert.That(wasDetected, Is.True);
        }

        [Test]
        public void PatrolState_WhenPlayerNotVisible_MovesTowardPatrolPoint()
        {
            EnemyController enemy = CreateConfiguredEnemy(
                enemyPosition: Vector2.zero,
                playerPosition: new Vector2(100f, 100f),
                sightRange: 2f,
                attackRange: 0.2f,
                attackPower: 1,
                patrolPoints: new List<Vector2> { new Vector2(3f, 0f) });
            EnemyStateContext context = new EnemyStateContext(enemy);
            PatrolState state = new PatrolState(context, null);

            state.Enter();
            state.Tick(0.016f);

            Assert.That(enemy.GetComponent<Rigidbody2D>().velocity.x, Is.GreaterThan(0f));
        }

        [Test]
        public void PatrolState_WhenWaypointReached_AdvancesPatrolIndex()
        {
            EnemyController enemy = CreateConfiguredEnemy(
                enemyPosition: Vector2.zero,
                playerPosition: new Vector2(100f, 100f),
                sightRange: 1f,
                attackRange: 0.1f,
                attackPower: 1,
                patrolPoints: new List<Vector2> { Vector2.zero, new Vector2(2f, 0f) });
            EnemyStateContext context = new EnemyStateContext(enemy);
            PatrolState state = new PatrolState(context, null);

            state.Enter();
            state.Tick(0.016f);

            Assert.That(context.CurrentPatrolPointIndex, Is.EqualTo(1));
        }

        [Test]
        public void ChaseState_WhenTargetInAttackRange_InvokesAttackRangeCallback()
        {
            EnemyController enemy = CreateConfiguredEnemy(
                enemyPosition: Vector2.zero,
                playerPosition: new Vector2(0.2f, 0f),
                sightRange: 3f,
                attackRange: 0.5f,
                attackPower: 1,
                patrolPoints: new List<Vector2> { Vector2.zero });
            EnemyStateContext context = new EnemyStateContext(enemy);
            bool reachedAttackRange = false;
            ChaseState state = new ChaseState(context, null, () => reachedAttackRange = true);

            state.Enter();
            state.Tick(0.016f);

            Assert.That(reachedAttackRange, Is.True);
        }

        [Test]
        public void ChaseState_WhenPlayerLostBeyondGrace_InvokesLostCallback()
        {
            EnemyController enemy = CreateConfiguredEnemy(
                enemyPosition: Vector2.zero,
                playerPosition: new Vector2(10f, 0f),
                sightRange: 1f,
                attackRange: 0.5f,
                attackPower: 1,
                patrolPoints: new List<Vector2> { Vector2.zero });
            EnemyStateContext context = new EnemyStateContext(enemy);
            bool playerLost = false;
            ChaseState state = new ChaseState(context, () => playerLost = true, null);

            state.Enter();
            state.Tick(0.2f);
            Assert.That(playerLost, Is.False);

            state.Tick(0.2f);
            Assert.That(playerLost, Is.True);
        }

        [Test]
        public void AttackState_WhenTargetOutOfRange_InvokesOutOfRangeCallback()
        {
            EnemyController enemy = CreateConfiguredEnemy(
                enemyPosition: Vector2.zero,
                playerPosition: new Vector2(2f, 0f),
                sightRange: 5f,
                attackRange: 0.5f,
                attackPower: 1,
                patrolPoints: new List<Vector2> { Vector2.zero });
            EnemyStateContext context = new EnemyStateContext(enemy);
            bool targetOutOfRange = false;
            bool targetLost = false;
            AttackState state = new AttackState(
                context,
                () => targetOutOfRange = true,
                () => targetLost = true,
                0.5f);

            state.Enter();
            state.Tick(0.016f);

            Assert.That(targetOutOfRange, Is.True);
            Assert.That(targetLost, Is.False);
        }

        [Test]
        public void AttackState_WhenPlayerNotVisible_InvokesTargetLostCallback()
        {
            EnemyController enemy = CreateConfiguredEnemy(
                enemyPosition: Vector2.zero,
                playerPosition: new Vector2(3f, 0f),
                sightRange: 0.5f,
                attackRange: 5f,
                attackPower: 1,
                patrolPoints: new List<Vector2> { Vector2.zero });
            EnemyStateContext context = new EnemyStateContext(enemy);
            bool targetOutOfRange = false;
            bool targetLost = false;
            AttackState state = new AttackState(
                context,
                () => targetOutOfRange = true,
                () => targetLost = true,
                0.5f);

            state.Enter();
            state.Tick(0.016f);

            Assert.That(targetOutOfRange, Is.False);
            Assert.That(targetLost, Is.True);
        }

        [Test]
        public void AttackState_WhenAttacking_RespectsCooldownWindow()
        {
            EnemyController enemy = CreateConfiguredEnemy(
                enemyPosition: Vector2.zero,
                playerPosition: Vector2.zero,
                sightRange: 2f,
                attackRange: 1f,
                attackPower: 1,
                patrolPoints: new List<Vector2> { Vector2.zero });
            EnemyStateContext context = new EnemyStateContext(enemy);
            AttackState state = new AttackState(context, null, null, 10f);

            state.Enter();
            state.Tick(0.016f);
            float firstAttackWindow = context.NextAllowedAttackTime;

            state.Tick(0.016f);
            float secondAttackWindow = context.NextAllowedAttackTime;

            Assert.That(firstAttackWindow, Is.GreaterThan(0f));
            Assert.That(secondAttackWindow, Is.EqualTo(firstAttackWindow));
        }

        private EnemyController CreateConfiguredEnemy(
            Vector2 enemyPosition,
            Vector2 playerPosition,
            float sightRange,
            float attackRange,
            int attackPower,
            List<Vector2> patrolPoints)
        {
            EnemyController enemy = CreateEnemyController("EnemyStateTest_Enemy");
            PlayerController player = CreatePlayerController("EnemyStateTest_Player");

            enemy.transform.position = enemyPosition;
            player.transform.position = playerPosition;

            player.ApplyLevelConfiguration(5f, 10, 1);
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

        private EnemyController CreateEnemyController(string name)
        {
            GameObject enemyObject = new GameObject(name);
            m_CreatedObjects.Add(enemyObject);
            Rigidbody2D enemyRb = enemyObject.AddComponent<Rigidbody2D>();
            Animator enemyAnimator = enemyObject.AddComponent<Animator>();
            EnemyController enemy = enemyObject.AddComponent<EnemyController>();
            SetPrivateField(enemy, "rb", enemyRb);
            SetPrivateField(enemy, "m_Animator", enemyAnimator);
            return enemy;
        }

        private PlayerController CreatePlayerController(string name)
        {
            GameObject playerObject = new GameObject(name);
            m_CreatedObjects.Add(playerObject);
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<Animator>();
            return playerObject.AddComponent<PlayerController>();
        }

        private static void SetPrivateField(object target, string fieldName, object fieldValue)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Missing private field '{fieldName}'.");
            fieldInfo.SetValue(target, fieldValue);
        }
    }
}
