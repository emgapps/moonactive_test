Your Mission
1. Enemy AI Behavior System
The enemy zombies need intelligent behavior that changes based on game conditions. Currently, EnemyController.cs has helper methods but no logic to control when to use them.

The Challenge: Enemies must exhibit different behaviors:

Patrol - Move between waypoints when the player is not detected
Chase - Pursue the player when they come into sight
Attack - Engage the player when within range

Behavior transitions should feel natural and make enemies appear intelligent. The system should be generic and reusable.

Helper Methods Available:

public void MoveTo(Vector2 target)
public bool IsPlayerVisible()
public bool IsPlayerInAttackRange()
public void Attack()
public void PlayAnimation(string animName)
public Transform PlayerTarget { get; }
public List<Vector2> patrolPoints;

What to Design:

Architecture to manage different behaviors and transitions between them
At least 3 behavior implementations (patrol, chase, attack)
Integration into EnemyController.Awake(), Start(), Update()


2. Object Pooling System
Implement a generic object pool to eliminate GC spikes from CoinSpawner.Instantiate() calls.

Requirements: Generic design (any Component type), clean API (Get, Release, Clear). Integrate with CoinSpawner.Awake(), SpawnNewCoin(), ClearAllCoins().

What to Create: Generic object pool class, replace Instantiate() calls.


3. Level Loading System
Implement data loading from JSON (Assets/Resources/Levels/Levels.json).

What You're Given:

ILevelDataProvider.cs interface (uses object types - needs updating)
Empty methods in LevelLoader.cs: InitializeDataProvider(), ApplyPlayerConfiguration(), SpawnZombies()

What to Create:

A) Data Classes: Analyze JSON, create classes, replace object types.
B) Data Provider: Class implementing ILevelDataProvider (Resources), JSON parsing, error handling.
C) Integration: Implement 3 empty methods, update CoinSpawner.OnLevelLoaded(), update LevelManager.



4. Weapons System

Implement a complete weapons system that allows the player to shoot zombies with different weapon types.

The Challenge: Create a flexible weapons system with multiple weapon types, each with unique characteristics and configurable stats.

What You Need to Implement


Weapon Types

Pistol - Single-shot weapon with moderate damage and fire rate
Shotgun - Spread-shot weapon with high damage, lower fire rate, multiple projectiles
Machinegun - Rapid-fire weapon with lower damage per shot but high fire rate

Weapon Configuration

All weapon stats must be configurable via JSON file (Assets/Resources/Weapons/Weapons.json)

Each weapon must have the following primary stats:
  - Damage: Damage dealt per shot/bullet
  - Magazine Size: Number of bullets in a magazine
  - Fire Rate: Time between shots (in seconds)
  - Reload Time: Time required to reload magazine (in seconds)
  - Range: Maximum effective range of the weapon


Weapon Selection UI


Create a separate UI window/screen that appears before level start
Player must be able to select one of the three weapons before starting the level
UI should display weapon information (stats, name, visual representation)
Selection should be confirmed before proceeding to gameplay
Selected weapon should persist throughout the level

UI Indicators


Bullet Amount Display: Show current bullets remaining in magazine (e.g., "15/30" format showing current/max)
Display should update in real-time as player shoots
Reloading Indicator: Visual indicator that shows when weapon is reloading
Indicator should display reload progress (progress bar, timer, or animation)

Shooting Mechanics


Player shoots by pressing Space button on keyboard
Shooting should respect fire rate (prevent spamming)
Implement magazine system: when magazine is empty, player must reload
Reloading can be automatic when magazine is empty, or manual (press R key)
