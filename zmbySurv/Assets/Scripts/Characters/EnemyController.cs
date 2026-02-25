using System.Collections;
using System.Collections.Generic;
using Characters.EnemyAI;
using UnityEngine;

namespace Characters
{
    /// <summary>
    /// Controls enemy AI behavior and orchestrates patrol, chase, and attack states.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class EnemyController : MonoBehaviour, IEnemyController
    {
        #region Serialized Fields

        [Header("Movement Settings")]
        [SerializeField]
        private float m_MoveSpeed = 3f;
        [SerializeField]
        private float m_RotationSpeed = 5f;
        [SerializeField]
        private float m_ChaseSpeed = 5f;

        [Header("Detection Settings")]
        [SerializeField]
        private float m_SightRange = 10f;
        [SerializeField]
        private float m_AttackRange = 0.5f;

        [Header("Combat Settings")]
        [SerializeField]
        private int m_AttackPower = 1;
        [SerializeField]
        private float m_AttackCooldownSeconds = 0.8f;

        [Header("AI State Settings")]
        [SerializeField]
        private float m_StateTransitionIntervalSeconds = 0.1f;

        [Header("Environment References")]
        [SerializeField]
        private Rigidbody2D rb;
        [SerializeField]
        private Animator m_Animator;
        [SerializeField]
        private SpriteRenderer m_EnemySpriteRenderer;

        [Header("Damage Feedback")]
        [SerializeField]
        private Color m_DamageFlashColor = Color.red;
        [SerializeField]
        private float m_DamageFlashDurationSeconds = 0.1f;

        #endregion

        #region Private Data

        /// <summary>
        /// Backing field for player target assignment.
        /// </summary>
        private PlayerController m_Player;

        /// <summary>
        /// Backing field for patrol route points.
        /// </summary>
        private List<Vector2> m_PatrolPoints = new List<Vector2>();

        #endregion

        #region Private Fields

        /// <summary>
        /// AI state context shared by all enemy states.
        /// </summary>
        private EnemyStateContext m_StateContext;

        /// <summary>
        /// AI state machine responsible for transitions and ticking.
        /// </summary>
        private EnemyStateMachine m_StateMachine;

        private PatrolState m_PatrolState;
        private ChaseState m_ChaseState;
        private AttackState m_AttackState;

        private bool m_IsAiInitialized;
        private bool m_HasLoggedMissingPlayer;
        private bool m_IsDamageFlashActive;
        private bool m_HasCachedDefaultSpriteColor;
        private Color m_DefaultSpriteColor = Color.white;
        private Coroutine m_DamageFlashCoroutine;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets enemy name used in diagnostics.
        /// </summary>
        public string ControllerName => name;

        /// <summary>
        /// Gets or sets the player target used by the enemy.
        /// </summary>
        public PlayerController Player
        {
            get => m_Player;
            set
            {
                m_Player = value;
                m_HasLoggedMissingPlayer = false;
            }
        }

        /// <summary>
        /// Gets the patrol route points currently configured for this enemy.
        /// </summary>
        public IReadOnlyList<Vector2> PatrolPoints => m_PatrolPoints;

        /// <summary>
        /// Gets the current player target transform.
        /// </summary>
        public Transform PlayerTarget => Player != null ? Player.transform : null;

        /// <summary>
        /// Gets patrol movement speed.
        /// </summary>
        public float PatrolSpeed => m_MoveSpeed;

        /// <summary>
        /// Gets chase movement speed.
        /// </summary>
        public float ChaseSpeed => m_ChaseSpeed;

        /// <summary>
        /// Gets current world-space enemy position.
        /// </summary>
        public Vector2 CurrentPosition => transform.position;

        /// <summary>
        /// Gets current AI state name for diagnostics.
        /// </summary>
        public string CurrentStateName => m_StateMachine != null ? m_StateMachine.CurrentStateName : "None";

        #endregion

        #region Unity Lifecycle Methods

        /// <summary>
        /// Caches required component references.
        /// </summary>
        private void Awake()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            if (m_Animator == null)
            {
                m_Animator = GetComponent<Animator>();
            }

            if (m_EnemySpriteRenderer == null)
            {
                m_EnemySpriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (m_EnemySpriteRenderer == null)
            {
                m_EnemySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (m_EnemySpriteRenderer != null)
            {
                m_DefaultSpriteColor = m_EnemySpriteRenderer.color;
                m_HasCachedDefaultSpriteColor = true;
            }

            if (rb == null || m_Animator == null)
            {
                Debug.LogError($"[EnemyAI] InitializationFailed | enemy={name} reason=missing_components");
            }
        }

        /// <summary>
        /// Attempts to initialize enemy AI runtime.
        /// </summary>
        private void Start()
        {
            TryInitializeAi("start");
        }

        /// <summary>
        /// Ticks enemy AI each frame after initialization.
        /// </summary>
        private void Update()
        {
            if (!m_IsAiInitialized)
            {
                if (!TryInitializeAi("update_retry"))
                {
                    return;
                }
            }

            m_StateMachine.Tick(Time.deltaTime);
        }

        /// <summary>
        /// Ensures AI runtime is torn down safely when object is disabled.
        /// </summary>
        private void OnDisable()
        {
            ResetDamageFeedbackState();
            TeardownAi("disabled");
        }

        /// <summary>
        /// Ensures AI runtime is torn down safely when object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            ResetDamageFeedbackState();
            TeardownAi("destroyed");
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Applies level-specific enemy configuration loaded from level data.
        /// </summary>
        /// <param name="moveSpeed">Patrol movement speed.</param>
        /// <param name="chaseSpeed">Chase movement speed.</param>
        /// <param name="sightRange">Player detection range.</param>
        /// <param name="attackRange">Attack execution range.</param>
        /// <param name="attackPower">Damage dealt per successful attack.</param>
        /// <param name="patrolPoints">Patrol route points.</param>
        public void ApplyLevelConfiguration(
            float moveSpeed,
            float chaseSpeed,
            float sightRange,
            float attackRange,
            int attackPower,
            List<Vector2> patrolPoints)
        {
            m_MoveSpeed = moveSpeed;
            m_ChaseSpeed = chaseSpeed;
            m_SightRange = sightRange;
            m_AttackRange = attackRange;
            m_AttackPower = attackPower;
            m_PatrolPoints = patrolPoints != null ? new List<Vector2>(patrolPoints) : new List<Vector2>();

            if (m_PatrolPoints.Count == 0)
            {
                m_PatrolPoints.Add(transform.position);
            }

            Debug.Log(
                $"[EnemyAI] ConfigApplied | enemy={name} move={moveSpeed} chase={chaseSpeed} sight={sightRange} attackRange={attackRange} attackPower={attackPower} patrolPoints={m_PatrolPoints.Count}");

            if (m_StateContext != null)
            {
                m_StateContext.Reset();
                RequestStateChange(m_PatrolState, "level_configuration_updated");
            }
        }

        /// <summary>
        /// Moves the enemy towards a specified target position.
        /// </summary>
        /// <param name="target">The target position to move towards.</param>
        public void MoveTo(Vector2 target)
        {
            float speed = IsPlayerVisible() ? m_ChaseSpeed : m_MoveSpeed;
            MoveTo(target, speed);
        }

        /// <summary>
        /// Moves the enemy towards a specified position using explicit movement speed.
        /// </summary>
        /// <param name="target">Target world position.</param>
        /// <param name="movementSpeed">Movement speed in world units per second.</param>
        public void MoveTo(Vector2 target, float movementSpeed)
        {
            if (rb == null)
            {
                return;
            }

            Vector2 currentPos = rb.position;
            Vector2 direction = target - currentPos;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            direction.Normalize();
            rb.velocity = direction * Mathf.Max(0f, movementSpeed);

            if (direction.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * m_RotationSpeed);
            }
        }

        /// <summary>
        /// Stops enemy movement immediately.
        /// </summary>
        public void StopMovement()
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }

