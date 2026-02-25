# Shots Animation Implementation Plan

Goal: Implement pooled visual bullets spawned on `WeaponShotRequest`, synchronized with weapon trace-hit logic, and despawned on enemy/wall/range endpoints while preserving existing hitscan damage behavior.

Constraints:
- Keep `WeaponRuntime` runtime-only (no `MonoBehaviour`/pool ownership).
- Use `GenericObjectPool<T>` for bullet instances.
- Introduce an abstraction between trace resolution and bullet GameObject control.
- Follow SOLID, naming conventions, XML-doc requirements, and structured logging rules from `AGENTS.md`.
- Deliver incremental, testable phases with commit after each phase.

## Common Verification Commands (run after each phase)

Project path:
- `/Users/artemhulmetov/moonactive_test/zmbySurv`

Static analysis / compile gate:
```bash
/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath /Users/artemhulmetov/moonactive_test/zmbySurv \
  -logFile /Users/artemhulmetov/moonactive_test/tmp_shots_phase<PHASE>_compile.log
```

EditMode tests:
```bash
/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -runTests -testPlatform EditMode \
  -projectPath /Users/artemhulmetov/moonactive_test/zmbySurv \
  -testResults /Users/artemhulmetov/moonactive_test/tmp_shots_phase<PHASE>_editmode.xml \
  -logFile /Users/artemhulmetov/moonactive_test/tmp_shots_phase<PHASE>_editmode.log
```

PlayMode tests:
```bash
/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -runTests -testPlatform PlayMode \
  -projectPath /Users/artemhulmetov/moonactive_test/zmbySurv \
  -testResults /Users/artemhulmetov/moonactive_test/tmp_shots_phase<PHASE>_playmode.xml \
  -logFile /Users/artemhulmetov/moonactive_test/tmp_shots_phase<PHASE>_playmode.log
```

Log checks after each run:
- No `error CS` entries.
- No new `warning CS` entries in modified scope.
- Test runner summary indicates `0` failures.

## Phases

### Phase 1: Trace Dispatch Abstraction
Deliverables:
- Add immutable shot-trace payload (per pellet) describing origin, direction, endpoint, and impact type.
- Add trace dispatch abstraction interface.
- Extend `WeaponHitResolver` to emit trace payloads while preserving existing damage logic.
- Add EditMode tests for resolver trace payload emission and hit classification.

Commit:
- `phase 1: add shot trace dispatch abstraction and resolver emission`

### Phase 2: Bullet Runtime Entity
Deliverables:
- Add `Bullet : MonoBehaviour, IPoolable` with launch/move/complete lifecycle.
- Bullet movement follows emitted trace endpoint and despawns when destination reached.
- Add EditMode tests for bullet lifecycle and motion completion behavior.

Commit:
- `phase 2: add pooled bullet runtime entity`

### Phase 3: BulletSpawner Pool Integration
Deliverables:
- Add `BulletSpawner` using `GenericObjectPool<Bullet>`.
- Implement safe pool get/release/clear with ownership guards and logs.
- Add abstraction implementation in spawner to consume trace payload and spawn bullets.
- Add EditMode tests for spawner pool behavior.

Commit:
- `phase 3: add bullet spawner with generic object pool`

### Phase 4: Weapon Pipeline Wiring
Deliverables:
- Wire `PlayerWeaponController` and `WeaponHitResolver` to dispatch trace payloads into `BulletSpawner` through abstraction.
- Keep gameplay hits authoritative and backward-compatible if spawner is absent.
- Add/adjust EditMode tests for controller orchestration.

Commit:
- `phase 4: wire weapon shot trace pipeline to bullet visuals`

### Phase 5: PlayMode Integration Coverage
Deliverables:
- Add PlayMode integration tests validating bullet despawn on enemy impact.
- Add PlayMode integration tests validating bullet despawn on wall impact.
- Add PlayMode integration tests validating bullet despawn at max-range endpoint when no collision.

Commit:
- `phase 5: add playmode bullet integration coverage`

### Phase 6: Final Hardening and Regression
Deliverables:
- Verify all affected tests and flows remain stable.
- Ensure no compiler warnings/errors are introduced.
- Final pass on logs and XML docs for new public APIs.

Commit:
- `phase 6: harden shot animation flow and finalize regression`

Risks:
- `RaycastNonAlloc` ordering can differ by collider setup -> explicitly resolve nearest valid impact endpoint.
- Visual bullets can appear out of sync with instant damage -> endpoint is derived from exact same resolver pass and direction.
- Missing scene/prefab references -> keep null-safe fallback with warnings while preserving damage flow.
- Unity batchmode can fail if project is open in another Editor instance -> close active Unity instance before validation commands.
