# Zombie Survival Game - Codex AI Rules

## Goal
Use AI to deliver stable, production-safe Unity increments for the Zombie Survival Game.

## Stack Context
- Engine: Unity (2D)
- Language: C#
- Project root: `zmbySurv/`
- Main code area: `zmbySurv/Assets/Scripts/`

## Core Engineering Rules

### 1) OOP and SOLID
- Follow Single Responsibility: one class, one clear reason to change.
- Follow Open/Closed: extend behavior via interfaces, composition, and new classes rather than editing stable code paths.
- Follow Liskov Substitution: derived implementations must preserve base/interface behavior.
- Follow Interface Segregation: prefer focused interfaces over broad "god interfaces".
- Follow Dependency Inversion: depend on abstractions (`interface`) instead of concrete implementations.
- Favor composition over inheritance unless inheritance clearly models an is-a relationship.

### 2) Code Quality
- Use descriptive names; avoid ambiguous names like `data`, `obj`, `tmp`, `value`.
- Keep consistent naming.
- `PascalCase` for types, methods, properties, events.
- `camelCase` for local variables and parameters.
- `m_PascalCase` for private serialized fields to align with the existing codebase.
- Add XML docs for all public APIs: public classes, interfaces, enums, methods, properties, events, and delegates.
- Ensure consistent formatting and style in edited files.
- Do not introduce compiler warnings or errors.
- Remove dead code and commented-out blocks instead of leaving clutter.
- Avoid magic numbers and magic strings; extract constants or config objects.

### 3) Folder and Project Structure
- Keep logical feature-based structure under `Assets/Scripts/` (for example: `Characters`, `Coins`, `Levels`, `Combat`, `UI`).
- Place shared abstractions in clear folders like `Core`, `Common`, or `Abstractions` when needed.
- Keep MonoBehaviour classes thin; move business logic to plain C# classes/services.
- Keep scene-specific logic out of reusable systems unless explicitly intended.

## Unity-Specific Rules

### 4) MonoBehaviour Discipline
- Use `Awake` for internal reference setup.
- Use `Start` for runtime initialization that depends on other objects.
- Use `OnEnable`/`OnDisable` (or `OnDestroy`) to pair event subscriptions.
- Do not allocate avoidable garbage each frame in `Update`.
- Do not perform expensive searches (`FindObjectOfType`, repeated `GetComponent`) per frame.
- Cache references and validate serialized fields early.

### 5) Data and Configuration
- Prefer data-driven tuning through ScriptableObjects or serialized config classes for gameplay values.
- Keep level/settings loading code separated from gameplay execution code.
- Validate loaded data and fail with clear errors when invalid.

### 6) Reliability and Safety
- Guard null references at boundaries and log actionable errors.
- Keep side effects explicit.
- Use deterministic state transitions for gameplay flows (spawn, win/lose, level transitions).
- For async/networked loading, always define success/failure/timeout behavior.

### 7) Logging Rules (Development Phase)
- Maximize observability during development; cover gameplay-critical logic with debug logs.
- Log lifecycle and core gameplay events: level load start/success/failure, config apply, spawn/despawn, damage/heal, currency change, win/lose triggers, and scene transitions.
- Log state transitions in explicit form (`from -> to`) with trigger/cause.
- Include useful context in each log: object name/id, level index, position, counts, health/target values, and relevant inputs.
- Use correct severity: `Debug.Log` for expected flow tracing, `Debug.LogWarning` for recoverable or suspicious states, and `Debug.LogError` for failures, missing references, invalid data, and broken flow.
- Use structured, consistent messages: `[System] Action | key=value`.
- Do not spam per-frame logs in `Update`; throttle or guard any temporary frame-level tracing.
- Gate verbose tracing with `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Keep essential warnings/errors active in all builds; trim noisy trace logs before release.

## AI Agent Workflow Rules (Stable Increment Policy)

### 8) Increment Size and Scope
- Deliver changes in small vertical slices that can be tested end-to-end.
- One increment should target one clear behavior change.
- Avoid broad refactors mixed with feature work in the same increment.

### 9) Pre-Change Protocol
- Restate task intent in one sentence.
- Inspect impacted files and dependencies before editing.
- Identify risks and affected systems (player, enemies, levels, UI, save/load).
- If requirements are ambiguous, document assumptions in the output.

### 10) Implementation Protocol
- Prefer minimal changes first, then iterate.
- Preserve backward compatibility unless breaking change is explicitly requested.
- Update or add interfaces/tests/docs together with behavior changes.
- Keep public API changes intentional and documented.
- Add structured debug logs for critical logic paths during development, with noise control and build gating.

### 11) Verification Protocol (Required Before Completion)
- Confirm project still compiles with zero new warnings.
- Validate primary gameplay flow touched by the change.
- Check spawn/load path behavior.
- Check affected player control/combat interactions.
- Check affected win/lose or transition behavior.
- Verify no broken references in modified prefabs/scenes/scripts.
- Provide a short verification summary: what was checked and results.

### 12) Completion Criteria (Definition of Done)
- Feature/fix behaves as requested.
- No new compiler warnings/errors.
- Public APIs are XML-documented.
- Naming/formatting/folder rules are followed.
- Change scope is minimal and logically isolated.
- Risks and follow-ups are clearly listed.

## Decision Heuristics for AI
- Choose clarity over cleverness.
- Prefer explicit, maintainable code over terse abstractions.
- If a quick fix increases long-term instability, implement a small robust fix instead.
- If uncertain between options, choose the one with lower regression risk and easier testability.

## Anti-Patterns to Avoid
- God objects (large classes handling unrelated responsibilities).
- Hidden coupling between unrelated systems.
- Frame-loop heavy logic without profiling/need.
- Mixing data loading, business logic, and UI updates in one class.
- Silent failures without logs or fallback behavior.
- Partial changes without verification steps.

## Output Expectations for AI Tasks
- Summarize what changed and why.
- List touched files.
- Provide verification steps executed.
- Note residual risks or follow-up tasks if any.
