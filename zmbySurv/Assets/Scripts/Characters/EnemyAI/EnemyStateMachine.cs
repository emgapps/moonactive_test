using System;
using UnityEngine;

namespace Characters.EnemyAI
{
    /// <summary>
    /// Coordinates active enemy AI state execution and transitions.
    /// </summary>
    public sealed class EnemyStateMachine
    {
        private readonly EnemyStateContext m_Context;
        private readonly float m_MinTransitionIntervalSeconds;
        private IEnemyState m_CurrentState;
        private float m_LastTransitionTime;

        /// <summary>
        /// Creates a new state machine for an enemy context.
        /// </summary>
        /// <param name="context">Shared runtime context.</param>
        /// <param name="minTransitionIntervalSeconds">Minimum time between transitions.</param>
        public EnemyStateMachine(EnemyStateContext context, float minTransitionIntervalSeconds)
        {
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            m_MinTransitionIntervalSeconds = Mathf.Max(0f, minTransitionIntervalSeconds);
            m_LastTransitionTime = float.NegativeInfinity;
        }

        /// <summary>
        /// Gets the currently active state.
        /// </summary>
        public IEnemyState CurrentState => m_CurrentState;

        /// <summary>
        /// Gets the current state name for diagnostics.
        /// </summary>
        public string CurrentStateName => m_CurrentState != null ? m_CurrentState.Name : "None";

        /// <summary>
        /// Changes to the given state when transition guards allow it.
        /// </summary>
        /// <param name="nextState">Target state.</param>
        /// <param name="reason">Transition reason for logging.</param>
        /// <returns>True when transition happened, otherwise false.</returns>
        public bool TryChangeState(IEnemyState nextState, string reason)
        {
            if (nextState == null)
            {
                throw new ArgumentNullException(nameof(nextState));
            }

            if (ReferenceEquals(m_CurrentState, nextState))
            {
                return false;
            }

            float currentTime = Time.time;
            bool hasCurrentState = m_CurrentState != null;
            if (hasCurrentState && currentTime - m_LastTransitionTime < m_MinTransitionIntervalSeconds)
            {
                return false;
            }

            string previousStateName = CurrentStateName;
            if (hasCurrentState)
            {
                m_CurrentState.Exit();
            }

            m_CurrentState = nextState;
            m_LastTransitionTime = currentTime;
            m_CurrentState.Enter();

            Debug.Log(
                $"[EnemyAI] StateTransition | enemy={m_Context.Controller.name} from={previousStateName} to={m_CurrentState.Name} reason={reason}");

            return true;
        }

        /// <summary>
        /// Ticks the active state.
        /// </summary>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        public void Tick(float deltaTime)
        {
            if (m_CurrentState == null)
            {
                return;
            }

            m_CurrentState.Tick(deltaTime);
        }

        /// <summary>
        /// Clears active state and resets transition timing.
        /// </summary>
        public void Reset()
        {
            if (m_CurrentState != null)
            {
                m_CurrentState.Exit();
                m_CurrentState = null;
            }

            m_LastTransitionTime = float.NegativeInfinity;
        }
    }
}
