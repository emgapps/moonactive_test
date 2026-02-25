using PlayerMovements;
using UnityEngine;
using UnityEngine.UI;
using Weapons;

namespace Characters
{
    /// <summary>
    /// Controls the player character, handling movement, currency collection, and health management.
    /// Delegates input handling to an IPlayerController implementation.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Movement Settings")]
        [SerializeField]
        private float speed = 5f;

        [Header("Components")]
        [SerializeField]
        private Rigidbody2D m_Rigidbody2D;
        [SerializeField]
        private Animator m_Animator;

        [Header("Stats")]
        [SerializeField]
        private int m_Life;

        [Header("UI References")]
        [SerializeField]
        private Text m_LifeText;
        [SerializeField]
        private Text m_CoinText;

        #endregion

        #region Private Fields

        private int m_Currency;
        private int m_TargetCurrency = 10;
        private Vector2 m_MoveDirection = Vector2.zero;
        private bool m_IsDead = false;
        private bool m_IsInvulnerable = false;
        private PlayerWeaponController m_PlayerWeaponController;

        #endregion

        #region Public Properties

        /// <summary>
        /// The active player controller responsible for handling input.
        /// </summary>
        public IPlayerController controller;

        /// <summary>
        /// Gets the target currency amount required to complete the level.
        /// </summary>
        public int TargetCurrency => m_TargetCurrency;

        /// <summary>
        /// Gets the current currency amount.
        /// </summary>
        public int CurrentCurrency => m_Currency;

        #endregion

        #region Events

        /// <summary>
        /// Delegate for currency change events.
        /// </summary>
        /// <param name="newAmount">The new total currency amount.</param>
        public delegate void CurrencyChanged(int newAmount);

        /// <summary>
        /// Delegate for target currency reached events.
        /// </summary>
        public delegate void TargetCurrencyReached();

        /// <summary>
        /// Delegate for when the player dies.
        /// </summary>
        public delegate void PlayerDied();

        /// <summary>
        /// Event triggered when the player's currency changes.
        /// Subscribers can listen to this event to respond to currency collection.
        /// </summary>
        public event CurrencyChanged OnCurrencyChanged;

        /// <summary>
        /// Event triggered when the player reaches the target currency amount.
        /// Subscribers can use this to trigger special events like spawning the home.
        /// </summary>
        public event TargetCurrencyReached OnTargetCurrencyReached;

        /// <summary>
        /// Event triggered when the player's health reaches zero.
        /// Subscribers can use this to handle game over logic.
        /// </summary>
        public event PlayerDied OnPlayerDied;

        #endregion

        #region Public API Methods

        /// <summary>
        /// Resets the player's currency to zero.
        /// Called when starting a new level.
        /// </summary>
        public void ResetCurrency()
        {
            m_Currency = 0;
            
            // Update UI
            if (m_CoinText != null)
            {
                m_CoinText.text = $"{m_Currency} / {m_TargetCurrency}";
            }
            
            Debug.Log("PlayerController: Currency reset to 0");
        }

        #endregion

        #region Unity Lifecycle Methods

        /// <summary>
        /// Caches required component references and ensures weapon controller is available.
        /// </summary>
        private void Awake()
        {
            if (m_Rigidbody2D == null)
            {
                m_Rigidbody2D = GetComponent<Rigidbody2D>();
            }

            if (m_Animator == null)
            {
                m_Animator = GetComponent<Animator>();
            }

            m_PlayerWeaponController = GetComponent<PlayerWeaponController>();
            if (m_PlayerWeaponController == null)
            {
                m_PlayerWeaponController = gameObject.AddComponent<PlayerWeaponController>();
            }
        }

        /// <summary>
        /// Initializes the player controller and UI elements.
        /// </summary>
        private void Start()
        {
            controller = new HumanController(this);
            
            // Ensure currency starts at 0
            m_Currency = 0;
            
            // Initialize UI
            if (m_LifeText != null)
            {
                m_LifeText.text = $"{m_Life}";
            }
            if (m_CoinText != null)
            {
                m_CoinText.text = $"{m_Currency} / {m_TargetCurrency}";
            }
        }

