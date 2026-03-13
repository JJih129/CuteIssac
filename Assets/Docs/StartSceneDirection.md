# Start Scene Direction

Use the current `SampleScene` as the prototype bootstrap scene instead of creating multiple scenes immediately.

Recommended root objects:

- `__App`
- `Main Camera`
- `World`
- `UIRoot`

Recommended components on `__App`:

- `RunManager`
- `GameBootstrap`
- `InputSystemPlayerInputReader`
- `RoomNavigationController`

Suggested child placeholders under `World`:

- `RoomRoot`
- `PlayerSpawn`
- `EnemyRoot`
- `PickupRoot`
- `ProjectileRoot`
- `Player`

Suggested child placeholders under `UIRoot`:

- `GameplayCanvas`
- `DebugCanvas`

Bootstrap flow for this prototype:

1. `GameBootstrap` runs first on scene load.
2. `GameBootstrap` calls `RunManager.Bootstrap`.
3. `RunManager` creates a fresh `RunContext`.
4. Future systems subscribe to `RunManager` events to spawn rooms, player, enemies, and UI.
5. Player-facing systems read gameplay input through `IPlayerInputReader` instead of talking to `InputAction`s directly.

Minimal player movement setup:

1. Create a `Player` object under `World`.
2. Add `Rigidbody2D`, `CapsuleCollider2D` or `CircleCollider2D`, `PlayerMovement`, `PlayerController`, `PlayerStats`, `PlayerInventory`, `PlayerItemManager`, and `PlayerHealth`.
3. Set `Rigidbody2D` gravity scale to `0` and freeze rotation if it is not already configured that way.
4. Assign the `InputSystemPlayerInputReader` on `__App` to the `inputReaderSource` field on `PlayerController`.
5. `PlayerStats` now acts as the final stat source for move speed, projectile damage, and fire interval.
6. `PlayerHealth` already implements `IDamageable`, so enemy contact damage can hit the player through the shared combat contract.

Minimal HUD setup:

1. Create or reuse a scene `Canvas` object such as `GameplayCanvas`.
2. Place one `HUDRoot` object or `HUDRoot` prefab under that canvas.
3. Keep `HUDController` on `HUDRoot` and assign `PlayerHealth`, `PlayerInventory`, plus each panel view directly in the inspector.
4. `HUDController` only forwards data to replaceable views: `HealthPanelView`, `ResourcePanelView`, `ActiveItemPanelView`, `BossHpPanelView`, and `MinimapPanelView`.
5. `HealthPanelView` supports a heart slot template, so future art can replace the slot image or slot prefab skin without changing gameplay code.
6. `ResourcePanelView`, `ActiveItemPanelView`, `BossHpPanelView`, and `MinimapPanelView` expose image/text roots through `SerializeField`, so designers can replace icons, frames, bars, and placeholders directly on the HUD prefab.

Minimal combat setup:

1. Add `PlayerCombat` and `ProjectileSpawner` to the `Player` object.
2. Create a projectile prefab with `SpriteRenderer`, `Rigidbody2D`, `Collider2D`, and `ProjectileController`.
3. Set the projectile collider to trigger if you want trigger-based hits.
4. Create a `ProjectileDefinition` asset and assign the projectile prefab, damage, speed, and lifetime.
5. Create a `PlayerAttackDefinition` asset and assign the `ProjectileDefinition`, fire interval, and muzzle offset.
6. Assign the `InputSystemPlayerInputReader` on `__App` to the `inputReaderSource` field on `PlayerCombat`.
7. Assign the `PlayerAttackDefinition` to `PlayerCombat` and keep `ProjectileSpawner` on the same object or wire it manually.
8. `PlayerCombat` uses `PlayerStats` for final damage and fire interval, so passive items can change combat without editing attack data.

Minimal enemy setup:

1. Create an enemy prefab with `SpriteRenderer`, `Rigidbody2D`, `Collider2D`, `EnemyMovement`, `EnemyHealth`, and `EnemyController`.
2. Set `Rigidbody2D` gravity scale to `0` and freeze rotation.
3. Tune move speed on `EnemyMovement`, max health on `EnemyHealth`, and contact damage on `EnemyController`.
4. Set the player object's tag to `Player`, or assign the player transform directly to `EnemyController.targetOverride`.
5. The enemy can already take damage from player projectiles because `EnemyHealth` implements `IDamageable`.

Minimal room setup:

1. Create a room root object with a trigger `Collider2D` that covers the playable room area.
2. Add `RoomController` to the room root and assign any `RoomDoor` components that should lock during combat.
3. Optionally assign `roomContentRoots`, `defaultPlayerSpawnPoint`, and `cameraFocusPoint` on `RoomController`.
4. Add `RoomDoor` to each doorway blocker and wire the blocking colliders plus optional locked/unlocked visuals.
5. For manual scene tests, assign `connectedRoom`, `connectedDoor`, and an optional `arrivalPoint` on each `RoomDoor`.
6. Add `RoomEnemyMember` to each enemy placed in the room and assign the room controller.
7. Add `RoomNavigationController` on `__App`, assign the room list, starting room, player, and camera.
8. When the player enters the trigger, the room moves from `Idle` to `Combat`, locks doors, and waits for all registered enemies to die.
9. When `AliveEnemyCount` reaches `0`, the room changes to `Cleared`, unlocks doors, and exposes a `RoomCleared` event for future reward spawn logic.

Manual multi-room layout:

