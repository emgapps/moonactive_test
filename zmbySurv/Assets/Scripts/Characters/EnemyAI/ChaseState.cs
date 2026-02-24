using System;
using UnityEngine;

namespace Characters.EnemyAI
{
    /// <summary>
    /// Pursues the player while visible and transitions to attack or patrol based on range conditions.
    /// </summary>
    public sealed class ChaseState : IEnemyState
    {
        private const float LostSightGraceSeconds = 0.35f;

        private readonly EnemyStateContext m_Context;
        private readonly Action m_OnPlayerLost;
        private readonly Action m_OnAttackRangeReached;

        private float m_TimeWithoutSight;

        /// <summary>
        /// Creates a chase state for the given enemy context.
        /// </summary>
        /// <param name="context">Shared runtime context.</param>
        /// <param name="onPlayerLost">Callback invoked when player is not visible for the grace period.</param>
        /// <param name="onAttackRangeReached">Callback invoked when player enters attack range.</param>
        public ChaseState(
            EnemyStateContext context,
            Action onPlayerLost,
            Action onAttackRangeReached)
        {
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            m_OnPlayerLost = onPlayerLost;
            m_OnAttackRangeReached = onAttackRangeReached;
            m_TimeWithoutSight = 0f;
        }

        /// <summary>
        /// Gets the state name for diagnostics.
        /// </summary>
        public string Name => "Chase";

        /// <summary>
        /// Resets chase-specific timers on state activation.
        /// </summary>
        public void Enter()
        {
            m_TimeWithoutSight = 0f;
            m_Context.Controller.PlayAnimation("Move");
        }

        /// <summary>
        /// Executes pursuit behavior and transition checks.
        /// </summary>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        public void Tick(float deltaTime)
        {
            Transform playerTarget = m_Context.Controller.PlayerTarget;
            if (playerTarget == null)
            {
                m_OnPlayerLost?.Invoke();
                return;
            }

            if (m_Context.Controller.IsPlayerInAttackRange())
            {
                m_OnAttackRangeReached?.Invoke();
                return;
            }

            if (!m_Context.Controller.IsPlayerVisible())
            {
                m_TimeWithoutSight += Mathf.Max(0f, deltaTime);
                if (m_TimeWithoutSight >= LostSightGraceSeconds)
                {
                    m_OnPlayerLost?.Invoke();
                }

                return;
            }

            m_TimeWithoutSight = 0f;
            m_Context.Controller.MoveTo(playerTarget.position, m_Context.Controller.ChaseSpeed);
        }

        /// <summary>
        /// Clears state-local timers on state exit.
        /// </summary>
        public void Exit()
        {
            m_TimeWithoutSight = 0f;
        }
    }
}
