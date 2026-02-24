using System;
using UnityEngine;

namespace Characters.EnemyAI
{
    /// <summary>
    /// Moves the enemy across configured patrol waypoints until the player is detected.
    /// </summary>
    public sealed class PatrolState : IEnemyState
    {
        private const float WaypointReachDistance = 0.1f;

        private readonly EnemyStateContext m_Context;
        private readonly Action m_OnPlayerDetected;

        /// <summary>
        /// Creates a patrol state for the given enemy context.
        /// </summary>
        /// <param name="context">Shared runtime context.</param>
        /// <param name="onPlayerDetected">Callback invoked once visibility condition is met.</param>
        public PatrolState(EnemyStateContext context, Action onPlayerDetected)
        {
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            m_OnPlayerDetected = onPlayerDetected;
        }

        /// <summary>
        /// Gets the state name for diagnostics.
        /// </summary>
        public string Name => "Patrol";

        /// <summary>
        /// Sets movement animation when patrol starts.
        /// </summary>
        public void Enter()
        {
            m_Context.Controller.PlayAnimation("Move");
        }

        /// <summary>
        /// Executes patrol movement and checks for transition triggers.
        /// </summary>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        public void Tick(float deltaTime)
        {
            if (m_Context.Controller.IsPlayerVisible())
            {
                m_OnPlayerDetected?.Invoke();
                return;
            }

            if (!m_Context.HasPatrolPoints)
            {
                m_Context.Controller.PlayAnimation("Idle");
                return;
            }

            Vector2 patrolTarget = m_Context.GetCurrentPatrolPoint();
            m_Context.Controller.MoveTo(patrolTarget);

            float distanceToPatrolPoint = Vector2.Distance(
                m_Context.Controller.transform.position,
                patrolTarget);

            if (distanceToPatrolPoint <= WaypointReachDistance)
            {
                m_Context.AdvancePatrolPoint();
            }
        }

        /// <summary>
        /// Stops patrol-specific side effects before leaving the state.
        /// </summary>
        public void Exit()
        {
        }
    }
}