1. Place 3 to 5 room roots under `RoomRoot`, each with its own `RoomController`.
2. Put rooms at different world positions, or keep them overlapped and use `roomContentRoots` plus `cameraFocusPoint` to show only the current room.
3. Connect doors in pairs by wiring `connectedRoom` and `connectedDoor`.
4. Add a child transform near each destination doorway and assign it as `arrivalPoint` so the player lands just inside the next room.
5. `RoomNavigationController` now acts as the scene-level room graph runner and can later be replaced or fed by dungeon generation data.

Dungeon data split:

- `RoomData`: designer-authored room metadata such as type, weighting, and optional room-local layout overrides.
- `RoomLayoutData`: concrete room layout prefab plus supported door directions.
- `RoomLayoutSet`: shared collection of layout assets that floors can reuse.
- `FloorConfig`: designer-authored rules for a floor, including room counts and allowed room pools.
- `EnemyPoolData`: floor-facing enemy candidate asset with separate normal, elite, and boss pools.
- `EnemySpawnEntry`: one enemy candidate with prefab, weight, unlock floor, and budget cost metadata.
- `EnemyWaveData`: authored wave override asset for rooms that should use a fixed composition instead of procedural selection.
- `DungeonMap`: runtime graph produced by a future generator for the current floor.
- `DungeonRoomNode`: runtime room instance data with grid position and room connections.
- `GridPosition` and `RoomConnection`: lightweight runtime structs for grid layout and links between rooms.

Room graph generation:

- `RoomGraphBuilder` creates the first-pass grid graph from `FloorConfig`.
- The graph starts with a `Start` room at `(0, 0)`.
- Normal rooms expand only to open cardinal neighbors, so duplicate coordinates are avoided.
- `DungeonRoomDistanceCalculator` computes BFS distance from the start room for every node.
- `DungeonBossRoomAssigner` promotes the farthest eligible room to `Boss` using the floor's minimum boss distance rule.
- `DungeonSpecialRoomAssigner` assigns treasure and shop rooms by replacing eligible normal rooms, then adds secret rooms on empty coordinates with enough adjacent support.
- `RoomGraphBuilder` also resolves a compatible `RoomLayoutData` for each room node so later scene instantiation can use the selected prefab directly.
- `RoomGraphDebugRunner` can generate a graph in-scene and log the result before room instantiation exists.

Layout resolution:

- `RoomLayoutResolver` chooses a layout for each generated room after graph construction.
- Required door directions come from the room's actual graph connections.
- The resolver prefers room-local layout overrides first, then falls back to floor-wide shared layout sets.
- Within either source, the resolver prefers exact door-mask matches first, then layouts that support a superset of the required doors.
- Room-specific layouts can live on `RoomData`, and floor-wide shared layouts can come from `FloorConfig` via `RoomLayoutSet`.

Dungeon instantiation:

- `DungeonInstantiator` converts a generated `DungeonMap` into actual `RoomController` prefab instances in the scene.
- Spawn positions come from the generated room `GridPosition` and the instantiator's `roomWorldSpacing`.
- Each room prefab should expose one `RoomDoor` per supported direction, and each `RoomDoor` must have its `doorDirection` set correctly.
- Door links are wired from the generated graph, so `RoomDoor.connectedRoom` and `connectedDoor` are filled automatically after instantiation.
- The generated start room is passed into `RoomNavigationController`, which can also move the player to that room's spawn point.
- `DungeonInstantiationDebugRunner` can build a graph and instantiate it into the current scene for prototype testing.

Enemy pool setup:

- `FloorConfig` can reference one `EnemyPoolData` to define which enemies belong to that floor.
- `EnemyPoolData` keeps separate candidate lists for `Normal`, `Elite`, and `Boss` encounters.
- `EnemySpawnEntry` stores the enemy prefab, selection weight, unlock floor, and difficulty cost so later room or wave builders can scale encounters without hardcoding enemy names.
- `FloorConfig.GetEnemyBudget(...)` exposes a simple per-floor encounter budget hook for future normal room, elite room, or boss room composition.

Enemy wave assignment:

- `RoomData` can optionally reference an `EnemyWaveData` override for hand-authored rooms such as future boss rooms.
- `DungeonEnemyWaveAssigner` currently generates only normal room waves from the floor's normal enemy pool.
- `Start`, `Treasure`, `Shop`, and `Secret` rooms intentionally receive no default combat wave.
- Normal room wave budget scales with `distanceFromStart`, so rooms farther from the start can receive denser enemy compositions.
- Generated wave results live on `DungeonRoomNode.AssignedEnemyWave`, which a future room spawner can read to instantiate enemies when the room starts combat.

Passive item setup:

- `ItemData` stores authored passive item metadata plus a list of `StatModifier` entries.
- `PlayerInventory` owns the player's passive items and stays responsible only for storage.
- `PlayerItemManager` handles acquisition and tells `PlayerStats` to recalculate when inventory changes.
- `PlayerStats` reads base move speed from `PlayerMovement` and base damage/fire interval from `PlayerCombat` attack data, then applies modifiers to produce final runtime stats.
- Example test items:
  `Boots`: `MoveSpeed + 1.5`
  `Damage Up`: `Damage + 2`
  `Tears Up`: `FireInterval * 0.8` or `FireInterval + -0.05`

Player resource HUD notes:

- `PlayerInventory` now owns coins, keys, and bombs in addition to passive items.
- Resource count changes emit `ResourcesChanged`, which the HUD uses instead of polling.
- `PlayerHealth.HealthChanged` drives the health display, and the current debug HUD also renders simple pip text for readability before final art exists.

Why keep it this small:

- The project has no gameplay systems yet, so a single bootstrap scene is enough.
- `RunManager` becomes the run-level entry point without turning into a global mega-manager.
- Room, dungeon, player, and combat systems can be added later as separate modules that listen to run state.
