Goal: Implement a configurable, pre-level-selectable weapons system (Pistol, Shotgun, Machinegun) with runtime shooting/reload/ammo UI and deterministic enemy damage integration.

Constraints:
- Unity 2022.3 2D project and existing script structure under `zmbySurv/Assets/Scripts/`.
- Follow `AGENTS.md`: SOLID boundaries, thin `MonoBehaviour`, XML docs on public APIs, structured logs, no new warnings/errors.
- Respect gameplay loop from `readme.md`: level load -> coin objective -> home unlock -> level transition/restart.
- Weapon tuning must be JSON-driven from `Assets/Resources/Weapons/Weapons.json`.
- Player input requirements from `.codex/task.md`: shoot on `Space`, reload flow (auto and/or `R`), fire-rate lockout, magazine constraints.
- Preserve existing level systems already marked done (Level Loading, Enemy AI, Object Pooling) without broad refactors.

Current State Analysis:
- Level pipeline is typed and active (`LevelLoader`, typed DTOs, resources provider); no weapon selection phase exists before gameplay start.
- Enemy AI system is active (`EnemyController` + states) and currently only applies damage from enemy to player (`Attack` -> `PlayerController.ReduceLife`).
- Object pooling is active and reusable (`GenericObjectPool<T>`), currently integrated with coins only.
- No weapon scripts exist under `Assets/Scripts`; no `Weapons` folder, no weapon DTOs/provider, no runtime weapon controller/service.
- No `Assets/Resources/Weapons` folder or `Weapons.json` asset exists in the project.
- Current player input is movement-only (`HumanController.UpdateControl` with WASD), so shooting/reload input has no handler yet.
- Existing UI supports life/coins/level messaging; ammo/reload UI bindings and weapon selection UI are missing.
- Existing tests cover enemy AI and pooling; weapons-specific EditMode/PlayMode tests are absent.

General Requirements (from `readme.md` and `.codex/task.md`):
- Support three weapon types: Pistol, Shotgun, Machinegun.
- Configure each weapon via JSON: damage, magazine size, fire rate, reload time, range.
- Add pre-level weapon selection screen/window with stat visibility and explicit confirm before gameplay.
- Persist selected weapon through the active level run.
- Add runtime indicators: ammo display and reload progress.
- Ensure shooting mechanics enforce cadence and magazine logic under repeated input.

Planned Runtime Architecture:
- `Weapons/Data`: DTOs + validation utilities for weapon configuration.
- `Weapons/Providers`: resources-backed weapon data provider.
- `Weapons/Runtime`: domain abstractions (`IWeapon`, firing/reload state, selection state) independent from scene UI.
- `Weapons/Combat`: hit resolution and enemy damage contract to avoid tight coupling with concrete enemy controller internals.
- `Weapons/UI`: pre-level selection UI controller and in-level ammo/reload HUD presenter.

Plan:
1. [status: in_progress] Finalize weapon contracts, folder layout, and integration boundaries.
   Deliverable: Define concrete interfaces/types (`IWeapon`, `IWeaponConfigProvider`, `IEnemyDamageable`) and target folders under `Assets/Scripts/Weapons`.
   Validation: Design check confirms no circular dependency between `Characters`, `Weapons`, and `Levels`, and only one owner for weapon state.
2. [status: pending] Implement JSON weapon data model and provider.
   Deliverable: Add weapon DTOs, validation utility, and `Resources` provider for `Weapons/Weapons.json` with explicit error paths.
   Validation: EditMode parse tests for valid and invalid JSON; runtime log confirms successful load and selected default weapon id.
3. [status: pending] Implement runtime weapon domain logic.
   Deliverable: Add focused runtime classes for fire cooldown, magazine tracking, reload timing, and per-weapon behavior differences (single, spread, rapid).
   Validation: Deterministic tests verify fire-rate lock, magazine depletion, reload completion timing, and per-type projectile count logic.
4. [status: pending] Implement pre-level weapon selection flow.
   Deliverable: Add selection UI controller/window that appears before gameplay, renders weapon stats, and requires explicit confirm to continue.
   Validation: Manual flow check confirms level load is gated until selection is confirmed and selected weapon state is stored for the run.
5. [status: pending] Integrate player input and runtime shooting orchestration.
   Deliverable: Wire `Space` shoot and `R` reload into weapon runtime (without breaking movement input) via focused input/adapter layer.
   Validation: Play Mode check confirms movement remains responsive while shooting/reloading and no repeated-shot spam bypasses fire rate.
6. [status: pending] Implement hit resolution and enemy damage integration.
   Deliverable: Introduce enemy damage abstraction (`IEnemyDamageable`) and apply weapon damage within configured range, including shotgun spread handling.
   Validation: Play Mode confirms enemies take expected damage per weapon type and out-of-range shots do not apply damage.
7. [status: pending] Implement ammo and reload UI indicators.
   Deliverable: Add HUD presenters for `current/max` ammo and reload progress state synchronized with weapon runtime events.
   Validation: UI updates immediately on shot/reload start/reload complete and remains consistent after level restart/next-level transition.
8. [status: pending] Add automated coverage and full regression verification.
   Deliverable: Add EditMode tests for weapon domain logic and PlayMode integration tests for selection -> combat -> level transition paths.
   Validation: Unity compile clean (no new warnings/errors) and weapons test suites pass alongside existing enemy/pooling suites.

Risks:
- Input conflict between movement and shooting/reload paths -> isolate weapon input adapter and keep movement controller unchanged.
- Tight coupling to `EnemyController` internals -> use `IEnemyDamageable` boundary and minimal adapter component on enemies.
- UI state desync across restart/next-level -> centralize weapon runtime state owner and reset explicitly on level transitions.
- Balance instability from JSON misconfiguration -> enforce strict validation with actionable error logs and safe fallback selection.
- Performance spikes from projectile creation in rapid fire -> plan immediate projectile pooling reuse after baseline weapon loop is stable.

Validation:
- Compile project in Unity with zero new warnings/errors.
- Execute weapon-specific EditMode tests (config validation + fire/reload state machine).
- Execute PlayMode integration path: select weapon -> shoot/reload -> kill zombies -> death restart -> next-level transition.
- Verify scene/prefab references for new UI and weapon components remain intact after reopen/reload.
