# Prototype Guide

## Quick Start

1. Open **`Assets/_Project/Scenes/BootstrapScene.unity`**.
2. Press **Play** — 3D abilities demo loads immediately.

`AppEntryPoint` runs `AppBootstrap`, which registers core modules + `AbilitiesDemoModule` and activates gameplay.

## Scene Layout

| Entity | EntityId | Spawn (approx.) |
|---|---|---|
| Player | 1 | `(-4, 0, 0)` |
| Enemy A | 2 | `(2, 0, 2)` |
| Enemy B | 3 | `(4, 0, 0)` |
| Enemy C | 4 | `(2, 0, -2)` |

Camera: `(0, 8, -10)`. Entities use entity FSM + `EntityAnimationPresenter`.

## Controls

| Input | Action |
|---|---|
| **Click / tap ground** | Clear selection + walk to point |
| **Click / tap enemy** | Select one enemy (replaces previous selection) |
| **Shift + click enemy** | Toggle enemy in multi-selection |
| **Click same enemy** (sole selection, no Shift) | Deselect |
| **Click player** | Clear selection |
| **Keys 1–4** | Cast — `TargetType = Enemy` requires a selected enemy; `TargetType = Player` does not |

### Input while abilities are running

Movement and rotation blocking are **per-ability** settings on `LockInputComponentData` (`BlockMovement`, `BlockRotation`) — not a single global rule.

| Demo ability | Block movement | Block rotation |
|---|---|---|
| Dash, Healing, Defenced Attack | Yes | No — can turn toward another enemy while casting |
| Firewall | Yes | Yes — cannot turn during cast executor |

After the cast **executor** ends, locks stay until phase **`Complete`** (enemy DoT included). Details: **[Player-Input-During-Cast.md](Player-Input-During-Cast.md)**, **[Ability-Config-Reference.md](Ability-Config-Reference.md)**.

Still allowed during active casts (unless a flag blocks it):

- Selecting / deselecting enemies (click or Shift+click)
- Facing the selected enemy (when `BlockRotation` is off)
- Casting on another enemy that does **not** already have an ability running on it

## Ability Hotkeys

| Key | Ability Id | Name |
|---|---|---|
| **1** | `dash` | Dash |
| **2** | `firewall` | Firewall |
| **3** | `healing` | Healing |
| **4** | `defenced_attack` | Defenced Attack |

### Defenced Attack (key 4) — dual status

`defenced_attack` applies **two parallel status effects** on the selected enemy:

| Status | Effect | Floating text |
|---|---|---|
| **Freezing** | Blocks enemy movement via FSM `Input.Movement` | — |
| **Bleeding** | DoT ticks via FSM `Status.Bleeding` | Orange **Dot** (`-3` per tick at default config) |

Instant **damage** (15) lands from `DamageComponentData` on cast start (red **Damage** number). VFX: `VFX_DefencedAttack` on hit + optional status VFX from config (`VFX_Bleeding` for bleeding).

## Targeting

See **[Targeting-And-Casting.md](Targeting-And-Casting.md)** for the full specification.

| Ability (`Target Type` in config) | Selection required? | Resolved targets |
|---|---|---|
| **`Enemy`** — `dash`, `firewall`, `defenced_attack` | **Yes** | Selected enemy(ies); no selection → `InvalidTarget` |
| **`Player`** — shipped as `healing` key 3 | **No** | Caster only (`Target Type` from config) |

All abilities use **`Target Type`** on the asset — no hard-coded id rules. `healing` hits the player because `Healing.asset` is **Player**; set **Enemy** to cast on selected enemies.

**Selection is not sticky:** clearing selection (ground, player, re-click deselect) removes all targets. Abilities with `TargetType = Enemy` will not hit any enemy until the player selects again — there is no nearest-enemy or last-target fallback.

**Range:** casts with `TargetType = Enemy` fail if target is beyond ability `Range` from config (dash movement ignores range check).

## Demo Features (L1–L14)

| Feature | Notes |
|---|---|
| Health bars | Billboard HP above units |
| Floating combat text | Pooled damage/heal numbers |
| Contact damage | Player ↔ enemy proximity damage |
| Gradual combat | DoT via `IsGradual` / `GradualCombatApplier` |
| Death state | 0 HP → dead animation/state |
| Shift multi-select | See controls above |
| 4 abilities + 3 enemies | Keys 1–4, enemies tinted A/B/C |
| Click-to-move | Ground click navigates arena |
| Target ring | Ring under selected enemy(ies) |
| Face on cast | Caster rotates toward target |
| Fallback primitives | Capsule/plane if Addressables missing |

## Combat & floating numbers (expected behaviour)

These are **intentional demo mechanics**, not bugs.

### Two damage numbers on one cast (e.g. Firewall: **-25** then **-8**)

**Firewall** (and similar combos) applies **two separate combat effects** from its config:

| Source | Config field | Demo value | Floating text |
|--------|----------------|------------|---------------|
| Instant hit | `DamageComponentData` (`TotalValue`, `ApplicationDuration = 0`) | **25** at 0.5s delay | Red **Damage** (`-25`) |
| Burn DoT (**burning**) | `StatusEffectComponentData` (`EffectType = Combustion`, `Value`, `TickInterval`) | **8** per tick for 4s | Orange **Dot** (`-8` each second) |

So **-25 then -8** means: burst damage landed, then the first combustion tick — not one ability “splitting” a single hit incorrectly.

Other abilities may show only one number (instant only) or several orange ticks while the status runs. Gradual-only damage uses `GradualCombatApplier` when `ApplicationDuration > 0` on `DamageComponentData` (see [Ability-Config-Reference.md](Ability-Config-Reference.md#damagecomponentdata)).

### Contact damage — both player and enemy take damage

When the player **collides / stands within melee range** of an enemy, `EntityContactDamageService` applies proximity damage to **both** entities on the same tick:

| Constant | Value | Meaning |
|----------|-------|---------|
| `DemoConstants.Combat.ContactDistance` | 0.9 | Horizontal distance threshold |
| `DemoConstants.Combat.ContactDamageAmount` | **8** | HP removed from **each** unit |
| `DemoConstants.Combat.ContactCooldownSeconds` | 1.0 | Per-entity cooldown between contact hits |

This is **symmetric by design** (L3 demo feature): bumping an enemy hurts you and them — useful for testing HP bars, floating text, and death/despawn without casting.

If you see an extra **-8** (red Damage, not orange Dot) while standing on an enemy, it may be **contact damage**, not DoT. Step back to isolate ability-only numbers.

Implementation: `EntityContactDamageService` (registered in [LifeCycle-Tick-Contract.md](LifeCycle-Tick-Contract.md)).

## Module Dependencies

`AbilitiesDemo` requires: Abilities, Input, Addressables, **Pool**, Audio, Effects, LifeCycle, Logger, StateMachine.

If the demo does not appear, run **AvantajPrim → Project → Setup Addressables Bootstrap**.

## Editor Tools

- **AvantajPrim → Abilities → Authoring Window** — list/create abilities, validate, register Addressables, remap hotkeys in Play Mode
- Custom inspector on `AbilityConfigAsset`
- **AvantajPrim → Project → Setup Addressables Bootstrap**
- **AvantajPrim → Abilities → Replay Last Cast**

See also: **[Targeting-And-Casting.md](Targeting-And-Casting.md)** for selection and hotkey rules.
