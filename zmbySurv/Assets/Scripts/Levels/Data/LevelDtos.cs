using System;
using System.Collections.Generic;
using UnityEngine;

namespace Level.Data
{
    /// <summary>
    /// Root DTO containing all level definitions loaded from JSON.
    /// </summary>
    [Serializable]
    public sealed class LevelCollectionDto
    {
        /// <summary>
        /// Ordered list of playable levels.
        /// </summary>
        public List<LevelDataDto> levels;
    }

    /// <summary>
    /// DTO describing a single level configuration.
    /// </summary>
    [Serializable]
    public sealed class LevelDataDto
    {
        /// <summary>
        /// Stable unique level identifier.
        /// </summary>
        public string levelId;

        /// <summary>
        /// Display name for level UI.
        /// </summary>
        public string levelName;

        /// <summary>
        /// Currency target required to complete the level.
        /// </summary>
        public int goalCoins;

        /// <summary>
        /// Maximum number of coins allowed simultaneously on the board.
        /// </summary>
        public int maxCoinsOnBoard;

        /// <summary>
        /// Player gameplay parameters for the level.
        /// </summary>
        public PlayerConfigDto playerConfig;

        /// <summary>
        /// Zombie instances and their behavior configuration.
        /// </summary>
        public List<ZombieConfigDto> zombies;
    }

    /// <summary>
    /// DTO with player setup values for a level.
    /// </summary>
    [Serializable]
    public sealed class PlayerConfigDto
    {
        /// <summary>
        /// Player movement speed in world units per second.
        /// </summary>
        public float speed;

        /// <summary>
        /// Player health at level start.
        /// </summary>
        public int health;

        /// <summary>
        /// Player world spawn position.
        /// </summary>
        public Vector2Dto spawnPosition;
    }

    /// <summary>
    /// DTO with configuration for one zombie.
    /// </summary>
    [Serializable]
    public sealed class ZombieConfigDto
    {
        /// <summary>
        /// Stable unique zombie identifier in a level.
        /// </summary>
        public string zombieId;

        /// <summary>
        /// Initial zombie world spawn position.
        /// </summary>
        public Vector2Dto spawnPosition;

        /// <summary>
        /// Patrol movement speed.
        /// </summary>
        public float moveSpeed;

        /// <summary>
        /// Chase movement speed.
        /// </summary>
        public float chaseSpeed;

        /// <summary>
        /// Player detection range.
        /// </summary>
        public float detectDistance;

        /// <summary>
        /// Attack range threshold.
        /// </summary>
        public float attackRange;

        /// <summary>
        /// Damage applied per attack.
        /// </summary>
        public int attackPower;

        /// <summary>
        /// Ordered patrol points in world space.
        /// </summary>
        public List<Vector2Dto> patrolPath;
    }

    /// <summary>
    /// JSON-friendly 2D vector DTO.
    /// </summary>
    [Serializable]
    public sealed class Vector2Dto
    {
        /// <summary>
        /// X axis value.
        /// </summary>
        public float x;

        /// <summary>
        /// Y axis value.
        /// </summary>
        public float y;

        /// <summary>
        /// Converts DTO to Unity vector.
        /// </summary>
        /// <returns>Unity vector with DTO coordinates.</returns>
        public Vector2 ToUnityVector2()
        {
            return new Vector2(x, y);
        }

        /// <summary>
        /// Converts DTO to Unity vector3 on Z=0 plane.
        /// </summary>
        /// <returns>Unity vector3 with DTO coordinates and zero Z.</returns>
        public Vector3 ToUnityVector3()
        {
            return new Vector3(x, y, 0f);
        }
    }
}
