Goal: Implement deterministic, data-driven Enemy AI (Patrol -> Chase -> Attack) that fits the existing level-loading and gameplay loop.

Constraints:
- Unity 2022.3 2D project and existing script structure under `zmbySurv/Assets/Scripts/`.
- Keep `EnemyController` as a thin `MonoBehaviour`; place AI behavior logic in focused classes.
- Respect current data contract: zombie configuration is applied from `LevelLoader` via `ApplyLevelConfiguration`.
- Preserve existing player damage and level flow behavior (`PlayerController.ReduceLife`, `LevelManager` restart/transition).
- Follow `AGENTS.md` standards: SOLID, XML docs for public APIs, structured logs, and no new warnings/errors.

Current State Analysis:
- `EnemyController` now initializes and runs AI in `Awake`/`Start`/`Update`, with safe teardown in `OnDisable`/`OnDestroy`.
- AI architecture is implemented under `Characters/EnemyAI` with `IEnemyState`, `EnemyStateContext`, and `EnemyStateMachine`.
- Patrol/chase/attack behaviors are implemented as dedicated states (`PatrolState`, `ChaseState`, `AttackState`) with explicit transition callbacks.
- Patrol routes from level JSON are mapped via `LevelLoader.BuildPatrolPoints` and applied through `EnemyController.ApplyLevelConfiguration`.
- Transition throttling (`m_StateTransitionIntervalSeconds`), lost-sight grace (`ChaseState`), and attack cooldown (`AttackState`) reduce oscillation and frame-rate-coupled damage.

Plan:
1. [status: completed] Define Enemy AI contracts and runtime context.
   Deliverable: `IEnemyState` and `EnemyStateContext` with enter/tick/exit responsibilities and shared runtime data.
   Validation: Public API docs present and state responsibilities remain outside `EnemyController`.
2. [status: completed] Implement a focused state machine coordinator.
   Deliverable: `EnemyStateMachine` with guarded `TryChangeState` flow and structured transition logs (`from -> to`).
   Validation: Runtime logs expose valid transition sequence without null-state errors.
3. [status: completed] Implement Patrol behavior from configured route data.
   Deliverable: `PatrolState` loops configured points and hands off to chase on visibility.
   Validation: Enemies follow patrol routes in order when player is not detected.
4. [status: completed] Implement Chase behavior with stable range handling.
   Deliverable: `ChaseState` pursues player target, applies lost-sight grace, and transitions to attack in range.
   Validation: Enemies enter chase on detection and fall back to patrol when target is lost.
5. [status: completed] Implement Attack behavior with cooldown and safety checks.
   Deliverable: `AttackState` stops movement while attacking, enforces attack interval, and exits on lost/out-of-range targets.
   Validation: Damage cadence is periodic (not per-frame) and constrained to attack range.
6. [status: completed] Integrate state machine into enemy lifecycle and level flow.
   Deliverable: `EnemyController` orchestration for init/tick/teardown plus configuration re-application behavior.
   Validation: Enemy AI reinitializes correctly on load/restart/transition paths.

Risks:
- Transition thrashing near thresholds -> mitigated via transition interval and chase lost-sight grace period.
- Missing player/patrol references at runtime -> mitigated with delayed initialization, fallback patrol point, and explicit logs.
- Future coupling with weapons/combat changes -> keep AI behavior boundaries stable and integration points explicit.

Validation:
- Implementation review confirms expected state architecture and lifecycle wiring in runtime scripts.
- Structured logs are present for initialization, transitions, attack execution, and teardown.
- Remaining verification dependency is full Play Mode regression run together with next feature increments.
