# Targeting and Casting

This document describes how **enemy selection**, **target resolution**, and **ability casting** work in the 3D demo. It is the authoritative reference for demo input + hotkey behaviour.

Related docs:

- [Prototype-Guide.md](Prototype-Guide.md) — quick controls and ability list
- [Player-Input-During-Cast.md](Player-Input-During-Cast.md) — config-driven movement/rotation locks while casts run
- [Ability-Config-Reference.md](Ability-Config-Reference.md) — all ScriptableObject fields
- [Abilities-System.md](Abilities-System.md) — execution pipeline, components, cast results

## Overview

```
Player click / hotkey
        │
        ▼
TargetingService          ← stores selected enemy EntityIds
        │
        ▼
DemoAbilityTargetResolver ← maps ability TargetType + selection → target list
        │
        ▼
AbilityCastingService     ← range check, FSM guard, face target
        │
        ▼
IAbilitiesFacade          ← CastAsync / CastOnTargetsAsync
```

**Important rule:** abilities with **`TargetType = Enemy`** never auto-pick a target. If no enemy is selected, the cast fails with `InvalidTarget` and **nothing** is applied — even if an enemy was selected earlier and selection was cleared.

**`TargetType = Player`:** resolver returns the caster (`registry.PlayerId`). Enemy selection is ignored. No selection is required. Demo: `healing` uses this because `Healing.asset` has `Target Type = Player`. Change the asset to `Enemy` and the same ability behaves like any enemy-target ability.

## Selection (`TargetingService`)

Selection state is a list of `EntityId` values maintained by `TargetingService`.

| Input | Result |
|---|---|
| **Click enemy** (no Shift) | Replace selection with that one enemy |
| **Shift + click enemy** | Toggle that enemy in/out of multi-selection |
| **Click same enemy** (sole selection, no Shift) | Clear all selection |
| **Click player** | Clear all selection |
| **Click ground** | Clear all selection (+ move player if locomotion allowed) |

Selection changes raise `SelectedTargetsChanged`. The yellow ring (`TargetSelectionIndicator`) follows the current list.

### What selection does **not** do

- Selection is **not** remembered as an implicit target after it is cleared.
- Clearing selection (ground, player, re-click deselect) means **no enemy is targeted** until the player selects again.
- Non-enemy ids in the selection list are ignored by target resolution (only registered enemies count).

## Target resolution (`DemoAbilityTargetResolver`)

Target resolution runs on every hotkey cast via `IAbilityTargetResolver.ResolveTargets`.

Implementation: `DemoAbilityTargetResolver.cs`.

### Step 1 — collect selected enemies

```csharp
CollectSelectedEnemies()
```

Walks `TargetingService.SelectedTargets` and keeps only ids that exist in `DemoEntityRegistry.EnemyIds`.

### Step 2 — apply ability `TargetType`

| `AbilityTargetType` | Shipped example | Resolution rule |
|---|---|---|
| **Enemy** | `dash`, `firewall`, `defenced_attack` | Return selected enemies only. **Empty list if none selected.** |
| **Player** | `healing` (current `Healing.asset`) | Return caster (`registry.PlayerId`). Selection ignored. |

There is **no** per-ability-id logic — only `definition.TargetType` from the config.

There is **no** fallback to:

- nearest living enemy
- last selected enemy
- first enemy in the scene
- player (for `TargetType = Enemy` abilities)

### Examples

| Selection state | Hotkey | Ability | Resolved target(s) | Cast result |
|---|---|---|---|---|
| Enemy B selected | 2 | Firewall | `[Enemy B]` | Success (if in range) |
| Enemies B + C selected | 2 | Firewall | `[Enemy B, Enemy C]` | `CastOnTargetsAsync` (parallel) |
| Nothing selected | 2 | Firewall (`Enemy`) | `[]` | **Fail** `InvalidTarget` |
| Had enemy, then clicked ground | 1 | Dash (`Enemy`) | `[]` | **Fail** `InvalidTarget` |
| Nothing selected | 3 | Healing (`Player`) | `[Player]` | Success (config: Player target) |
| Enemy A selected | 3 | Healing (`Player`) | `[Player]` | Success (selection ignored — config: Player) |
| Enemy A selected | 3 | Healing **if config were `Enemy`** | `[Enemy A]` | Success on selected enemy |
| Nothing selected | 3 | Healing **if config were `Enemy`** | `[]` | **Fail** `InvalidTarget` |

