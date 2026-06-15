# Abilities System

How abilities are defined, loaded, cast, and presented in this project.

## Ability Identity

- **`AbilityId`** — string key (e.g. `dash`, `firewall`, `healing`, `defenced_attack`).
- **`AbilityConfigAsset`** — ScriptableObject under `Assets/_Project/Configs/Abilities/`.
- **`AbilityDefinition`** — runtime model built by `AbilityConfigMapper.ToDefinition`.

Configs are registered into `AbilityCatalog` at startup via `AbilitiesDemoBootstrapService` and `AbilityAddressableDiscovery` (Addressables label `ability`).

### Adding a new ability (no core code changes)

1. Duplicate an existing `AbilityConfigAsset` or create a new one in the Inspector.
2. Set `_abilityId`, hotkey/target/range metadata, and compose components (`Animation`, `Sound`, `VFX`, etc.).
3. Assign Addressables entry with label **`ability`**.
4. Ensure cast animation clips are registered in Addressables (group **Animations**, address `anim_{TriggerName}`) and the entity animator has matching triggers/states (demo wizard builder adds common triggers automatically).

Cast animation trigger names are resolved from **`AnimationComponentData.CastClip`** via `ResolveAnimationName()` (address prefix `anim_` → animator trigger). The entity FSM accepts any `Action.Casting.{name}` transition via wildcard rules — no FSM factory edits required.

## Component Model

Each ability contains an ordered list of components implementing `IAbilityComponentData`:

| Field | Meaning |
|---|---|
| `PlayTimeType` | `OnStart`, `OnEnd`, or `Delay` |
| `DelaySeconds` | Wait before running (for `Delay` only) |

Built-in component types (`Abilities.Data`):