        /// <summary>
        /// Updates player input and physics each frame.
        /// </summary>
        private void Update()
        {
            if (controller != null)
            {
                controller.UpdateControl();
            }

            // Apply movement velocity
            m_Rigidbody2D.velocity = m_MoveDirection * speed;

            // Handle rotation based on movement direction
            if (m_MoveDirection.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(m_MoveDirection.y, m_MoveDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Applies level configuration to the player.
        /// Used by LevelLoader to set player stats from JSON data.
        /// </summary>
        /// <param name="moveSpeed">The movement speed for this level.</param>
        /// <param name="health">The starting health for this level.</param>
        /// <param name="targetCurrency">The target currency to complete the level.</param>
        public void ApplyLevelConfiguration(float moveSpeed, int health, int targetCurrency)
        {
            speed = moveSpeed;
            m_Life = health;
            m_TargetCurrency = targetCurrency;
            
            // Ensure currency is reset when applying level config
            // (currency should already be 0 from ResetCurrency, but being explicit)
            m_Currency = 0;
            
            // Reset dead flag and invulnerability when starting/restarting level
            m_IsDead = false;
            m_IsInvulnerable = false;

            // Update UI
            if (m_LifeText != null)
            {
                m_LifeText.text = $"{m_Life}";
            }
            if (m_CoinText != null)
            {
                m_CoinText.text = $"{m_Currency} / {m_TargetCurrency}";
            }

            m_PlayerWeaponController?.ResetForLevelStart();

            Debug.Log($"PlayerController: Applied level config - Speed: {moveSpeed}, Health: {health}, Target: {targetCurrency}, Currency: {m_Currency}");
        }

        /// <summary>
        /// Adds currency to the player's total and updates the UI.
        /// Triggers the OnCurrencyChanged event to notify subscribers.
        /// Also triggers OnTargetCurrencyReached when the target is met.
        /// </summary>
        /// <param name="amount">The amount of currency to add.</param>
        public void AddCurrency(int amount)
        {
            m_Currency += amount;
            OnCurrencyChanged?.Invoke(m_Currency);
            
            if (m_CoinText != null)
            {
                m_CoinText.text = $"{m_Currency} / {m_TargetCurrency}";
            }

            Debug.Log($"PlayerController: AddCurrency({amount}) - Currency: {m_Currency}/{m_TargetCurrency}");

            // Check if target currency has been reached
            if (m_Currency >= m_TargetCurrency)
            {
                OnTargetCurrencyReached?.Invoke();
            }
        }

        /// <summary>
        /// Sets the movement direction vector based on controller input.
        /// Updates the animator state based on whether the player is moving or idle.
        /// </summary>
        /// <param name="direction">The direction vector for movement.</param>
        public void SetMoveDirection(Vector3 direction)
        {
            if (direction == Vector3.zero)
            {
                m_Animator.Play("Idle");
            }
            else
            {
                m_Animator.Play("Move");
            }
            m_MoveDirection = direction.normalized;
        }

        /// <summary>
        /// Reduces the player's health by the specified amount and updates the UI.
        /// Triggers death event if health reaches zero.
        /// </summary>
        /// <param name="amount">The amount of health to reduce.</param>
        public void ReduceLife(int amount)
        {
            // Ignore damage if already dead or invulnerable (level complete)
            if (m_IsDead || m_IsInvulnerable)
            {
                return;
            }

            m_Life -= amount;
            
            if (m_LifeText != null)
            {
                m_LifeText.text = $"{m_Life}";
            }
            
            // Check for death
            if (m_Life <= 0)
            {
                m_Life = 0;
                m_IsDead = true;
                Debug.Log("PlayerController: Player died!");
                OnPlayerDied?.Invoke();
            }
        }

        /// <summary>
        /// Sets the player's invulnerability state.
        /// Used when level is complete to prevent further damage.
        /// </summary>
        /// <param name="invulnerable">True to make player invulnerable, false to allow damage.</param>
        public void SetInvulnerable(bool invulnerable)
        {
            m_IsInvulnerable = invulnerable;
            Debug.Log($"PlayerController: Invulnerability set to {invulnerable}");
        }

        #endregion
    }
}