## Cast entry point (`AbilityCastingService`)

Hotkeys 1–4 call `AbilitiesInputService` → `AbilityCastingService.CastAsync(abilityId)`.

Pipeline:

1. **Session active** — `DemoGameplaySession.IsActive` must be true.
2. **Player registered** — `DemoEntityRegistry.PlayerId`.
3. **FSM guard** — player not dead/hit-react; can enter `Action.Casting.*` (or already casting).
4. **Resolve targets** — `DemoAbilityTargetResolver` (see above).
5. **Empty targets** → `CastAbilityResult.Fail(InvalidTarget)` — facade is **not** called.
6. **Range check** — for abilities with `Range > 0` (except movement-based dash), every target must be within range on the XZ plane.
7. **Face first target** — `PlayerInputRouter.FaceTarget` (allowed even during active casts).
8. **Dispatch** — one target → `CastAsync`; multiple → `CastOnTargetsAsync`.

### `ResolveTarget` vs `ResolveTargets`

- `ResolveTargets` returns the full list (may be empty).
- `ResolveTarget` returns the first resolved id, or `default(EntityId)` when `ResolveTargets` is empty — except `TargetType = Player`, which still returns caster id via the same resolver path.

## Per-target busy rule (`AbilitiesService`)

Separate from selection:

- Each **enemy** can have at most one ability lifecycle running at a time (`AlreadyCasting`).
- The player **can** cast on enemy B while enemy A is still burning.
- The player **cannot** cast twice on the same busy enemy until the first cast fully completes (including DoT/status).

See [Player-Input-During-Cast.md](Player-Input-During-Cast.md) for locomotion blocking during active casts.

## Cast error codes (demo-relevant)

| Code | Typical demo cause |
|---|---|
| `InvalidTarget` | `TargetType = Enemy` with no enemy selected; or target out of range |
| `AlreadyCasting` | Same enemy still has an active ability lifecycle |
| `Blocked` | Gameplay inactive; FSM rejected cast transition |
| `InvalidCaster` | Player not registered |
| `UnknownAbility` | Ability id not in catalog |

## Source files

| File | Role |
|---|---|
| `TargetingService.cs` | Selection list, click is handled in `PlayerInputRouter` |
| `PlayerInputRouter.cs` | Raycast enemy/ground/player, update selection |
| `DemoAbilityTargetResolver.cs` | `IAbilityTargetResolver` — selection → targets |
| `AbilityCastingService.cs` | Hotkey cast orchestration |
| `AbilitiesInputService.cs` | Key 1–4 → `CastAsync` |
| `AbilitiesService.cs` | Per-target busy tracking, execution |
| `TargetSelectionIndicator.cs` | Visual ring under selected enemies |

## Manual QA checklist

1. Select enemy → cast Firewall → hits that enemy.
2. Clear selection (ground click) → press 2 → **no effect** on any enemy.
3. Select two enemies with Shift → press 2 → both take damage simultaneously.
4. Press 3 with no selection → player heals (green VFX on self) — `Healing.asset` has `TargetType = Player`.
5. Select enemy → press 3 → **player still heals** (selection ignored while config is `Player`; change config to `Enemy` to heal selected enemy instead).
6. Start Firewall on A → while active, select B → cast Dash on B → works.
7. Start Firewall on A → try Firewall on A again → blocked (`AlreadyCasting`).

Automated coverage: `AbilityCastingServiceTests`, `AbilitiesDemoExtendedGameplayTests` (selection clear).
