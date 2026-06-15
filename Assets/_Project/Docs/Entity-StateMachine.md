# Entity State Machine

Entity gameplay (player and enemies) is driven by **StateMachineModule** — not Unity Animator. Animator is presentation-only via `EntityAnimationPresenter`.

## Regions

| Region | Kind | States |
|--------|------|--------|
| `Vitality` | Exclusive | `Alive`, `Dead` |
| `Locomotion` | Exclusive | `Idle`, `Walking`, `Displaced` (player only transitions) |
| `Action` | Exclusive | `None`, `HitReact`, `Action.Casting.*` (dynamic substates) |
| `Input` | Parallel | `Movement`, `Rotation` (independent locks) |
| `Status` | Parallel | `Combustion` (**burning**), `Freezing`, `Healing`, `Bleeding` (stack simultaneously) |
| `AI` | Exclusive | `Idle` (enemy stub) |

Paths use dotted notation, e.g. `Action.Casting.Dash`, `Status.Combustion`.

## Dynamic cast states

Any `Action.Casting.{name}` is allowed from `Action.None` via wildcard rule `Action.Casting.*`. Trigger name comes from **`AnimationComponentData.CastClip`** (`ResolveAnimationName()`) in the ability ScriptableObject — no factory code changes when adding abilities.

`EntityStateMachineController.TryTransition` triggers the matching Animator parameter after a successful cast transition.

## Key Types

| Type | Location |
|------|----------|
| `EntityStatePaths` | `AbilitiesDemo/Domain` |
| `EntityStateMachineFactory` | Builds definition per player/enemy profile |
| `EntityStateMachineController` | Plain C# runtime per entity (`IEntityStateMachineController`) |
| `EntityStateRegistry` | `EntityId → Controller` |
| `EntityStatePort` | `IEntityStatePort` implementation for executors |
| `EntityAnimationPresenter` | Maps FSM enter/exit to Animator |
| `AbilityCastAnimationResolver` | Reads cast animation via `AnimationComponentData.ResolveAnimationName()` from `AbilityCatalog` |

## Guards

Global guards on entity FSM:

- **NotDead** — blocks locomotion/action when `Vitality.Dead` is active
- **NotMovementLocked** — blocks transition to `Locomotion.Walking` when parallel `Input.Movement` is active; allows `Locomotion.Displaced` (dash)

**Rotation lock** does not block entering `Locomotion.Walking` — when only `Input.Rotation` is active, the player can walk but not turn (enforced in `PlayerInputRouter` / `PlayerMovementService`). See [Player-Input-During-Cast.md](Player-Input-During-Cast.md).

Casting (`Action.Casting.*`) does **not** by itself block walking; use `LockInputComponentData` for input blocking.

## Abilities Integration

Executors call `IEntityStatePort` instead of legacy control masks:

| Executor | FSM transition |
|----------|----------------|
| `AnimationComponentExecutor` | `Action.Casting.{name}` |
| `MovementComponentExecutor` | `Locomotion.Displaced` |
| `LockInputComponentExecutor` | parallel `Input.Movement` / `Input.Rotation` |
| `AimComponentExecutor` | publishes `PresentationAimIntent` (presentation rotates caster) |
| `DamageComponentExecutor` | target `Action.HitReact` |
| `StatusEffectComponentExecutor` | parallel `Status.*` with duration payload |

`CastPhasePresentationHandler` (wired by `AbilityPresentationBridge`) listens to `PhaseChanged` → on `"End"` resets `Action.None` + `Locomotion.Idle` when no executor-phase casts remain; on `"Complete"` calls `ReleaseCastInputLockLayer()` (after DoT/status/displacement finish).

Player click-to-move and facing are gated by FSM **`Input.Movement` / `Input.Rotation`** (from ability config), not by `HasActiveCasts`. See [Player-Input-During-Cast.md](Player-Input-During-Cast.md).

Status DoT/heal ticks run in FSM `OnUpdate` on parallel status states. **Freezing** additionally applies parallel `Input.Movement` on enter and removes it on exit.

## Adding a New Cast Animation

1. Assign `AnimationComponentData.CastClip` in the ability ScriptableObject and register the clip in Addressables (group **Animations**, address `anim_{TriggerName}`).
2. Add matching trigger/state in the entity animator (e.g. `DemoWizardAnimatorBuilder` for demo assets).

No edits to `EntityStateMachineFactory` cast lists are required.

## Tick

`AbilitiesDemoTickHandler.OnUpdate` calls `EntityStateRegistry.TickAll(deltaTime)` for status durations and DoT.

## Scope notes

- **Enemy AI** — `AI.Idle` stub only; out of demo scope.
- **Per-target cast occupancy** — `AbilitiesService` blocks overlapping abilities on the same target (`AlreadyCasting`); parallel casts on different targets are allowed. Player movement/rotation during casts follow **`LockInputComponentData`** per ability — see [Player-Input-During-Cast.md](Player-Input-During-Cast.md).
