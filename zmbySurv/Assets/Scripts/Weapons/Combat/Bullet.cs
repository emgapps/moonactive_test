using System;
using Core.Pooling;
using UnityEngine;

namespace Weapons.Combat
{
    /// <summary>
    /// Visual bullet entity that moves from trace origin to trace endpoint.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Bullet : MonoBehaviour, IPoolable
    {
        private const float MinSpeedUnitsPerSecond = 0.0001f;

        [Header("Motion")]
        [SerializeField]
        private float m_DefaultSpeedUnitsPerSecond = 20f;
        [SerializeField]
        private float m_DestinationTolerance = 0.01f;

        private Vector2 m_Destination;
        private float m_ActiveSpeedUnitsPerSecond;
        private bool m_IsInFlight;
        private Action<Bullet> m_OnReachedDestination;

        /// <summary>
        /// Gets whether this bullet is currently moving toward a destination.
        /// </summary>
        public bool IsInFlight => m_IsInFlight;

        /// <summary>
        /// Gets active movement speed in world units per second.
        /// </summary>
        public float ActiveSpeedUnitsPerSecond => m_ActiveSpeedUnitsPerSecond;

        /// <summary>
        /// Gets the current resolved destination for this bullet.
        /// </summary>
        public Vector2 Destination => m_Destination;

        /// <summary>
        /// Gets impact type associated with this bullet's current trace.
        /// </summary>
        public WeaponShotImpactType ImpactType { get; private set; }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Starts bullet movement from trace origin to endpoint.
        /// </summary>
        /// <param name="shotTrace">Resolved shot trace payload.</param>
        /// <param name="speedUnitsPerSecond">Requested movement speed in world units per second.</param>
        /// <param name="onReachedDestination">Optional callback raised when destination is reached.</param>
        public void Launch(
            WeaponShotTrace shotTrace,
            float speedUnitsPerSecond,
            Action<Bullet> onReachedDestination = null)
        {
            transform.position = shotTrace.Origin;
            m_Destination = shotTrace.EndPoint;
            ImpactType = shotTrace.ImpactType;
            m_ActiveSpeedUnitsPerSecond = ResolveSpeed(speedUnitsPerSecond);
            m_OnReachedDestination = onReachedDestination;
            m_IsInFlight = true;

            if (HasReachedDestination(transform.position))
            {
                CompleteFlight();
            }
        }

        /// <summary>
        /// Advances bullet movement by supplied delta time.
        /// </summary>
        /// <param name="deltaTimeSeconds">Delta time in seconds.</param>
        public void Tick(float deltaTimeSeconds)
        {
            if (!m_IsInFlight || deltaTimeSeconds <= 0f)
            {
                return;
            }

            Vector2 currentPosition = transform.position;
            float stepDistance = m_ActiveSpeedUnitsPerSecond * deltaTimeSeconds;
            Vector2 nextPosition = Vector2.MoveTowards(currentPosition, m_Destination, stepDistance);
            transform.position = nextPosition;

            if (HasReachedDestination(nextPosition))
            {
                CompleteFlight();
            }
        }

        /// <summary>
        /// Handles state reset when bullet is taken from pool.
        /// </summary>
        public void OnTakenFromPool()
        {
            ResetState();
        }

        /// <summary>
        /// Handles state reset when bullet is returned to pool.
        /// </summary>
        public void OnReturnedToPool()
        {
            ResetState();
        }

        private void CompleteFlight()
        {
            if (!m_IsInFlight)
            {
                return;
            }

            m_IsInFlight = false;
            Action<Bullet> callback = m_OnReachedDestination;
            m_OnReachedDestination = null;
            callback?.Invoke(this);
        }

        private bool HasReachedDestination(Vector2 position)
        {
            float tolerance = Mathf.Max(MinSpeedUnitsPerSecond, m_DestinationTolerance);
            return (m_Destination - position).sqrMagnitude <= tolerance * tolerance;
        }

        private float ResolveSpeed(float requestedSpeed)
        {
            if (requestedSpeed > MinSpeedUnitsPerSecond)
            {
                return requestedSpeed;
            }

            return Mathf.Max(MinSpeedUnitsPerSecond, m_DefaultSpeedUnitsPerSecond);
        }

        private void ResetState()
        {
            m_Destination = Vector2.zero;
            m_ActiveSpeedUnitsPerSecond = 0f;
            m_IsInFlight = false;
            m_OnReachedDestination = null;
            ImpactType = WeaponShotImpactType.None;
        }
    }
}