| Component | Core effect | Demo presentation |
|---|---|---|
| `LockInputComponentData` | Parallel FSM `Input.Movement` / `Input.Rotation` (refcounted, independent flags) | Blocks **player** click-to-move and/or facing per config — see [Ability-Config-Reference.md](Ability-Config-Reference.md#lockinputcomponentdata) |
| `AnimationComponentData` | FSM `Action.Casting.{name}` | Animator trigger via `EntityAnimationPresenter`; clip assigned via `AssetReferenceT<AnimationClip>`; optional `WaitUntilEnd` blocks timeline until animation completes |
| `SoundComponentData` | Publishes sound intent | 3D one-shot audio |
| `VisualFxComponentData` | Publishes VFX intent | Spawns Addressable prefab |
| `MovementComponentData` | FSM `Locomotion.Displaced` + payload | DOTween displacement |
| `DamageComponentData` | Target FSM `Action.HitReact` + damage event | Combat HP + floating numbers |
| `AimComponentData` | Publishes aim intent (caster faces target) | Bridge rotates caster toward target |
| `StatusEffectComponentData` | Parallel FSM `Status.*` with duration | DoT/heal ticks via FSM `OnUpdate` |

### Adding a new component type

1. Add `[Serializable]` data class in `Abilities.Data`.
2. Add executor in `Abilities.Execution/Executors/`.
3. Register once in `AbilitiesInstaller`.

Core pipeline (`AbilitiesService`, `AbilityExecutor`, catalog) stays unchanged.

## Execution Timeline

When `IAbilitiesFacade.CastAsync` is called:

1. **OnStart** components run immediately (in list order). If `AnimationComponentData.WaitUntilEnd` is set, execution waits for `IAbilityAnimationAwaiter` before the next component.
2. **Delay** components run after `DelaySeconds` (sequential).
3. **OnEnd** components run after a fixed 0.5s pause (if any exist).

Executors resolve through `AbilityComponentRegistry`. Missing executors are skipped silently.

After the executor finishes, **`PhaseChanged`** emits `"End"` (animation reset). Input locks release on **`"Complete"`** after all pending effects finish (DoT, status, displacement). See [Player-Input-During-Cast.md](Player-Input-During-Cast.md) and [Ability-Config-Reference.md](Ability-Config-Reference.md).

## Entities

Casting requires a registered caster (`IAbilityEntity`):

```csharp
abilities.RegisterEntity(new AbilityEntityModel(new EntityId(1), isPlayer: true));
```

`EntityId` and `IsPlayer` are used by targeting and presentation. Gameplay state (movement lock, casting, status, death) lives in the **entity FSM** (`EntityStateMachineController`), not on `IAbilityEntity`.

### Movement and rotation control

`LockInputComponentData` fields (Inspector-configurable) — **independent** toggles:

| Field | FSM state | Effect on player input |
|---|---|---|
| `BlockMovement` | `Input.Movement` (parallel) | Blocks click-to-move and ongoing walk for the full cast lifecycle |
| `BlockRotation` | `Input.Rotation` (parallel) | Blocks facing selected enemy and rotation while walking for the full cast lifecycle |

Both can be combined or disabled independently. Locks are acquired on OnStart and cleared on phase **Complete** (after all pending effects; refcounted for parallel casts).

**Not controlled by these flags:** ability aim (`AimComponent`, pre-cast `FaceTarget`), enemy selection, hotkey cast on free targets. Full matrix and “looks like a bug” notes: [Ability-Config-Reference.md](Ability-Config-Reference.md).

**All component fields:** [Ability-Config-Reference.md](Ability-Config-Reference.md).

## Status Effects

`StatusEffectComponentExecutor` enters parallel FSM states (`Status.Combustion`, `Status.Freezing`, `Status.Healing`, `Status.Bleeding`) with duration payload.

### Terminology: Combustion = burning

The test assignment lists status effects **«burning, freezing»**. In this project:

| Test assignment | Code / config | FSM path |
|-----------------|---------------|----------|
| **burning** | `StatusEffectType.Combustion` | `Status.Combustion` |
| **freezing** | `StatusEffectType.Freezing` | `Status.Freezing` |

The enum name **Combustion** is intentional (matches FSM + firewall config). Reviewers and docs may say **burning** — same feature. Do not add a duplicate `Burning` enum value; use `Combustion` in assets and Inspector.

- Duration counts down via FSM `Tick`.
- **Combustion (burning)** / **Bleeding** ticks publish `DamageRequestedEvent`.
- **Healing** ticks call `EntityCombatState.ApplyHeal`.
- **Freezing** enters parallel `Input.Movement` on the affected entity (blocks walking until status ends).

Component-level **`AbilityTargetType`**: only **Player** (caster) and **Enemy** (cast target). Resolved uniformly via `AbilityTargetIdResolver` in executors.

**Firewall example:** config includes both `DamageComponentData` (instant 25) and `StatusEffectComponentData` (Combustion, 8/tick). Floating text shows **-25** (red) then **-8** (orange) per tick — two components, not a single broken hit. Details: [Prototype-Guide.md](Prototype-Guide.md#combat--floating-numbers-expected-behaviour).

**Defenced Attack (key 4):** applies **Freezing** + **Bleeding** in parallel on the target (`DefencedAttack.asset`). Expect orange **Dot** ticks from both status components while they run.

No separate `StatusEffectStack` — FSM is the source of truth.

## Cast lifecycle troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Player cannot move or cast after killing an enemy mid-cast | Orphan `RegisterPendingEffect` when `TryTransition` fails (e.g. status on dead target) | Fixed: executors register pending **only after** successful FSM transition; `GradualCombatApplier.CancelPendingForTarget` completes lifecycle on despawn |
| Movement still blocked after cast animation ends | Input locks release on phase **`Complete`**, not **`End`** | Wait for DoT/status/displacement to finish; check `IAbilityCastLifecycle` pending count |
| Log shows `AlreadyCasting` | Target still in `_busyTargets` from hung cast | Same as row 1 — ensure cast lifecycle completes |

Phases: **`Start`** → executor runs → **`End`** (animation reset) → wait for pending effects → **`Complete`** (input lock release via `CastPhasePresentationHandler`).

### Healing design choice (demo)

The **Healing** ability uses `StatusEffectComponentData` with effect type **Healing**, not negative `DamageComponentData`. Rationale:

| Approach | Pros | Cons |
|----------|------|------|
| **StatusEffect (chosen)** | Reuses FSM tick + duration; consistent with combustion/freezing; heal-over-time without custom executor | Requires parallel `Status.Healing` state |
| Negative damage | Single damage pipeline | Mixes damage/heal semantics; harder to block heal independently |

Implementation: `StatusEffectComponentExecutor` enters `Status.Healing`; FSM `OnUpdate` calls `EntityCombatState.ApplyHeal` per tick. Shipped config: `Healing.asset` has ability **`Target Type = Player`** (cast resolves to caster) and component `TargetType = Player`. Change ability `Target Type` to `Enemy` to cast on selected enemies like any other enemy-target ability.

## Cast Results

`CastAbilityResult` indicates success or failure:

| `CastAbilityErrorCode` | When |
|---|---|
| `UnknownAbility` | ID not in catalog |
| `InvalidCaster` | Caster not registered |
| `InvalidTarget` | `TargetType = Enemy` with no enemy selected, or out of range |
| `AlreadyCasting` | That **target** already has an ability in progress |
| `Blocked` | Gameplay inactive or FSM guard rejected cast |
| `None` | Success |

Demo target resolution (`DemoAbilityTargetResolver`) is documented in [Targeting-And-Casting.md](Targeting-And-Casting.md).

## Activation Log

`AbilityActivationLog` records component type names per cast with timestamps. Used for replay (`AbilityActivationReplayService`) and editor debugging (`AbilityEditorPlayAccess`).

## Layer boundaries (AssetReference)

| Assembly | `noEngineReferences` | Unity / Addressables |
|---|---|---|
| `Abilities.Domain` | yes | none |
| `Abilities.Execution` | yes | none — executors use string keys from `Resolve*Key()` |
| `Abilities.Data` | no | `AssetReference*`, `ScriptableObject`, `AddressableAssetRefUtility` (authoring boundary) |
| Demo adapters | no | `AbilityPresentationBridge`, `IAddressablesFacade`, `IEffectsFacade` load by string address |

Executors publish `PresentationSoundIntent(clipKey)` and `PresentationVfxIntent(prefabKey)` as **strings** (`AbilityEvents.cs`). Presentation adapters resolve Addressables at runtime.

## Addressable assets

Built-in demo configs reference Addressable assets via `AssetReferenceT<AudioClip>` / `AssetReferenceGameObject` / `AssetReferenceT<AnimationClip>` on component data (`SoundComponentData`, `VisualFxComponentData`, `StatusEffectComponentData`, `AnimationComponentData`). Runtime loading prefers the Addressables label **`ability`** via `AbilityAddressableDiscovery` so new configs appear without editing hard-coded keys.

## Demo Presentation Pipeline

`AbilityPresentationBridge` (AbilitiesDemo) subscribes to facade observables:

- **`PhaseChanged`** — `"End"` resets action/locomotion; `"Complete"` releases input locks
- **`AimIntents`** — rotates caster toward target entity
- **`SoundIntents`** — 3D one-shot audio at caster
- **`VfxIntents`** — VFX via `IEffectsFacade`
- **`DamageEvents`** — applies combat HP (hit react animation driven by FSM in executor)

Entity animation/movement is driven by **entity FSM** + `EntityAnimationPresenter` (see [Entity-StateMachine.md](Entity-StateMachine.md)).

Keyboard casting uses **`AbilityCastingService`**, which resolves targets via **`IAbilityTargetResolver`** (`DemoAbilityTargetResolver`) and validates FSM cast transitions using **`AnimationComponentData.ResolveAnimationName()`** from the catalog. See [Targeting-And-Casting.md](Targeting-And-Casting.md).

## Object Pooling (Demo)

`CombatFeedbackPresenter` creates an `IPoolFacade` pool (`demo.combat_float`) for `PooledCombatLabel` instances. Pool is created with `initialSize: 0`; labels are prewarmed via `PrewarmPool(12)` in `AbilitiesDemoPresentationOrchestrator.BuildWorldAsync` and returned after DOTween completes.

## Public API

Use **`IAbilitiesFacade`** from game/UI code:

- `RegisterEntity` / `UnregisterEntity`
- `CastAsync(abilityId, casterId, targetId)` / `CastOnTargetsAsync(...)`
- `HasActiveCasts(casterId)` — caster session count (not the same as per-target `AlreadyCasting`)
- Observables: `PhaseChanged`, `AnimationIntents`, `SoundIntents`, `VfxIntents`, `MovementIntents`, `AimIntents`, `DamageEvents`

Executors and entity FSM wiring use **`IEntityStatePort`** (demo implementation: `EntityStatePort` → `EntityStateRegistry`).

Do not reference executors or `AbilitiesService` from presentation layers.
