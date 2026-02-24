Goal: Replace runtime coin `Instantiate`/`Destroy` paths with a reusable generic object pool integrated into coin spawning and cleanup flow.

Constraints:
- Scope is Object Pooling only (generic pool + coin integration points from `.codex/task.md`: `CoinSpawner.Awake()`, `SpawnNewCoin()`, `ClearAllCoins()`).
- Keep current level loop from `readme.md` stable: load level -> spawn/collect coins -> unlock home -> transition/restart.
- Pool must be generic for any `Component` type and expose clean API: `Get`, `Release`, `Clear`.
- Maintain deterministic coin target logic (`currentCurrency + activeCoins < targetCurrency`) and existing event subscriptions.
- Follow `AGENTS.md`: SOLID, explicit logs for critical flow, no per-frame log spam, no new warnings/errors.

Current State Analysis:
- No generic pool implementation exists under `zmbySurv/Assets/Scripts/`.
- `CoinSpawner.SpawnNewCoin()` currently allocates with `Instantiate(coinPrefab, transform)` and increments `m_CoinAmount`.
- `Coin.OnTriggerEnter2D()` currently calls `Destroy(gameObject)` after currency grant; this bypasses any reuse lifecycle.
- `CoinSpawner.ClearAllCoins()` destroys all child objects on transitions; this creates allocation churn when the next level starts.
- `LevelLoader.CleanupLevel()` already centralizes coin cleanup through `coinSpawner.ClearAllCoins()`, which is the correct hook for pool release.

General Requirements (from `readme.md` and `.codex/task.md`):
- Remove gameplay-time allocation spikes tied to coin lifecycle.
- Keep architecture reusable so pool can later support projectiles/weapons.
- Preserve behavior parity: same coin cap behavior, same collection flow, same transition behavior.
- Failure paths must be explicit (missing prefab/spawner references should log actionable errors).
- Add automated coverage for pooling behavior via unit tests and gameplay integration tests.

Plan:
1. [status: in_progress] Define pool architecture and ownership boundaries.
   Deliverable: Finalize `GenericObjectPool<T>` responsibility and `CoinSpawner` ownership model (single spawner-owned pool instance).
   Validation: Design review checklist confirms no circular ownership and clear `Get/Release/Clear` semantics.
2. [status: pending] Implement `GenericObjectPool<T>` with safe lifecycle hooks.
   Deliverable: Add reusable generic class with constructor-based create/get/release callbacks, optional prewarm, active/inactive tracking, and `Clear` disposal policy.
   Validation: Compile check and debug logs confirm balanced `Get`/`Release` counts in basic runtime usage.
3. [status: pending] Initialize and configure pool in `CoinSpawner.Awake`.
   Deliverable: Create pool instance using `coinPrefab`, parent transform, and reset hooks for pooled coins (activate/deactivate, transform reset).
   Validation: `Awake` logs show pool initialization once; no runtime `Instantiate` in normal spawn flow.
4. [status: pending] Replace coin spawn/clear paths with pool operations.
   Deliverable: `SpawnNewCoin()` uses `Get`, `ClearAllCoins()` releases active coins, and level load/reset paths remain deterministic.
   Validation: Runtime confirms stable active coin cap across load, collect, restart, and next-level transitions.
5. [status: pending] Update `Coin` collection lifecycle to release instead of destroy.
   Deliverable: `Coin` notifies spawner/pool on collection, includes guards against double release, and preserves currency award behavior.
   Validation: Collecting a coin increases currency once and returns the same instance to pool without duplication.
6. [status: pending] Cover `GenericObjectPool` with unit and integration tests.
   Deliverable: Add EditMode tests for pool invariants (`Get`/`Release`/`Clear`, double-release guard, reuse ordering) and PlayMode integration tests for `CoinSpawner`/`Coin` pool lifecycle across collect and level transition.
   Validation: Unity Test Runner passes the new EditMode + PlayMode suites with deterministic results.
7. [status: pending] Verify end-to-end behavior and regression risk.
   Deliverable: Manual verification pass for coin loop, home unlock, death restart, and level transitions with pooling enabled.
   Validation: Zero new warnings/errors; no missing-reference exceptions; gameplay behavior matches pre-pooling expectations.

Risks:
- Double release or releasing foreign instances -> maintain active set and reject invalid release attempts with warnings.
- Stale pooled state (position/collider/visibility) -> enforce reset hooks on both `Get` and `Release`.
- Pool clear during active iteration (transition timing) -> snapshot active list before release operations.
- Future weapon/projectile reuse requirements diverging from coin assumptions -> keep pool generic and avoid coin-specific code inside core pool class.

Validation:
- Unity compile with zero new warnings/errors.
- Unity Test Runner pass for object-pooling EditMode and PlayMode suites.
- Play Mode run: spawn, collect, goal reach, restart, and next level transitions.
- Log-based sanity check: no runtime `Instantiate`/`Destroy` calls in coin lifecycle during normal gameplay.
