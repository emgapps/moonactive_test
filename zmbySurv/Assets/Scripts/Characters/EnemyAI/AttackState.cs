using System;
using UnityEngine;

namespace Characters.EnemyAI
{
    /// <summary>
    /// Applies periodic damage while the player remains in attack range.
    /// </summary>
    public sealed class AttackState : IEnemyState
    {
        private readonly EnemyStateContext m_Context;
        private readonly Action m_OnTargetOutOfRange;
        private readonly Action m_OnTargetLost;
        private readonly float m_AttackCooldownSeconds;

        /// <summary>
        /// Creates an attack state for the given enemy context.
        /// </summary>
        /// <param name="context">Shared runtime context.</param>
        /// <param name="onTargetOutOfRange">Callback invoked when target leaves attack range.</param>
        /// <param name="onTargetLost">Callback invoked when target can no longer be tracked.</param>
        /// <param name="attackCooldownSeconds">Time between attack executions.</param>
        public AttackState(
            EnemyStateContext context,
            Action onTargetOutOfRange,
            Action onTargetLost,
            float attackCooldownSeconds)
        {
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            m_OnTargetOutOfRange = onTargetOutOfRange;
            m_OnTargetLost = onTargetLost;
            m_AttackCooldownSeconds = Mathf.Max(0.05f, attackCooldownSeconds);
        }

        /// <summary>
        /// Gets the state name for diagnostics.
        /// </summary>
        public string Name => "Attack";

        /// <summary>
        /// Prepares attack visuals when state becomes active.
        /// </summary>
        public void Enter()
        {
            m_Context.Controller.PlayAnimation("Attack");
        }

        /// <summary>
        /// Attacks with cooldown while transition conditions remain valid.
        /// </summary>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        public void Tick(float deltaTime)
        {
            Transform playerTarget = m_Context.Controller.PlayerTarget;
            if (playerTarget == null)
            {
                m_OnTargetLost?.Invoke();
                return;
            }

            if (!m_Context.Controller.IsPlayerVisible())
            {
                m_OnTargetLost?.Invoke();
                return;
            }

            if (!m_Context.Controller.IsPlayerInAttackRange())
            {
                m_OnTargetOutOfRange?.Invoke();
                return;
            }

            // Keeps the enemy stationary while attacking.
            m_Context.Controller.MoveTo(m_Context.Controller.transform.position);

            float currentTime = Time.time;
            if (currentTime < m_Context.NextAllowedAttackTime)
            {
                return;
            }

            m_Context.Controller.Attack();
            m_Context.NextAllowedAttackTime = currentTime + m_AttackCooldownSeconds;
        }

        /// <summary>
        /// Clears attack-local data on state exit.
        /// </summary>
        public void Exit()
        {
        }
    }
}
