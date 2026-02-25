using UnityEngine;

namespace Weapons.Combat
{
    /// <summary>
    /// Immutable trace payload for a resolved pellet path.
    /// </summary>
    public readonly struct WeaponShotTrace
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponShotTrace"/> struct.
        /// </summary>
        /// <param name="weaponId">Weapon identifier that produced this trace.</param>
        /// <param name="pelletIndex">Zero-based index of pellet in the shot.</param>
        /// <param name="pelletCount">Total pellet count for the shot.</param>
        /// <param name="origin">World-space pellet start position.</param>
        /// <param name="direction">Normalized pellet travel direction.</param>
        /// <param name="endPoint">World-space pellet destination point.</param>
        /// <param name="maxRange">Configured max range for this pellet.</param>
        /// <param name="traveledDistance">Resolved travel distance from origin to endpoint.</param>
        /// <param name="impactType">Resolved impact classification.</param>
        /// <param name="impactCollider">Collider that ended the pellet trace; null when max range was reached.</param>
        public WeaponShotTrace(
            string weaponId,
            int pelletIndex,
            int pelletCount,
            Vector2 origin,
            Vector2 direction,
            Vector2 endPoint,
            float maxRange,
            float traveledDistance,
            WeaponShotImpactType impactType,
            Collider2D impactCollider)
        {
            WeaponId = weaponId;
            PelletIndex = pelletIndex;
            PelletCount = pelletCount;
            Origin = origin;
            Direction = direction;
            EndPoint = endPoint;
            MaxRange = maxRange;
            TraveledDistance = traveledDistance;
            ImpactType = impactType;
            ImpactCollider = impactCollider;
        }

        /// <summary>
        /// Gets weapon identifier that emitted this trace.
        /// </summary>
        public string WeaponId { get; }

        /// <summary>
        /// Gets zero-based pellet index in the shot.
        /// </summary>
        public int PelletIndex { get; }

        /// <summary>
        /// Gets total pellet count in the shot.
        /// </summary>
        public int PelletCount { get; }

        /// <summary>
        /// Gets world-space start position.
        /// </summary>
        public Vector2 Origin { get; }

        /// <summary>
        /// Gets normalized travel direction.
        /// </summary>
        public Vector2 Direction { get; }

        /// <summary>
        /// Gets world-space destination point.
        /// </summary>
        public Vector2 EndPoint { get; }

        /// <summary>
        /// Gets configured max range value.
        /// </summary>
        public float MaxRange { get; }

        /// <summary>
        /// Gets resolved travel distance from origin to endpoint.
        /// </summary>
        public float TraveledDistance { get; }

        /// <summary>
        /// Gets impact classification for this pellet path.
        /// </summary>
        public WeaponShotImpactType ImpactType { get; }

        /// <summary>
        /// Gets collider that ended this pellet path; null when max range was reached.
        /// </summary>
        public Collider2D ImpactCollider { get; }
    }
}
