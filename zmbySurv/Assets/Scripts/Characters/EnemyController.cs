using System;
using System.Collections.Generic;
using Characters;
using UnityEngine;

namespace Characters
{
    /// <summary>
    /// Controls enemy AI behavior
    /// Manages movement, detection, and combat with the player through various states (Patrol, Chase, Attack).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class EnemyController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Movement Settings")]
        [SerializeField]
        private float m_MoveSpeed = 3f;
        [SerializeField]
        private float m_RotationSpeed = 5f;
        [SerializeField]
        private float m_ChaseSpeed = 5f;

        [Header("Detection Settings")]
        [SerializeField]
        private float m_SightRange = 10f;
        [SerializeField]
        private float m_AttackRange = 0.5f;

        [Header("Combat Settings")]
        [SerializeField]
        private int m_AttackPower = 1;

        [Header("Environment References")]
        [SerializeField]
        private Rigidbody2D rb;
        [SerializeField]
        private Animator m_Animator;

        #endregion

        #region Public Fields

        /// <summary>
        /// Reference to the player character that this enemy will target.
        /// </summary>
        public PlayerController Player;

        /// <summary>
        /// List of patrol points for the enemy to follow when not chasing the player.
        /// </summary>
        public List<Vector2> patrolPoints;
     
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the Transform of the player target.
        /// </summary>
        public Transform PlayerTarget => Player.transform;

        #endregion

        #region Unity Lifecycle Methods

        private void Awake()
        {
        }

        private void Start()
        {
        }

        private void Update()
        {
        }

        #endregion

        #region Public API Methods

        public void ApplyLevelConfiguration(
            float moveSpeed,
            float chaseSpeed,
            float sightRange,
            float attackRange,
            int attackPower,
            List<Vector2> patrolPoints)
        {
            m_MoveSpeed = moveSpeed;
            m_ChaseSpeed = chaseSpeed;
            m_SightRange = sightRange;
            m_AttackRange = attackRange;
            m_AttackPower = attackPower;
            this.patrolPoints = patrolPoints;

            Debug.Log($"EnemyController: Applied level config - Move: {moveSpeed}, Chase: {chaseSpeed}, Sight: {sightRange}");
        }

        /// <summary>
        /// Moves the enemy towards a specified target position.
        /// Automatically adjusts speed based on distance to target.
        /// Use this in your states to move the enemy!
        /// </summary>
        /// <param name="target">The target position to move towards.</param>
        public void MoveTo(Vector2 target)
        {
            Vector2 currentPos = rb.position;
            Vector2 direction = (target - currentPos).normalized;

            // Simple movement - you can detect which state you're in if needed
            float speed = Vector2.Distance(currentPos, PlayerTarget.position) < m_SightRange ? m_ChaseSpeed : m_MoveSpeed;
            rb.velocity = direction * speed;

            // Apply smooth rotation towards movement direction
            if (direction.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, angle));
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * m_RotationSpeed
                );
            }
        }

        /// <summary>
        /// Plays the specified animation on the enemy's animator.
        /// Use this in your states!
        /// Available animations: "Idle", "Move", "Attack"
        /// </summary>
        /// <param name="animation">The name of the animation to play.</param>
        public void PlayAnimation(string animation)
        {
            m_Animator.Play(animation);
        }

        /// <summary>
        /// Checks if the player is within the enemy's sight range.
        /// Use this to detect when to chase!
        /// </summary>
        /// <returns>True if the player is visible, false otherwise.</returns>
        public bool IsPlayerVisible()
        {
            if (PlayerTarget == null)
            {
                return false;
            }

            float distance = Vector2.Distance(transform.position, PlayerTarget.position);
            return distance <= m_SightRange;
        }

        /// <summary>
        /// Checks if the player is within the enemy's attack range.
        /// Use this to detect when to attack!
        /// </summary>
        /// <returns>True if the player is in attack range, false otherwise.</returns>
        public bool IsPlayerInAttackRange()
        {
            if (PlayerTarget == null)
            {
                return false;
            }

            float distance = Vector2.Distance(transform.position, PlayerTarget.position);
            return distance <= m_AttackRange;
        }

        /// <summary>
        /// Executes an attack on the player, dealing damage based on attack power.
        /// Call this in your AttackState!
        /// </summary>
        public void Attack()
        {
            Debug.Log("Enemy Agent is Attacking!");
            Player.ReduceLife(m_AttackPower);
        }

        #endregion
    }
}

