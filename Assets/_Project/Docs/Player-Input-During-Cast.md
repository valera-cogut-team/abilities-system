# Player Input During Ability Execution

This document describes **intentional** demo input rules while abilities run. Behaviour is **data-driven** from `LockInputComponentData` on each ability config — not hard-coded globally.

**Full field reference:** [Ability-Config-Reference.md](Ability-Config-Reference.md#lockinputcomponentdata).

## Summary

Player **ground movement** (click-to-move) and **player rotation** (face selected enemy, face while walking) are controlled **independently** by two Inspector flags on the ability’s `LockInputComponentData`:

- **`BlockMovement`** → parallel FSM `Input.Movement`
- **`BlockRotation`** → parallel FSM `Input.Rotation`

An ability may block nothing, movement only, rotation only, or both — depending on how those checkboxes are set.

**Enemy selection** and **hotkey casting** on free targets remain available regardless of these flags (subject to range and `AlreadyCasting` rules).

## What is blocked vs allowed

### Player movement (click ground / ongoing walk)

Blocked when the player entity has active FSM state **`Input.Movement`** (`EntityStateMachineController.IsMovementBlocked`).

Checked in:

- `PlayerInputRouter.CanMovePlayer()`
- `PlayerMovementService` (start walk, update loop, stop when blocked mid-walk)

### Player rotation

Blocked when the player entity has active FSM state **`Input.Rotation`** (`IsRotationBlocked`).

Affects:

- Facing an enemy after click selection (`PlayerInputRouter.FacePlayerToward`)
- Rotation while walking toward a ground point (`PlayerMovementService` slerp)

Does **not** affect:

- **`AbilityCastingService.FaceTarget`** before cast execution (runs before locks)
- **`AimComponentData`** during cast (presentation bridge rotates caster — ability aim)

### Always allowed (demo)

| Action | Notes |
|--------|-------|
| Click / Shift+click enemies | Updates selection; facing may be blocked if `BlockRotation` |
| Hotkeys 1–4 | Offensive abilities need selection; per-target `AlreadyCasting` still applies |
| Cast on another free enemy | Parallel casts on different targets OK |

## Lock lifecycle

| Event | What happens |
|-------|----------------|
| **`LockInputComponentData` OnStart runs** | `AcquireCastInputLock(BlockMovement, BlockRotation)` — refcount per layer |
| **Executor finishes** | `PhaseChanged` → `"End"` → cast animation reset only |
| **All pending effects finish** | `PhaseChanged` → `"Complete"` → `ReleaseCastInputLockLayer()` — one layer per cast |
| **After `"Complete"`** | Movement/rotation follow config only for layers still held by other active casts |

Locks last for the **full cast lifecycle** (including target DoT/status after the cast animation), until `IAbilityCastLifecycle` has no pending effects for that cast.

**Input blocking** is driven by FSM parallel **`Input.Movement` / `Input.Rotation`** (from `LockInputComponentData`), not by `HasActiveCasts`. **`AlreadyCasting`** is per-target occupancy in `AbilitiesService._busyTargets` — separate from caster session tracking.

## Input matrix (by config)

Use this when verifying an ability asset — replace ☑/☐ with that ability’s `LockInputComponentData` flags.

| Action | BlockMovement ☐ | BlockMovement ☑ | BlockRotation ☐ | BlockRotation ☑ |
|--------|-----------------|-----------------|-----------------|-----------------|
| Click ground → walk | Allowed | **Blocked** | (walk allowed if movement not blocked) | Walk OK but **no turn while moving** |
| Click enemy → face | Allowed if rotation not blocked | Blocked if movement blocked (no walk) | **Allowed** | **Blocked** |
| Walk rotation toward point | Allowed | N/A (no walk) | Allowed | **Blocked** (slide without turning) |
| Hotkey cast + aim | Allowed | Allowed | Allowed | Allowed (aim is ability-driven) |

### Demo ability examples

| Ability | Block Movement | Block Rotation | Typical feel during cast |
|---------|:--------------:|:--------------:|--------------------------|
| Dash | ☑ | ☐ | Rooted, can turn |
| Firewall | ☑ | ☑ | Rooted, cannot turn |
| Healing | ☑ | ☐ | Rooted, can turn |
| Defenced Attack | ☑ | ☐ | Rooted, can turn |

## Concurrent casts

Each cast adds one refcount layer per enabled flag. Example:

1. Cast A (`BlockMovement` only) → movement depth 1  
2. Cast B (`BlockMovement` only) on another target → movement depth 2  
3. A **`Complete`** → depth 1 → **still cannot move** (if B still pending)  
4. B **`Complete`** → depth 0 → movement unlocked  

Rotation layers are independent.

## Implementation

| Type | Role |
|------|------|
| `LockInputComponentData` | Config source (`Assets/_Project/Configs/Abilities/`) |
| `LockInputComponentExecutor` | Acquire/release via `IEntityStatePort` |
| `EntityStateMachineController` | `Input.Movement` / `Input.Rotation` parallel states + refcount |
| `AbilityInputLockResolver` | Reads OnStart lock flags from `AbilityCatalog` |
| `PlayerInputRouter` | Ground click, enemy select, facing |
| `PlayerMovementService` | Click-to-move execution |
| `AbilityCastingService` | `CancelCurrent()` only when `BlocksMovement(definition)` |
| `CastPhasePresentationHandler` | `"End"` → animation reset; `"Complete"` → `ReleaseCastInputLockLayer()` |

### Per-target cast queue (separate rule)

`AbilitiesService` maintains busy targets:

- Overlapping abilities on the **same** enemy → `AlreadyCasting`
- Parallel casts on **different** enemies → allowed
- Input locks and cast occupancy are **orthogonal**

## QA checklist

1. **Firewall** (both flags): cast → click ground → no move; click other enemy → selection OK, **no face** until cast executor ends.  
2. **Healing** (`TargetType = Player`, movement lock only): cast → cannot walk; click another enemy → **player turns** toward them (cast target remains caster per config).  
3. **Hypothetical rotation-only config** (`BlockMovement=☐`, `BlockRotation=☑`): cast → can walk; click enemy → selection OK, **no face**; walk without turning toward destination.  
4. After cast animation ends while enemy still has combustion → player **still cannot** move/turn if that ability had `BlockMovement` / `BlockRotation` until phase **`Complete`**.  
5. Two parallel casts with `BlockMovement` → movement blocked until **both** casts reach **`Complete`**.  
6. Hotkey cast still rotates caster toward target via aim even when `BlockRotation=☑`.

See also: [Targeting-And-Casting.md](Targeting-And-Casting.md), [Entity-StateMachine.md](Entity-StateMachine.md), [Abilities-System.md](Abilities-System.md), [Ability-Config-Reference.md](Ability-Config-Reference.md).
