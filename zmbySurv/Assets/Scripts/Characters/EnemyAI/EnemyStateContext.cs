using System;
using UnityEngine;

namespace Characters.EnemyAI
{
    /// <summary>
    /// Shared runtime state for enemy AI behaviors.
    /// </summary>
    public sealed class EnemyStateContext
    {
        private readonly EnemyController m_Controller;
        private int m_CurrentPatrolPointIndex;

        /// <summary>
        /// Initializes a new context for an enemy controller.
        /// </summary>
        /// <param name="controller">Enemy controller owning this context.</param>
        public EnemyStateContext(EnemyController controller)
        {
            m_Controller = controller ?? throw new ArgumentNullException(nameof(controller));
            m_CurrentPatrolPointIndex = 0;
            NextAllowedAttackTime = 0f;
        }

        /// <summary>
        /// Gets the enemy controller that owns this context.
        /// </summary>
        public EnemyController Controller => m_Controller;

        /// <summary>
        /// Gets the index of the active patrol waypoint.
        /// </summary>
        public int CurrentPatrolPointIndex => m_CurrentPatrolPointIndex;

        /// <summary>
        /// Gets or sets the next absolute time when attack is allowed.
        /// </summary>
        public float NextAllowedAttackTime { get; set; }

        /// <summary>
        /// Returns whether patrol waypoints are available.
        /// </summary>
        public bool HasPatrolPoints => m_Controller.patrolPoints != null && m_Controller.patrolPoints.Count > 0;

        /// <summary>
        /// Gets the current patrol target position.
        /// </summary>
        /// <returns>Current waypoint position, or enemy position if waypoints are missing.</returns>
        public Vector2 GetCurrentPatrolPoint()
        {
            if (!HasPatrolPoints)
            {
                return m_Controller.transform.position;
            }

            return m_Controller.patrolPoints[m_CurrentPatrolPointIndex];
        }

        /// <summary>
        /// Advances to the next patrol point in a loop.
        /// </summary>
        public void AdvancePatrolPoint()
        {
            if (!HasPatrolPoints)
            {
                return;
            }

            m_CurrentPatrolPointIndex = (m_CurrentPatrolPointIndex + 1) % m_Controller.patrolPoints.Count;
        }

        /// <summary>
        /// Resets context values for a fresh AI lifecycle.
        /// </summary>
        public void Reset()
        {
            m_CurrentPatrolPointIndex = 0;
            NextAllowedAttackTime = 0f;
        }
    }
}
