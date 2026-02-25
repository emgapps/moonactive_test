using System.Collections.Generic;
using UnityEngine;

namespace Characters
{
    /// <summary>
    /// Defines enemy control and combat callbacks handled by enemy controller implementations.
    /// </summary>
    public interface IEnemyController
    {
        /// <summary>
        /// Gets enemy name used in diagnostics.
        /// </summary>
        string ControllerName { get; }

        /// <summary>
        /// Gets the patrol route points currently configured for this enemy.
        /// </summary>
        IReadOnlyList<Vector2> PatrolPoints { get; }

        /// <summary>
        /// Gets the current player target transform.
        /// </summary>
        Transform PlayerTarget { get; }

        /// <summary>
        /// Gets patrol movement speed.
        /// </summary>
        float PatrolSpeed { get; }

        /// <summary>
        /// Gets chase movement speed.
        /// </summary>
        float ChaseSpeed { get; }

        /// <summary>
        /// Gets current world-space enemy position.
        /// </summary>
        Vector2 CurrentPosition { get; }

        /// <summary>
        /// Applies level-specific enemy configuration loaded from level data.
        /// </summary>
        /// <param name="moveSpeed">Patrol movement speed.</param>
        /// <param name="chaseSpeed">Chase movement speed.</param>
        /// <param name="sightRange">Player detection range.</param>
        /// <param name="attackRange">Attack execution range.</param>
        /// <param name="attackPower">Damage dealt per successful attack.</param>
        /// <param name="patrolPoints">Patrol route points.</param>
        void ApplyLevelConfiguration(
            float moveSpeed,
            float chaseSpeed,
            float sightRange,
            float attackRange,
            int attackPower,
            List<Vector2> patrolPoints);

        /// <summary>
        /// Moves the enemy towards a specified target position.
        /// </summary>
        /// <param name="target">The target position to move towards.</param>
        void MoveTo(Vector2 target);

        /// <summary>
        /// Moves the enemy towards a specified position using explicit movement speed.
        /// </summary>
        /// <param name="target">Target world position.</param>
        /// <param name="movementSpeed">Movement speed in world units per second.</param>
        void MoveTo(Vector2 target, float movementSpeed);

        /// <summary>
        /// Stops enemy movement immediately.
        /// </summary>
        void StopMovement();

        /// <summary>
        /// Plays the specified animation on the enemy animator.
        /// </summary>
        /// <param name="animation">The animation state name to play.</param>
        void PlayAnimation(string animation);

        /// <summary>
        /// Checks if the player is within enemy sight range.
        /// </summary>
        /// <returns>True if player is visible; otherwise false.</returns>
        bool IsPlayerVisible();

        /// <summary>
        /// Checks if the player is within enemy attack range.
        /// </summary>
        /// <returns>True if player is in attack range; otherwise false.</returns>
        bool IsPlayerInAttackRange();

        /// <summary>
        /// Executes an attack on the current target.
        /// </summary>
        void Attack();

        /// <summary>
        /// Handles successful incoming damage feedback for the enemy.
        /// </summary>
        void OnDamage();
    }
}
