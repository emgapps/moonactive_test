Goal: Replace runtime coin `Instantiate`/`Destroy` paths with a reusable generic object pool integrated into coin spawning and cleanup flow.

Constraints:
- Scope is Object Pooling only (generic pool + coin integration points from `.codex/task.md`: `CoinSpawner.Awake()`, `SpawnNewCoin()`, `ClearAllCoins()`).
- Keep current level loop from `readme.md` stable: load level -> spawn/collect coins -> unlock home -> transition/restart.
- Pool must be generic for any `Component` type and expose clean API: `Get`, `Release`, `Clear`.
- Maintain deterministic coin target logic (`currentCurrency + activeCoins < targetCurrency`) and existing event subscriptions.
- Follow `AGENTS.md`: SOLID, explicit logs for critical flow, no per-frame log spam, no new warnings/errors.

Current State Analysis:
- `Core.Pooling` architecture and contracts are implemented (`IObjectPool<T>`, `IPoolable`).
- `GenericObjectPool<T>` is implemented with lifecycle callbacks, active/inactive tracking, prewarm, guarded release, and clear behavior.
- `CoinSpawner` initializes and owns a single `GenericObjectPool<Coin>` instance and now uses `Get`/`Release` for spawn and clear flows.
- `Coin` collection returns instances to the spawner pool instead of destroying them.
- Automated coverage is implemented with EditMode unit tests and PlayMode integration tests for pooling behavior.

General Requirements (from `readme.md` and `.codex/task.md`):
- Remove gameplay-time allocation spikes tied to coin lifecycle.
- Keep architecture reusable so pool can later support projectiles/weapons.
- Preserve behavior parity: same coin cap behavior, same collection flow, same transition behavior.
- Failure paths must be explicit (missing prefab/spawner references should log actionable errors).
- Add automated coverage for pooling behavior via unit tests and gameplay integration tests.

Plan:
1. [status: done] Define pool architecture and ownership boundaries.
   Deliverable: Finalize `GenericObjectPool<T>` responsibility and `CoinSpawner` ownership model (single spawner-owned pool instance).
   Validation: Design review checklist confirms no circular ownership and clear `Get/Release/Clear` semantics.
2. [status: done] Implement `GenericObjectPool<T>` with safe lifecycle hooks.
   Deliverable: Add reusable generic class with constructor-based create/get/release callbacks, optional prewarm, active/inactive tracking, and `Clear` disposal policy.
   Validation: Compile check and debug logs confirm balanced `Get`/`Release` counts in basic runtime usage.
3. [status: done] Initialize and configure pool in `CoinSpawner.Awake`.
   Deliverable: Create pool instance using `coinPrefab`, parent transform, and reset hooks for pooled coins (activate/deactivate, transform reset).
   Validation: `Awake` logs show pool initialization once; runtime spawn path uses initialized pool.
4. [status: done] Replace coin spawn/clear paths with pool operations.
   Deliverable: `SpawnNewCoin()` uses `Get`, `ClearAllCoins()` releases active coins, and level load/reset paths remain deterministic.
   Validation: Runtime confirms stable active coin cap across load, collect, restart, and next-level transitions.
5. [status: done] Update `Coin` collection lifecycle to release instead of destroy.
   Deliverable: `Coin` notifies spawner/pool on collection, includes guards against double release, and preserves currency award behavior.
   Validation: Collecting a coin increases currency once and returns the same instance to pool without duplication.
6. [status: done] Cover `GenericObjectPool` with unit and integration tests.
   Deliverable: Add EditMode tests for pool invariants (`Get`/`Release`/`Clear`, double-release guard, reuse ordering) and PlayMode integration tests for `CoinSpawner`/`Coin` pool lifecycle across collect and level transition.
   Validation: Unity Test Runner passes the new EditMode + PlayMode suites with deterministic results.
7. [status: done] Verify end-to-end behavior and regression risk.
   Deliverable: Verification report with compile, static-analysis diagnostics scan, and test-suite outcomes.
   Validation: Zero compiler errors/warnings in logs and all existing/new tests pass in batch runs.

Risks:
- Manual gameplay verification in the live project session was not automated from this run context.
- Fallback `Destroy` paths remain intentionally for defensive handling of invalid pool/spawner state.

Validation:
- Unity compile check passed in batch mode (`phase1`..`phase7` verification runs).
- Unity Test Runner passed EditMode and PlayMode suites after each phase.
- Static diagnostic scan (`error CS`/`warning CS`) was executed on compile and test logs after each phase.
- Detailed verification output is recorded in `.codex/object_pooling_verification.md`.
