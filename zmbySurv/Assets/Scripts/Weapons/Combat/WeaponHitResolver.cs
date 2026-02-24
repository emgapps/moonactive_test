using System.Text;
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

            LogTrace(
                $"[Weapons] ResolveShotBegin | weaponId={shot.WeaponId} damage={shot.DamagePerPellet} origin={FormatVector2(origin)} direction={FormatVector2(normalizedDirection)} range={shot.Range:0.00} pellets={pelletCount} spread={spread:0.00} hitMask={DescribeLayerMask(hitMask)} owner={DescribeCollider(ownerCollider)}");

            int appliedHits = 0;
            for (int pelletIndex = 0; pelletIndex < pelletCount; pelletIndex++)
            {
                Vector2 pelletDirection = GetPelletDirection(normalizedDirection, pelletIndex, pelletCount, spread);
                int hitCount = Physics2D.RaycastNonAlloc(origin, pelletDirection, m_HitBuffer, shot.Range, hitMask);

                if (hitCount >= m_HitBuffer.Length)
                {
                    LogTrace(
                        $"[Weapons] ResolveShotBufferLimit | pellet={pelletIndex + 1}/{pelletCount} hitCount={hitCount} bufferSize={m_HitBuffer.Length}");
                }

                if (hitCount <= 0)
                {
                    LogTrace(
                        $"[Weapons] ResolveShotNoPhysicsHit | pellet={pelletIndex + 1}/{pelletCount} direction={FormatVector2(pelletDirection)}");
                    continue;
                }

                LogTrace(
                    $"[Weapons] ResolveShotPhysicsHits | pellet={pelletIndex + 1}/{pelletCount} direction={FormatVector2(pelletDirection)} hitCount={hitCount}");

                if (!TryResolveDamageable(
                        hitCount,
                        pelletIndex,
                        pelletCount,
                        ownerCollider,
                        out IEnemyDamageable damageable,
                        out Vector2 hitPoint))
                {
                    continue;
                }

                if (!damageable.TryApplyDamage(shot.DamagePerPellet, hitPoint, shot.WeaponId))
                {
                    LogTrace(
                        $"[Weapons] ResolveShotDamageRejected | pellet={pelletIndex + 1}/{pelletCount} target={DescribeTransform(damageable.DamageTransform)} hitPoint={FormatVector2(hitPoint)}");
                    continue;
                }

                appliedHits += 1;
                LogTrace(
                    $"[Weapons] ResolveShotDamageApplied | pellet={pelletIndex + 1}/{pelletCount} target={DescribeTransform(damageable.DamageTransform)} hitPoint={FormatVector2(hitPoint)} totalApplied={appliedHits}");
            }

            LogTrace($"[Weapons] ResolveShotEnd | weaponId={shot.WeaponId} appliedHits={appliedHits} pellets={pelletCount}");
            return appliedHits;
        }

        private bool TryResolveDamageable(
            int hitCount,
            int pelletIndex,
            int pelletCount,
            Collider2D ownerCollider,
            out IEnemyDamageable damageable,
            out Vector2 hitPoint)
        {
            damageable = null;
            hitPoint = Vector2.zero;
            Collider2D blockingCollider = null;
            float blockingDistance = float.MaxValue;

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
                    LogTrace(
                        $"[Weapons] ResolveShotHitSkipped | pellet={pelletIndex + 1}/{pelletCount} hitIndex={hitIndex} reason=null_collider");
                    continue;
                }

                LogTrace(
                    $"[Weapons] ResolveShotHitCandidate | pellet={pelletIndex + 1}/{pelletCount} hitIndex={hitIndex} collider={DescribeCollider(hitCollider)} point={FormatVector2(hit.point)} distance={hit.distance:0.00}");

                if (ownerCollider != null && hitCollider == ownerCollider)
                {
                    LogTrace(
                        $"[Weapons] ResolveShotHitSkipped | pellet={pelletIndex + 1}/{pelletCount} hitIndex={hitIndex} reason=owner_collider collider={DescribeCollider(hitCollider)}");
                    continue;
                }

                IEnemyDamageable candidate = hitCollider.GetComponent<IEnemyDamageable>();
                if (candidate == null)
                {
                    candidate = hitCollider.GetComponentInParent<IEnemyDamageable>();
                }

                if (candidate == null || !candidate.IsAlive)
                {
                    string reason = candidate == null ? "missing_IEnemyDamageable" : "damageable_not_alive";
                    LogTrace(
                        $"[Weapons] ResolveShotHitSkipped | pellet={pelletIndex + 1}/{pelletCount} hitIndex={hitIndex} reason={reason} collider={DescribeCollider(hitCollider)}");

                    if (hit.distance < blockingDistance)
                    {
                        blockingDistance = hit.distance;
                        blockingCollider = hitCollider;
                    }
                    continue;
                }

                damageable = candidate;
                hitPoint = hit.point;
                LogTrace(
                    $"[Weapons] ResolveShotTargetSelected | pellet={pelletIndex + 1}/{pelletCount} hitIndex={hitIndex} target={DescribeTransform(candidate.DamageTransform)} point={FormatVector2(hitPoint)}");
                return true;
            }

            if (blockingCollider != null)
            {
                LogTrace(
                    $"[Weapons] ResolveShotBlocked | pellet={pelletIndex + 1}/{pelletCount} collider={DescribeCollider(blockingCollider)} distance={blockingDistance:0.00}");
            }

            LogTrace(
                $"[Weapons] ResolveShotNoDamageable | pellet={pelletIndex + 1}/{pelletCount} scannedHits={hitCount}");
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

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void LogTrace(string message)
        {
            // Intentionally no-op: verbose trace logs were removed to keep only important logs.
        }

        private static string FormatVector2(Vector2 value)
        {
            return $"({value.x:0.00},{value.y:0.00})";
        }

        private static string DescribeTransform(Transform target)
        {
            if (target == null)
            {
                return "null";
            }

            return $"{target.name}@{FormatVector2(target.position)}";
        }

        private static string DescribeCollider(Collider2D collider)
        {
            if (collider == null)
            {
                return "null";
            }

            string layerName = LayerMask.LayerToName(collider.gameObject.layer);
            if (string.IsNullOrWhiteSpace(layerName))
            {
                layerName = $"layer_{collider.gameObject.layer}";
            }

            return $"{collider.name}(obj={collider.gameObject.name},layer={layerName},trigger={collider.isTrigger})";
        }

        private static string DescribeLayerMask(LayerMask layerMask)
        {
            int rawMask = layerMask.value;
            if (rawMask == 0)
            {
                return "none(0)";
            }

            StringBuilder builder = new StringBuilder();
            for (int layerIndex = 0; layerIndex < 32; layerIndex++)
            {
                if ((rawMask & (1 << layerIndex)) == 0)
                {
                    continue;
                }

                string layerName = LayerMask.LayerToName(layerIndex);
                if (string.IsNullOrWhiteSpace(layerName))
                {
                    layerName = $"layer_{layerIndex}";
                }

                if (builder.Length > 0)
                {
                    builder.Append('|');
                }

                builder.Append(layerName);
            }

            return $"{builder}({rawMask})";
        }
    }
}
