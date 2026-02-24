Goal: Implement deterministic, data-driven Enemy AI (Patrol -> Chase -> Attack) that fits the existing level-loading and gameplay loop.

Constraints:
- Unity 2022.3 2D project and existing script structure under `zmbySurv/Assets/Scripts/`.
- Keep `EnemyController` as a thin `MonoBehaviour`; place AI behavior logic in focused classes.
- Respect current data contract: zombie configuration is applied from `LevelLoader` via `ApplyLevelConfiguration`.
- Preserve existing player damage and level flow behavior (`PlayerController.ReduceLife`, `LevelManager` restart/transition).
- Follow `AGENTS.md` standards: SOLID, XML docs for public APIs, structured logs, and no new warnings/errors.

Current State Analysis:
- `EnemyController` already exposes helper methods (`MoveTo`, `IsPlayerVisible`, `IsPlayerInAttackRange`, `Attack`, `PlayAnimation`) and receives per-level config from JSON through `LevelLoader`.
- Enemy lifecycle methods (`Awake`, `Start`, `Update`) are currently empty, so no active state machine or transition logic runs.
- Patrol routes are already built from level data (`BuildPatrolPoints`) and passed to each enemy instance.
- `readme.md` requires continuous combat pressure in the main loop: zombies patrol, chase on proximity, and attack in range while the player collects coins.
- `readme.md` also requires deterministic level flow, so Enemy AI transitions must avoid oscillation and duplicate attacks.

Plan:
1. [status: in_progress] Define Enemy AI contracts and runtime context.
   Deliverable: Add `IEnemyState` and `EnemyStateContext` (or equivalent) with explicit responsibilities for enter/tick/exit and shared runtime data access.
   Validation: Project compiles; all new public APIs have XML docs; no state logic in `EnemyController` beyond orchestration.
2. [status: pending] Implement a focused state machine coordinator.
   Deliverable: Add `EnemyStateMachine` with controlled `ChangeState` flow, transition guards, and structured transition logs (`from -> to` with reason).
   Validation: Runtime logs show valid transition sequence without null-state errors in Play Mode.
3. [status: pending] Implement Patrol behavior from configured route data.
   Deliverable: Add `PatrolState` that iterates `patrolPoints`, moves with patrol speed, and switches only when visibility rules are met.
   Validation: Enemy follows route points in-order and loops safely when player is not detected.
4. [status: pending] Implement Chase behavior with stable range handling.
   Deliverable: Add `ChaseState` that pursues `PlayerTarget` using chase speed, returns to patrol when target is lost, and hands off to attack in range.
   Validation: Enemy reliably enters chase when visible and exits chase when player leaves detection range.
5. [status: pending] Implement Attack behavior with cooldown and safety checks.
   Deliverable: Add `AttackState` that stops movement while attacking, applies damage via `Attack()`, enforces attack interval, and exits correctly when out of range.
   Validation: Damage is not applied every frame; player receives expected periodic damage only when in attack range.
6. [status: pending] Integrate state machine into `EnemyController` lifecycle and verify level flow compatibility.
   Deliverable: Wire initialization in `Awake/Start`, state ticking in `Update`, and safe teardown in `OnDisable/OnDestroy`; add critical logs for initialization and transitions.
   Validation: Full gameplay check across level load, restart on death, and next-level transition confirms enemies reinitialize correctly without stale state.

Risks:
- Transition thrashing near sight/attack thresholds -> add hysteresis or cooldown-based transition guards.
- Missing player or empty patrol data at runtime -> fail fast with clear error logs and fallback to idle/patrol-safe behavior.
- Attack cadence tied to frame rate -> use explicit timers based on `Time.time` or accumulated delta time.
- Hidden coupling with `LevelLoader` spawn timing -> initialize AI only after required references/config are assigned.

Validation:
- Unity compile check with zero new warnings/errors after each phase.
- Manual Play Mode validation: patrol path follow, chase trigger, attack trigger/cooldown, and loss-of-target fallback.
- Regression flow check from `readme.md`: coin collection loop, home activation, level completion, and death restart remain stable with active enemies.
