using UnityEngine;
using Weapons.Runtime;

namespace Weapons.Combat
{
    /// <summary>
    /// Resolves weapon shot requests into physics hits and damage application.
    /// </summary>
    public sealed class WeaponHitResolver
    {
        private readonly RaycastHit2D[] m_HitBuffer = new RaycastHit2D[16];

        /// <summary>
        /// Resolves a shot request against the world and applies damage to hit enemies.
        /// </summary>
        /// <param name="shot">Shot request emitted by weapon runtime.</param>
        /// <param name="origin">World-space origin position.</param>
        /// <param name="direction">Normalized forward direction.</param>
        /// <param name="hitMask">Layer mask used for hit detection.</param>
        /// <param name="ownerCollider">Optional collider to ignore during raycasts.</param>
        /// <returns>Count of successful damage applications.</returns>
        public int ResolveShot(
            WeaponShotRequest shot,
            Vector2 origin,
            Vector2 direction,
            LayerMask hitMask,
            Collider2D ownerCollider)
        {
            Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            int pelletCount = Mathf.Max(1, shot.PelletCount);
            float spread = Mathf.Max(0f, shot.SpreadAngleDegrees);

            int appliedHits = 0;
            for (int pelletIndex = 0; pelletIndex < pelletCount; pelletIndex++)
            {
                Vector2 pelletDirection = GetPelletDirection(normalizedDirection, pelletIndex, pelletCount, spread);
                int hitCount = Physics2D.RaycastNonAlloc(origin, pelletDirection, m_HitBuffer, shot.Range, hitMask);
                if (!TryResolveDamageable(hitCount, ownerCollider, out IEnemyDamageable damageable, out Vector2 hitPoint))
                {
                    continue;
                }

                if (!damageable.TryApplyDamage(shot.DamagePerPellet, hitPoint, shot.WeaponId))
                {
                    continue;
                }

                appliedHits += 1;
            }

            return appliedHits;
        }

        private bool TryResolveDamageable(
            int hitCount,
            Collider2D ownerCollider,
            out IEnemyDamageable damageable,
            out Vector2 hitPoint)
        {
            damageable = null;
            hitPoint = Vector2.zero;

            if (hitCount <= 0)
            {
                return false;
            }

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                RaycastHit2D hit = m_HitBuffer[hitIndex];
                Collider2D hitCollider = hit.collider;
                if (hitCollider == null)
                {
                    continue;
                }

                if (ownerCollider != null && hitCollider == ownerCollider)
                {
                    continue;
                }

                IEnemyDamageable candidate = hitCollider.GetComponent<IEnemyDamageable>();
                if (candidate == null)
                {
                    candidate = hitCollider.GetComponentInParent<IEnemyDamageable>();
                }

                if (candidate == null || !candidate.IsAlive)
                {
                    continue;
                }

                damageable = candidate;
                hitPoint = hit.point;
                return true;
            }

            return false;
        }

        private static Vector2 GetPelletDirection(Vector2 baseDirection, int pelletIndex, int pelletCount, float spreadAngle)
        {
            if (pelletCount <= 1 || spreadAngle <= 0.001f)
            {
                return baseDirection;
            }

            float minAngle = -spreadAngle * 0.5f;
            float step = spreadAngle / (pelletCount - 1);
            float currentAngle = minAngle + (step * pelletIndex);
            return Rotate(baseDirection, currentAngle);
        }

        private static Vector2 Rotate(Vector2 vector, float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(
                x: (vector.x * cos) - (vector.y * sin),
                y: (vector.x * sin) + (vector.y * cos)).normalized;
        }
    }
}