        /// <summary>
        /// Plays the specified animation on the enemy's animator.
        /// </summary>
        /// <param name="animation">The name of the animation to play.</param>
        public void PlayAnimation(string animation)
        {
            if (m_Animator == null || string.IsNullOrWhiteSpace(animation))
            {
                return;
            }

            m_Animator.Play(animation);
        }

        /// <summary>
        /// Checks if the player is within the enemy's sight range.
        /// </summary>
        /// <returns>True if the player is visible, false otherwise.</returns>
        public bool IsPlayerVisible()
        {
            Transform playerTarget = PlayerTarget;
            if (playerTarget == null)
            {
                return false;
            }

            float distance = Vector2.Distance(transform.position, playerTarget.position);
            return distance <= m_SightRange;
        }

        /// <summary>
        /// Checks if the player is within the enemy's attack range.
        /// </summary>
        /// <returns>True if the player is in attack range, false otherwise.</returns>
        public bool IsPlayerInAttackRange()
        {
            Transform playerTarget = PlayerTarget;
            if (playerTarget == null)
            {
                return false;
            }

            float distance = Vector2.Distance(transform.position, playerTarget.position);
            return distance <= m_AttackRange;
        }

        /// <summary>
        /// Executes an attack on the player, dealing damage based on attack power.
        /// </summary>
        public void Attack()
        {
            if (Player == null)
            {
                Debug.LogWarning($"[EnemyAI] AttackSkipped | enemy={name} reason=missing_player");
                return;
            }

            Debug.Log($"[EnemyAI] Attack | enemy={name} player={Player.name} power={m_AttackPower}");
            Player.ReduceLife(m_AttackPower);
        }

