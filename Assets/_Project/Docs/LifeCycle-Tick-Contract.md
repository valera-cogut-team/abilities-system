# LifeCycle Tick Contract

All gameplay frame ticks are centralized through `LifeCycleModule` → UniRx → `ILifeCycleFacade`. **No** runtime `MonoBehaviour.Update` / `LateUpdate` / `FixedUpdate` in `Assets/_Project/Source`.

## Dispatch chain

```
LifeCycleModule.Enable()
  → Observable.EveryUpdate()     → LifeCycleFacade.TickUpdate(delta)
  → Observable.EveryLateUpdate() → LifeCycleFacade.TickLateUpdate(delta)
  → Observable.EveryFixedUpdate() → LifeCycleFacade.TickFixedUpdate(delta)  [no handlers registered]
```

## Registered handlers

| Handler | Module | Phase | Responsibility |
|---------|--------|-------|----------------|
| `AbilitiesInputService` | AbilitiesDemo | Update | Hotkeys 1–4, click routing |
| `AbilitiesDemoTickHandler` | AbilitiesDemo | Update + LateUpdate | FSM `TickAll`; indicator follow; UI billboard |
| `GradualCombatApplier` | AbilitiesDemo | Update | Gradual **`DamageComponentData`** ticks (`IsGradual`); not status-effect DoT |
| `PlayerMovementService` | AbilitiesDemo | Update | Click-to-move locomotion |
| `CastAnimationWaitService` | AbilitiesDemo | Update | Animation await polling |
| `EntityContactDamageService` | AbilitiesDemo | Update | Proximity damage |
| `AbilitiesUpdateHandler` | Abilities | Update | Core abilities module tick |
| `PooledEffectLifetimeService` | Effects | Update | Pooled VFX lifetime |

Status-effect DoT/heal (Combustion, Bleeding, Healing) ticks via **entity FSM** `EntityStateMachineController.TickStatus` — see [Entity-StateMachine.md](Entity-StateMachine.md).

## Audit result (2026-06-13)

Grep over `Assets/_Project/Source` and `Assets/_Project/Tests` for `void Update(`, `LateUpdate`, `FixedUpdate`:

- **Runtime:** 0 matches
- **Editor:** `AbilityConfigAssetEditor.serializedObject.Update()` only

## `IFixedUpdateHandler`

Infrastructure exists (`LifeCycleService.RegisterFixedUpdateHandler`) but **no implementors** in the project. Reserved for future physics-driven ticks. Listed in [Dead-Code-Candidates.md](Dead-Code-Candidates.md).

## Adding a new tick

1. Implement `IUpdateHandler` (and/or `ILateUpdateHandler`).
2. Register in module `Enable()` via `ILifeCycleFacade.RegisterUpdateHandler`.
3. Unregister in `Disable()`.
4. Do **not** add `MonoBehaviour.Update` on demo objects.
