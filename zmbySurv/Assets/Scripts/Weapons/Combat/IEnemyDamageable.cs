using UnityEngine;

namespace Weapons.Combat
{
    /// <summary>
    /// Abstraction for damageable enemy targets.
    /// </summary>
    public interface IEnemyDamageable
    {
        /// <summary>
        /// Gets whether this target can still receive damage.
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Gets transform used for hit positioning and diagnostics.
        /// </summary>
        Transform DamageTransform { get; }

        /// <summary>
        /// Attempts to apply incoming damage from a weapon source.
        /// </summary>
        /// <param name="damageAmount">Damage amount to apply.</param>
        /// <param name="hitPoint">World-space hit point.</param>
        /// <param name="sourceWeaponId">Weapon identifier that caused the damage.</param>
        /// <returns>True when damage was applied; otherwise false.</returns>
        bool TryApplyDamage(int damageAmount, Vector2 hitPoint, string sourceWeaponId);
    }
}
