# Isaac-Style Meta Progression Scaffold

## Added

- `CharacterProfileData` and `CharacterProfileCatalog` replace starting builds as the primary new-run path.
- The same player prefab/resources are reused. A character only changes starting resources, weapon relic, active item, passive items, and optional stat/projectile modifiers.
- `MetaProgressionSaveData` stores total runs, wins, defeats, abandons, best floor, room totals, resource totals, discovered item ids, enemy kill counts, achievement ids, and per-character records.
- `MetaProgressionManager` listens to run start/end, item acquisition, and enemy kills, then updates account progress at run end.
- `AchievementData` supports run count, win count, floor reach, enemy kill count, item discovery, character win, and character clear mark conditions.
- `TitleMenuController` now exposes new run, continue, character select, collection, achievements, stats, options, credits, quit, and two-step data reset.
- `RunResultPanelView` now displays character, run time, account runs/wins/defeats, best floor, and new unlock text, with restart and main menu actions.
- `SpecialRoomRuleData` and `FloorSpecialRoomRules` define Isaac-style special room rules for treasure/shop/boss/secret/curse/challenge/deal-room style extensions.
- `DungeonSpecialRoomAssigner` reads `FloorConfig.SpecialRoomRules` and can place additional rule-driven special rooms.

## How To Add A Character

1. Create a `CharacterProfileData` asset under `Assets/Resources/Characters`.
2. Set a stable `characterId`.
3. Assign optional `startingWeaponItem`, `startingActiveItem`, and passive items.
4. Add the asset to `DefaultCharacterProfileCatalog`.
5. Keep visuals on the shared player prefab unless the character truly needs unique art.

## How To Add An Achievement

1. Create an `AchievementData` asset under `Assets/Resources/Achievements`.
2. Set `achievementId`, display text, condition type, and required count/id.
3. If it unlocks content, set `rewardTargetType` to `UnlockKey` and fill `rewardUnlockKey`.
4. Add the matching unlock/content gate where the target content is filtered.

## Special Room Roadmap

- For devil/angel-like rooms, evaluate `postBossDealChance` after boss clear.
- For curse rooms, use `Health` or `Curse` entry cost.
- For shops and challenge rooms, gate entry with `Coin`, `Key`, health, or boss-clear conditions.
- Route rewards through the assigned `RoomRewardTable` or `ItemPoolData`.

## Remaining

- Achievement toast UI is event-ready through `MetaProgressionManager.AchievementsUnlocked`, but no dedicated toast prefab is authored yet.
- Character-specific visual swaps are intentionally not added; the current design reuses player resources.
- Devil/angel-like post-boss deal rooms still need a dedicated room type or a mapped generated room rule.
- Collection UI currently lists ids from save data; a polished icon grid should be added after final item art is stable.
