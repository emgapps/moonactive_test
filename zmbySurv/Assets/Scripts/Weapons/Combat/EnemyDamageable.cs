using UnityEngine;

namespace Weapons.Combat
{
    /// <summary>
    /// Default enemy damage receiver implementation used by weapon hit resolution.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyDamageable : MonoBehaviour, IEnemyDamageable
    {
        #region Serialized Fields

        [Header("Health")]
        [SerializeField]
        private int m_MaxHealth = 20;
        [SerializeField]
        private bool m_DestroyOnDeath = true;

        #endregion

        #region Private Fields

        private int m_CurrentHealth;
        private bool m_IsAlive;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets whether this target is alive.
        /// </summary>
        public bool IsAlive => m_IsAlive;

        /// <summary>
        /// Gets transform used as damage target root.
        /// </summary>
        public Transform DamageTransform => transform;

        /// <summary>
        /// Gets current health value.
        /// </summary>
        public int CurrentHealth => m_CurrentHealth;

        /// <summary>
        /// Gets configured max health value.
        /// </summary>
        public int MaxHealth => m_MaxHealth;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ResetHealthInternal(Mathf.Max(1, m_MaxHealth));
        }

        #endregion

        #region Public API

        /// <summary>
        /// Configures max health and resets current health.
        /// </summary>
        /// <param name="maxHealth">New max health value.</param>
        public void ConfigureHealth(int maxHealth)
        {
            m_MaxHealth = Mathf.Max(1, maxHealth);
            ResetHealthInternal(m_MaxHealth);
        }

        /// <summary>
        /// Attempts to apply damage from weapon hit.
        /// </summary>
        /// <param name="damageAmount">Incoming damage amount.</param>
        /// <param name="hitPoint">World-space hit position.</param>
        /// <param name="sourceWeaponId">Source weapon identifier.</param>
        /// <returns>True when damage was applied; otherwise false.</returns>
        public bool TryApplyDamage(int damageAmount, Vector2 hitPoint, string sourceWeaponId)
        {
            if (!m_IsAlive)
            {
                return false;
            }

            if (damageAmount <= 0)
            {
                Debug.LogWarning($"[Weapons] DamageIgnored | enemy={name} reason=non_positive_damage value={damageAmount}");
                return false;
            }

            m_CurrentHealth -= damageAmount;
            Debug.Log(
                $"[Weapons] DamageApplied | enemy={name} weapon={sourceWeaponId} damage={damageAmount} health={m_CurrentHealth}/{m_MaxHealth} hit=({hitPoint.x:0.00},{hitPoint.y:0.00})");

            if (m_CurrentHealth > 0)
            {
                return true;
            }

            m_CurrentHealth = 0;
            m_IsAlive = false;

            Debug.Log($"[Weapons] EnemyDefeated | enemy={name} weapon={sourceWeaponId}");

            if (m_DestroyOnDeath)
            {
                Destroy(gameObject);
            }

            return true;
        }

        #endregion

        #region Private Helpers

        private void ResetHealthInternal(int health)
        {
            m_CurrentHealth = Mathf.Max(1, health);
            m_IsAlive = true;
        }

        #endregion
    }
}
