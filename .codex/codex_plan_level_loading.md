Goal: Implement a typed, validated, JSON-driven level loading pipeline that configures player, zombies, coin spawning, and progression without `object`-based APIs.

Constraints:
- Unity 2022.3 2D project and current folder layout under `zmbySurv/Assets/Scripts/`.
- Keep existing gameplay loop from `readme.md` intact: load config -> apply player/zombies -> collect coins -> unlock home -> complete/restart level.
- Follow interface-based architecture (`ILevelDataProvider`) and keep `MonoBehaviour` classes thin.
- Scope is Level Loading only (data models, provider, loader integration, direct consumers: `CoinSpawner`, `LevelManager`).
- No new compiler warnings/errors; preserve backward compatibility of serialized scene references.

Current State Analysis:
- `LevelLoader` still stores `m_CurrentLevelData` and `m_LevelCollection` as `object`, and `InitializeDataProvider`, `ApplyPlayerConfiguration`, `SpawnZombies` are not implemented.
- `LevelLoader.IsLastLevel()` always returns `true`, and `LoadNextLevel()` has no bounds check against collection size.
- `ILevelDataProvider` returns `Action<object>` instead of a concrete level collection type.
- `CoinSpawner.OnLevelLoaded(object levelData)` ignores real level data and hardcodes `maxCoinsOnBoard = 5`.
- `LevelManager` uses placeholder level naming (`"Level"`) instead of reading `levelName` from current level config.
- `Levels.json` already contains the target data shape (`levelId`, `levelName`, `goalCoins`, `maxCoinsOnBoard`, `playerConfig`, `zombies`, `patrolPath`).

General Requirements Extracted From `readme.md` and task:
- Level data is JSON-driven from `Assets/Resources/Levels/Levels.json`.
- `LevelLoader` and `ILevelDataProvider` coordinate level initialization and callback-based loading (`onSuccess`, `onError`).
- Level config must drive player stats, zombie spawn/configuration, and coin objective balancing.
- The event/dependency flow must remain deterministic across load, restart, next-level transition, and fail states.

Core Level DTO Structure (Level-Related Data Entities):
```csharp
using System;
using System.Collections.Generic;

namespace Level.Data
{
    [Serializable]
    public sealed class LevelCollectionDto
    {
        public List<LevelDataDto> levels;
    }

    [Serializable]
    public sealed class LevelDataDto
    {
        public string levelId;
        public string levelName;
        public int goalCoins;
        public int maxCoinsOnBoard;
        public PlayerConfigDto playerConfig;
        public List<ZombieConfigDto> zombies;
    }

    [Serializable]
    public sealed class PlayerConfigDto
    {
        public float speed;
        public int health;
        public Vector2Dto spawnPosition;
    }

    [Serializable]
    public sealed class ZombieConfigDto
    {
        public string zombieId;
        public Vector2Dto spawnPosition;
        public float moveSpeed;
        public float chaseSpeed;
        public float detectDistance;
        public float attackRange;
        public int attackPower;
        public List<Vector2Dto> patrolPath;
    }

    [Serializable]
    public sealed class Vector2Dto
    {
        public float x;
        public float y;
    }
}
```

DTO Validation Rules:
- `LevelCollectionDto.levels` must exist and contain at least 1 level.
- `LevelDataDto.levelId` and `levelName` must be non-empty.
- `goalCoins` must be `> 0`; `maxCoinsOnBoard` must be `> 0` and `<= goalCoins`.
- `playerConfig` must exist; `speed > 0`; `health > 0`; `spawnPosition` must exist.
- `zombies` may be empty, but when present each zombie must have valid `spawnPosition`, non-negative ranges, and at least 1 patrol point for patrol behavior.
- `detectDistance` maps to `EnemyController.ApplyLevelConfiguration(... sightRange ...)`.

Level Config DTO Consumer Mapping:
- `LevelLoader.ApplyPlayerConfiguration`: uses `playerConfig.speed`, `playerConfig.health`, `goalCoins`, `playerConfig.spawnPosition`.
- `LevelLoader.SpawnZombies`: iterates `zombies`; uses `spawnPosition`, `moveSpeed`, `chaseSpeed`, `detectDistance`, `attackRange`, `attackPower`, `patrolPath`.
- `CoinSpawner.OnLevelLoaded(LevelDataDto)`: uses `maxCoinsOnBoard` and resets internal counters for new level.
- `LevelManager`: reads `levelName` from `LevelLoader.CurrentLevelData` to update UI.
- `LevelLoader.IsLastLevel/LoadNextLevel`: depends on `LevelCollectionDto.levels.Count` and `m_CurrentLevelIndex`.

Plan:
1. [status: completed] Finalize Level DTO contract and validation policy. Deliverable: add `Level.Data` DTO classes + validation utility with explicit error messages for invalid JSON. Validation: compile after adding models and run a lightweight parse test in Play Mode by loading `Levels.json`.
2. [status: completed] Implement `Resources` data provider for typed parsing. Deliverable: create `ResourcesLevelDataProvider : ILevelDataProvider` that loads `TextAsset` by `m_ResourcesPath`, parses with `JsonUtility`, validates, and returns `LevelCollectionDto` via callbacks. Validation: force invalid path/invalid JSON and verify `onError` logging.
3. [status: completed] Replace `object` contracts with typed contracts across level-loading APIs. Deliverable: update `ILevelDataProvider`, `LevelLoader` fields/properties/callback signatures, and `CurrentLevelData`/`CurrentCollection` typing. Validation: compile with zero new warnings and no remaining level-loading `object` placeholders.
4. [status: completed] Implement `LevelLoader` runtime integration methods. Deliverable: complete `InitializeDataProvider`, `ApplyPlayerConfiguration`, `SpawnZombies`, `IsLastLevel`, and safe bounds handling in `LoadNextLevel`. Validation: Play Mode check for first load, restart, next level, and end-of-collection behavior.
5. [status: completed] Integrate downstream level-data consumers. Deliverable: update `CoinSpawner.OnLevelLoaded` to use `maxCoinsOnBoard`; update `LevelManager` level title from `levelName`; keep `HomeManager` event flow unchanged. Validation: observe coin cap changes per level and correct level name updates on load/restart/next.
6. [status: completed] Run end-to-end verification and regression checks for level flow. Deliverable: verification log covering load success/failure, zombie spawn correctness, coin/home progression, death restart, and last-level completion. Validation: Unity compile clean + manual scenario checklist completed.

Risks:
- JSON schema drift between file and DTO field names (notably `detectDistance` vs enemy sight semantics) -> keep DTO names aligned to JSON and map explicitly when applying runtime config.
- Null scene references (`playerController`, `zombiePrefab`, `coinSpawner`, `homeManager`) causing runtime failures -> validate in `Start` and fail fast with actionable errors.
- Out-of-range level index on repeated `LoadNextLevel` calls -> clamp/check index and return `false` without mutating state when at final level.
- Invalid patrol data causing enemy state instability -> sanitize patrol path input and fallback to spawn point when path is missing.
- Partial load application (player configured but zombies not spawned) -> keep `LoadLevel` sequencing explicit and abort level activation on fatal provider/validation errors.

Validation:
- Compile project in Unity with no new warnings/errors.
- Run three manual play passes: first level start, death restart, multi-level progression to final level.
- Confirm level-specific effects are visible: player stats, target coins, max on-board coins, zombie count/config, level name text.
- Confirm failure path behavior with invalid resource path and malformed JSON produces clear error logs and no soft-lock.