        /// <summary>
        /// Handles incoming damage feedback with a short debounced flash.
        /// </summary>
        public void OnDamage()
        {
            if (!TryResolveDamageFeedbackRenderer())
            {
                Debug.LogWarning($"[EnemyAI] DamageFeedbackSkipped | enemy={name} reason=missing_sprite_renderer");
                return;
            }

            if (m_IsDamageFlashActive)
            {
                return;
            }

            m_EnemySpriteRenderer.color = m_DamageFlashColor;
            m_IsDamageFlashActive = true;
            float flashDurationSeconds = Mathf.Max(0.01f, m_DamageFlashDurationSeconds);
            m_DamageFlashCoroutine = StartCoroutine(ResetDamageFeedbackAfterDelay(flashDurationSeconds));
        }

        #endregion

        #region Private Helper Methods

        private bool TryInitializeAi(string source)
        {
            if (m_IsAiInitialized)
            {
                return true;
            }

            if (rb == null || m_Animator == null)
            {
                Debug.LogError(
                    $"[EnemyAI] InitializationFailed | enemy={name} source={source} reason=missing_components");
                return false;
            }

            if (Player == null)
            {
                if (!m_HasLoggedMissingPlayer)
                {
                    Debug.LogWarning(
                        $"[EnemyAI] InitializationDelayed | enemy={name} source={source} reason=missing_player_reference");
                    m_HasLoggedMissingPlayer = true;
                }

                return false;
            }

            m_HasLoggedMissingPlayer = false;
            if (m_PatrolPoints == null || m_PatrolPoints.Count == 0)
            {
                m_PatrolPoints = new List<Vector2> { transform.position };
                Debug.LogWarning(
                    $"[EnemyAI] PatrolFallback | enemy={name} reason=missing_patrol_points fallback=current_position");
            }

            m_StateContext = new EnemyStateContext(this);
            m_StateMachine = new EnemyStateMachine(m_StateContext, m_StateTransitionIntervalSeconds);

            m_PatrolState = new PatrolState(
                m_StateContext,
                () => RequestStateChange(m_ChaseState, "player_visible"));
            m_ChaseState = new ChaseState(
                m_StateContext,
                () => RequestStateChange(m_PatrolState, "player_lost"),
                () => RequestStateChange(m_AttackState, "player_in_attack_range"));
            m_AttackState = new AttackState(
                m_StateContext,
                () => RequestStateChange(m_ChaseState, "target_out_of_attack_range"),
                () => RequestStateChange(m_PatrolState, "target_lost"),
                m_AttackCooldownSeconds);

            m_StateContext.Reset();
            m_StateMachine.TryChangeState(m_PatrolState, "initial_state");
            m_IsAiInitialized = true;

            Debug.Log($"[EnemyAI] Initialized | enemy={name} source={source} state={CurrentStateName}");
            return true;
        }

        private void RequestStateChange(IEnemyState nextState, string reason)
        {
            if (m_StateMachine == null || nextState == null)
            {
                return;
            }

            m_StateMachine.TryChangeState(nextState, reason);
        }

        private void TeardownAi(string reason)
        {
            if (m_StateMachine == null && !m_IsAiInitialized)
            {
                return;
            }

            m_StateMachine?.Reset();
            StopMovement();

            m_StateMachine = null;
            m_StateContext = null;
            m_PatrolState = null;
            m_ChaseState = null;
            m_AttackState = null;
            m_IsAiInitialized = false;

            Debug.Log($"[EnemyAI] Teardown | enemy={name} reason={reason}");
        }

        private bool TryResolveDamageFeedbackRenderer()
        {
            if (m_EnemySpriteRenderer == null)
            {
                m_EnemySpriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (m_EnemySpriteRenderer == null)
            {
                m_EnemySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (m_EnemySpriteRenderer == null)
            {
                return false;
            }

            if (!m_HasCachedDefaultSpriteColor)
            {
                m_DefaultSpriteColor = m_EnemySpriteRenderer.color;
                m_HasCachedDefaultSpriteColor = true;
            }

            return true;
        }

        private IEnumerator ResetDamageFeedbackAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            m_DamageFlashCoroutine = null;
            ResetDamageFeedbackState();
        }

        private void ResetDamageFeedbackState()
        {
            if (m_DamageFlashCoroutine != null)
            {
                StopCoroutine(m_DamageFlashCoroutine);
                m_DamageFlashCoroutine = null;
            }

            if (m_EnemySpriteRenderer != null && m_HasCachedDefaultSpriteColor)
            {
                m_EnemySpriteRenderer.color = m_DefaultSpriteColor;
            }

            m_IsDamageFlashActive = false;
        }

        #endregion
    }
}
