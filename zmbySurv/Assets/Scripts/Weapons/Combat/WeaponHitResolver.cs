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
        private const float DistanceComparisonEpsilon = 0.0001f;
        private readonly RaycastHit2D[] m_HitBuffer = new RaycastHit2D[16];

        /// <summary>
        /// Resolves a shot request against the world and applies damage to hit enemies.
        /// </summary>
        /// <param name="shot">Shot request emitted by weapon runtime.</param>
        /// <param name="origin">World-space origin position.</param>
        /// <param name="direction">Normalized forward direction.</param>
        /// <param name="hitMask">Layer mask used for hit detection.</param>
        /// <param name="ownerCollider">Optional collider to ignore during raycasts.</param>
        /// <param name="shotTraceDispatcher">Optional trace dispatcher receiving one payload per resolved pellet.</param>
        /// <returns>Count of successful damage applications.</returns>
        public int ResolveShot(
            WeaponShotRequest shot,
            Vector2 origin,
            Vector2 direction,
            LayerMask hitMask,
            Collider2D ownerCollider,
            IWeaponShotTraceDispatcher shotTraceDispatcher = null)
        {
            Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            int pelletCount = Mathf.Max(1, shot.PelletCount);
            float spread = Mathf.Max(0f, shot.SpreadAngleDegrees);
            float shotRange = Mathf.Max(0f, shot.Range);

            LogTrace(
                $"[Weapons] ResolveShotBegin | weaponId={shot.WeaponId} damage={shot.DamagePerPellet} origin={FormatVector2(origin)} direction={FormatVector2(normalizedDirection)} range={shotRange:0.00} pellets={pelletCount} spread={spread:0.00} hitMask={DescribeLayerMask(hitMask)} owner={DescribeCollider(ownerCollider)}");

            int appliedHits = 0;
            for (int pelletIndex = 0; pelletIndex < pelletCount; pelletIndex++)
            {
                Vector2 pelletDirection = GetPelletDirection(normalizedDirection, pelletIndex, pelletCount, spread);
                int hitCount = Physics2D.RaycastNonAlloc(origin, pelletDirection, m_HitBuffer, shotRange, hitMask);

                if (hitCount >= m_HitBuffer.Length)
                {
                    LogTrace(
                        $"[Weapons] ResolveShotBufferLimit | pellet={pelletIndex + 1}/{pelletCount} hitCount={hitCount} bufferSize={m_HitBuffer.Length}");
                }

                PelletImpactResolution impactResolution = hitCount > 0
                    ? ResolvePelletImpact(hitCount, pelletIndex, pelletCount, ownerCollider)
                    : PelletImpactResolution.None;

                if (hitCount <= 0)
                {
                    LogTrace(
                        $"[Weapons] ResolveShotNoPhysicsHit | pellet={pelletIndex + 1}/{pelletCount} direction={FormatVector2(pelletDirection)}");
                }
                else
                {
                    LogTrace(
                        $"[Weapons] ResolveShotPhysicsHits | pellet={pelletIndex + 1}/{pelletCount} direction={FormatVector2(pelletDirection)} hitCount={hitCount}");
                }

                Vector2 endPoint = origin + (pelletDirection * shotRange);
                WeaponShotImpactType impactType = WeaponShotImpactType.None;
                Collider2D impactCollider = null;

                if (impactResolution.HasDamageable)
                {
                    impactType = WeaponShotImpactType.Enemy;
                    impactCollider = impactResolution.ImpactCollider;
                    endPoint = impactResolution.ImpactPoint;

                    if (!impactResolution.Damageable.TryApplyDamage(shot.DamagePerPellet, impactResolution.ImpactPoint, shot.WeaponId))
                    {
                        LogTrace(
                            $"[Weapons] ResolveShotDamageRejected | pellet={pelletIndex + 1}/{pelletCount} target={DescribeTransform(impactResolution.Damageable.DamageTransform)} hitPoint={FormatVector2(impactResolution.ImpactPoint)}");
                    }
                    else
                    {
                        appliedHits += 1;
                        LogTrace(
                            $"[Weapons] ResolveShotDamageApplied | pellet={pelletIndex + 1}/{pelletCount} target={DescribeTransform(impactResolution.Damageable.DamageTransform)} hitPoint={FormatVector2(impactResolution.ImpactPoint)} totalApplied={appliedHits}");
                    }
                }
                else if (impactResolution.HasBlockingCollider)
                {
                    impactType = WeaponShotImpactType.BlockingCollider;
                    impactCollider = impactResolution.ImpactCollider;
                    endPoint = impactResolution.ImpactPoint;
                }

                float traveledDistance = Mathf.Clamp(Vector2.Distance(origin, endPoint), 0f, shotRange);
                shotTraceDispatcher?.DispatchShotTrace(new WeaponShotTrace(
                    weaponId: shot.WeaponId,
                    pelletIndex: pelletIndex,
                    pelletCount: pelletCount,
                    origin: origin,
                    direction: pelletDirection,
                    endPoint: endPoint,
                    maxRange: shotRange,
                    traveledDistance: traveledDistance,
                    impactType: impactType,
                    impactCollider: impactCollider));
            }

            LogTrace($"[Weapons] ResolveShotEnd | weaponId={shot.WeaponId} appliedHits={appliedHits} pellets={pelletCount}");
            return appliedHits;
        }

        private PelletImpactResolution ResolvePelletImpact(
            int hitCount,
            int pelletIndex,
            int pelletCount,
            Collider2D ownerCollider)
        {
            IEnemyDamageable nearestDamageable = null;
            Collider2D nearestDamageableCollider = null;
            Vector2 nearestDamagePoint = Vector2.zero;
            float nearestDamageDistance = float.MaxValue;
            Collider2D nearestBlockingCollider = null;
            Vector2 nearestBlockingPoint = Vector2.zero;
            float nearestBlockingDistance = float.MaxValue;

            if (hitCount <= 0)
            {
                return PelletImpactResolution.None;
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

                IEnemyDamageable candidate = TryGetDamageable(hitCollider);
                if (candidate != null && candidate.IsAlive)
                {
                    if (hit.distance < nearestDamageDistance)
                    {
                        nearestDamageDistance = hit.distance;
                        nearestDamageable = candidate;
                        nearestDamageableCollider = hitCollider;
                        nearestDamagePoint = hit.point;
                    }

                    continue;
                }

                string reason = candidate == null ? "missing_IEnemyDamageable" : "damageable_not_alive";
                LogTrace(
                    $"[Weapons] ResolveShotHitSkipped | pellet={pelletIndex + 1}/{pelletCount} hitIndex={hitIndex} reason={reason} collider={DescribeCollider(hitCollider)}");

                if (hit.distance < nearestBlockingDistance)
                {
                    nearestBlockingDistance = hit.distance;
                    nearestBlockingCollider = hitCollider;
                    nearestBlockingPoint = hit.point;
                }
            }

            if (nearestDamageable != null &&
                (nearestBlockingCollider == null || nearestDamageDistance <= nearestBlockingDistance + DistanceComparisonEpsilon))
            {
                LogTrace(
                    $"[Weapons] ResolveShotTargetSelected | pellet={pelletIndex + 1}/{pelletCount} target={DescribeTransform(nearestDamageable.DamageTransform)} point={FormatVector2(nearestDamagePoint)} distance={nearestDamageDistance:0.00}");

                return PelletImpactResolution.ForDamageable(
                    damageable: nearestDamageable,
                    impactCollider: nearestDamageableCollider,
                    impactPoint: nearestDamagePoint,
                    impactDistance: nearestDamageDistance);
            }

            if (nearestBlockingCollider != null)
            {
                LogTrace(
                    $"[Weapons] ResolveShotBlocked | pellet={pelletIndex + 1}/{pelletCount} collider={DescribeCollider(nearestBlockingCollider)} distance={nearestBlockingDistance:0.00}");

                return PelletImpactResolution.ForBlockingCollider(
                    impactCollider: nearestBlockingCollider,
                    impactPoint: nearestBlockingPoint,
                    impactDistance: nearestBlockingDistance);
            }

            LogTrace(
                $"[Weapons] ResolveShotNoDamageable | pellet={pelletIndex + 1}/{pelletCount} scannedHits={hitCount}");
            return PelletImpactResolution.None;
        }

        private static IEnemyDamageable TryGetDamageable(Collider2D hitCollider)
        {
            if (hitCollider == null)
            {
                return null;
            }

            IEnemyDamageable damageable = hitCollider.GetComponent<IEnemyDamageable>();
            if (damageable != null)
            {
                return damageable;
            }

            return hitCollider.GetComponentInParent<IEnemyDamageable>();
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

        private readonly struct PelletImpactResolution
        {
            private PelletImpactResolution(
                PelletImpactResolutionType resolutionType,
                IEnemyDamageable damageable,
                Collider2D impactCollider,
                Vector2 impactPoint,
                float impactDistance)
            {
                ResolutionType = resolutionType;
                Damageable = damageable;
                ImpactCollider = impactCollider;
                ImpactPoint = impactPoint;
                ImpactDistance = impactDistance;
            }

            public static PelletImpactResolution None => new PelletImpactResolution(
                resolutionType: PelletImpactResolutionType.None,
                damageable: null,
                impactCollider: null,
                impactPoint: Vector2.zero,
                impactDistance: 0f);

            public bool HasDamageable => ResolutionType == PelletImpactResolutionType.Damageable && Damageable != null;

            public bool HasBlockingCollider => ResolutionType == PelletImpactResolutionType.BlockingCollider && ImpactCollider != null;

            public IEnemyDamageable Damageable { get; }

            public Collider2D ImpactCollider { get; }

            public Vector2 ImpactPoint { get; }

            public float ImpactDistance { get; }

            public PelletImpactResolutionType ResolutionType { get; }

            public static PelletImpactResolution ForDamageable(
                IEnemyDamageable damageable,
                Collider2D impactCollider,
                Vector2 impactPoint,
                float impactDistance)
            {
                return new PelletImpactResolution(
                    resolutionType: PelletImpactResolutionType.Damageable,
                    damageable: damageable,
                    impactCollider: impactCollider,
                    impactPoint: impactPoint,
                    impactDistance: impactDistance);
            }

            public static PelletImpactResolution ForBlockingCollider(
                Collider2D impactCollider,
                Vector2 impactPoint,
                float impactDistance)
            {
                return new PelletImpactResolution(
                    resolutionType: PelletImpactResolutionType.BlockingCollider,
                    damageable: null,
                    impactCollider: impactCollider,
                    impactPoint: impactPoint,
                    impactDistance: impactDistance);
            }
        }

        private enum PelletImpactResolutionType
        {
            None = 0,
            Damageable = 1,
            BlockingCollider = 2
        }
    }
}
