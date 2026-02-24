using System.Collections.Generic;
using System.Reflection;
using Characters;
using Characters.EnemyAI;
using NUnit.Framework;
using UnityEngine;

namespace EnemyAI.Tests.EditMode
{
    /// <summary>
    /// Unit tests for enemy state machine transition behavior.
    /// </summary>
    public sealed class EnemyStateMachineTests
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
        public void TryChangeState_SwitchesState_AndTicksCurrentState()
        {
            EnemyStateMachine stateMachine = CreateStateMachine(0f);
            FakeEnemyState firstState = new FakeEnemyState("First");
            FakeEnemyState secondState = new FakeEnemyState("Second");

            bool firstResult = stateMachine.TryChangeState(firstState, "test_enter_first");
            stateMachine.Tick(0.016f);
            bool secondResult = stateMachine.TryChangeState(secondState, "test_enter_second");
            stateMachine.Tick(0.016f);

            Assert.That(firstResult, Is.True);
            Assert.That(secondResult, Is.True);
            Assert.That(firstState.EnterCount, Is.EqualTo(1));
            Assert.That(firstState.ExitCount, Is.EqualTo(1));
            Assert.That(firstState.TickCount, Is.EqualTo(1));
            Assert.That(secondState.EnterCount, Is.EqualTo(1));
            Assert.That(secondState.TickCount, Is.EqualTo(1));
            Assert.That(stateMachine.CurrentState, Is.EqualTo(secondState));
            Assert.That(stateMachine.CurrentStateName, Is.EqualTo(secondState.Name));
        }

        [Test]
        public void TryChangeState_WhenSameState_ReturnsFalse()
        {
            EnemyStateMachine stateMachine = CreateStateMachine(0f);
            FakeEnemyState state = new FakeEnemyState("Single");

            bool firstResult = stateMachine.TryChangeState(state, "initial");
            bool secondResult = stateMachine.TryChangeState(state, "duplicate");

            Assert.That(firstResult, Is.True);
            Assert.That(secondResult, Is.False);
            Assert.That(state.EnterCount, Is.EqualTo(1));
            Assert.That(state.ExitCount, Is.EqualTo(0));
        }

        [Test]
        public void TryChangeState_WhenIntervalNotElapsed_BlocksTransition()
        {
            EnemyStateMachine stateMachine = CreateStateMachine(10f);
            FakeEnemyState firstState = new FakeEnemyState("First");
            FakeEnemyState secondState = new FakeEnemyState("Second");

            bool firstResult = stateMachine.TryChangeState(firstState, "initial");
            bool secondResult = stateMachine.TryChangeState(secondState, "too_fast");

            Assert.That(firstResult, Is.True);
            Assert.That(secondResult, Is.False);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(firstState));
            Assert.That(firstState.ExitCount, Is.EqualTo(0));
            Assert.That(secondState.EnterCount, Is.EqualTo(0));
        }

        [Test]
        public void Reset_ExitsCurrentState_AndClearsMachine()
        {
            EnemyStateMachine stateMachine = CreateStateMachine(0f);
            FakeEnemyState state = new FakeEnemyState("Active");

            stateMachine.TryChangeState(state, "initial");
            stateMachine.Reset();

            Assert.That(state.ExitCount, Is.EqualTo(1));
            Assert.That(stateMachine.CurrentState, Is.Null);
            Assert.That(stateMachine.CurrentStateName, Is.EqualTo("None"));
        }

        private EnemyStateMachine CreateStateMachine(float transitionIntervalSeconds)
        {
            EnemyController controller = CreateEnemyController("EnemyStateMachineTest");
            EnemyStateContext context = new EnemyStateContext(controller);
            return new EnemyStateMachine(context, transitionIntervalSeconds);
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

        private static void SetPrivateField(object target, string fieldName, object fieldValue)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Missing private field '{fieldName}'.");
            fieldInfo.SetValue(target, fieldValue);
        }

        /// <summary>
        /// Test state with counters to assert state machine behavior.
        /// </summary>
        private sealed class FakeEnemyState : IEnemyState
        {
            public FakeEnemyState(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public int EnterCount { get; private set; }

            public int TickCount { get; private set; }

            public int ExitCount { get; private set; }

            public void Enter()
            {
                EnterCount++;
            }

            public void Tick(float deltaTime)
            {
                TickCount++;
            }

            public void Exit()
            {
                ExitCount++;
            }
        }
    }
}
