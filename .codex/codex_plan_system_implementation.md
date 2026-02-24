Goal: Deliver remaining core systems from `readme.md`/`.codex/task.md` as stable, testable Unity increments with minimal regression risk.

Constraints:
- Unity 2022.3 2D project structure and existing folders under `Assets/Scripts`.
- SOLID/OOP and thin `MonoBehaviour` boundaries from `AGENTS.md`.
- Keep increments small and vertically testable.
- No new compiler warnings/errors; public APIs XML-documented.
- Preserve existing gameplay loop while replacing placeholder logic.

Current State:
- Done and integrated: `Level Loading` typed DTO pipeline (`LevelDtos`, `ResourcesLevelDataProvider`, `LevelLoader` typed integration, `CoinSpawner.OnLevelLoaded(LevelDataDto)`, level progression bounds).
- Done and integrated: `Enemy AI Behavior` state architecture (`IEnemyState`, `EnemyStateContext`, `EnemyStateMachine`) and concrete `Patrol`/`Chase`/`Attack` runtime wiring in `EnemyController`.
- Done and integrated: `Generic Object Pool` and coin lifecycle pooling (`GenericObjectPool<T>`, `CoinSpawner` pool ownership, `Coin` return-to-pool flow, pooling test coverage).
- Pending: `Weapons System` scripts/config/UI are still missing.

Plan:
1. [status: done] Level Loading | Phase 1: Add typed level data models and `Resources` provider for `Levels.json`.
   Deliverable: Strongly typed DTOs (`LevelCollectionDto`, `LevelDataDto`, `PlayerConfigDto`, `ZombieConfigDto`, `Vector2Dto`) and provider implementation.
2. [status: done] Level Loading | Phase 2: Integrate typed data into `LevelLoader`, `CoinSpawner`, `LevelManager`, and level progression.
   Deliverable: Implement `InitializeDataProvider`, `ApplyPlayerConfiguration`, `SpawnZombies`; replace `object` APIs; correct last-level detection and next-level bounds.
3. [status: done] Enemy AI | Phase 1: Define behavior architecture (`IEnemyState`, state machine context, transition rules for Patrol/Chase/Attack).
   Deliverable: State interfaces/classes added under `Characters/EnemyAI`, with controller integration points.
4. [status: done] Enemy AI | Phase 2: Implement patrol/chase/attack behaviors and wire into `EnemyController.Awake/Start/Update`.
   Deliverable: Concrete `PatrolState`, `ChaseState`, `AttackState` and state-machine lifecycle integration with guarded transitions.
5. [status: done] Object Pooling | Phase 1: Implement generic reusable pool (`Get`, `Release`, `Clear`) for `Component`.
   Deliverable: `GenericObjectPool<T>` with create/reset hooks, active tracking, and safe clear behavior.
6. [status: done] Object Pooling | Phase 2: Integrate pool into coin lifecycle (`CoinSpawner` + `Coin` return-to-pool flow).
   Deliverable: `CoinSpawner` no longer instantiates at runtime; `Coin` releases itself via spawner callback.
7. [status: done] Object Pooling | Phase 3: Cover `GenericObjectPool` with unit and integration tests.
   Deliverable: Add EditMode tests for pool invariants and PlayMode integration tests for coin-spawner pooling flow.
8. [status: pending] Weapons System | Phase 1: Data + domain setup (weapon configs JSON, weapon definitions, selection state).
   Deliverable: `Weapons.json`, loader/provider, weapon type model (`Pistol`/`Shotgun`/`Machinegun`), selected-weapon persistence per level run.
9. [status: pending] Weapons System | Phase 2: Shooting runtime + UI indicators + enemy damage integration.
   Deliverable: Fire-rate enforcement, magazine/reload logic (`Space` shoot, optional `R` reload), ammo text and reload progress UI, damage pipeline from weapon to enemy.

Risks:
- Pool ownership bugs (double release, leaked active objects) -> track active instances and validate release origin.
- Reused-object stale state (position/collider/visual flags) -> run reset hooks on `Get`/`Release` and clear contracts in coin lifecycle.
- Weapons integration conflicts with current input/movement flow -> isolate weapon runtime behind focused controller/service.

Validation:
- Compile check in Unity with zero new warnings/errors.
- Unity Test Runner pass for new object-pooling tests (EditMode and PlayMode).
- Manual vertical-slice playtests per phase (spawn/load, combat, win/lose, transition).
- Scene reference sanity check for modified scripts/prefabs before sign-off.
