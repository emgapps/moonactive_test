Goal: Deliver all missing core components from README/task as stable, testable Unity increments with minimal regression risk.

Constraints:
- Unity 2022.3 2D project structure and existing folders under `Assets/Scripts`.
- SOLID/OOP and thin `MonoBehaviour` boundaries from `AGENTS.md`.
- Keep increments small and vertically testable.
- No new compiler warnings/errors; public APIs XML-documented.
- Preserve existing gameplay loop while replacing placeholder logic.

Current State:
- Implemented and usable: `PlayerController`, `Coin`, `Home`, `HomeManager`, `HumanController`.
- Missing/partial `Enemy AI Behavior`: `Awake/Start/Update` are empty and no state architecture exists.
- Missing `Generic Object Pool`: `CoinSpawner` still uses `Instantiate`; coin collection currently destroys coin instances.
- Missing/partial `Level Loading System`: provider and level models are still `object`; three core methods are unimplemented.
- Missing `Weapons System`: no weapons scripts/config file yet; `Assets/Resources/Weapons/Weapons.json` is absent.
- Additional flow risk: `LevelLoader.IsLastLevel()` currently always returns `true`.

Plan:
1. [status: in_progress] Level Loading | Phase 1: Add typed level data models and `Resources` provider for `Levels.json`.
   Deliverable: Strongly typed DTOs (`LevelCollection`, `LevelData`, `PlayerConfig`, `ZombieConfig`, `Vector2Data`) and provider implementation.
2. [status: pending] Level Loading | Phase 2: Integrate typed data into `LevelLoader`, `CoinSpawner`, `LevelManager` and level progression.
   Deliverable: Implement `InitializeDataProvider`, `ApplyPlayerConfiguration`, `SpawnZombies`; replace `object` APIs; correct last-level detection and next-level bounds.
3. [status: pending] Enemy AI | Phase 1: Define behavior architecture (`IEnemyState`, state machine context, transition rules for Patrol/Chase/Attack).
   Deliverable: State interfaces/classes added under a dedicated enemy AI folder, controller integration points defined.
4. [status: pending] Enemy AI | Phase 2: Implement patrol/chase/attack behaviors and wire into `EnemyController.Awake/Start/Update`.
   Deliverable: Concrete `PatrolState`, `ChaseState`, `AttackState` using existing helper methods.
5. [status: pending] Object Pooling | Phase 1: Implement generic reusable pool (`Get`, `Release`, `Clear`) for `Component`.
   Deliverable: `GenericObjectPool<T>` with create/reset hooks and capacity handling.
6. [status: pending] Object Pooling | Phase 2: Integrate pool into coin lifecycle (`CoinSpawner` + `Coin` return-to-pool flow).
   Deliverable: `CoinSpawner` no longer instantiates at runtime; `Coin` releases itself via spawner callback.
7. [status: pending] Weapons System | Phase 1: Data + domain setup (weapon configs JSON, weapon definitions, selection state).
   Deliverable: `Weapons.json`, loader/provider, weapon type model (`Pistol`/`Shotgun`/`Machinegun`), selected-weapon persistence per level run.
8. [status: pending] Weapons System | Phase 2: Shooting runtime + UI indicators + enemy damage integration.
   Deliverable: Fire-rate enforcement, magazine/reload logic (`Space` shoot, optional `R` reload), ammo text and reload progress UI, damage pipeline from weapon to enemy.

Risks:
- JSON/schema mismatch between data models and existing files -> add strict validation + default fallbacks + explicit error logs.
- State transition oscillation near range thresholds -> add transition guards/cooldowns/hysteresis.
- Pool ownership bugs (double release, leaked active objects) -> track active set and validate release origin.
- Weapons integration may conflict with movement/input flow -> isolate weapon input handling behind dedicated controller/service.

Validation:
- Compile check in Unity with zero new warnings/errors.
- Manual vertical-slice playtests after each phase (spawn/load, combat, win/lose, transition).
- Scene reference sanity check for modified scripts/prefabs before sign-off.
