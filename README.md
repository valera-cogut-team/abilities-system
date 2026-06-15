# Avantaj Prim — Abilities System (Demo)

Unity project with a data-driven, component-based abilities system: domain/execution separation, status effects, activation logging, unit tests, and a playable 3D demo.

**Unity version:** 6000.3.15f1 (tested). Other Unity 6 versions — lower or higher — should work as well.

## How to Run

1. Open the project in Unity **6000.3.15f1** or another compatible Unity 6 version.
2. On first open, run **AvantajPrim → Project → Setup Addressables Bootstrap** if demo assets are not registered yet (creates VFX/SFX/cast clips, Addressables groups, and wires `AssetReference` fields on ability configs).
3. Open `Assets/_Project/Scenes/BootstrapScene.unity`.
4. Press **Play** — the 3D abilities demo starts immediately (no splash or menu).

## Controls

| Input | Action |
|---|---|
| **Click ground** | Clear selection + move player (WalkForward animation) |
| **Click enemy** | Select one enemy (replaces previous selection) |
| **Shift + click enemy** | Add/remove enemy from multi-selection |
| **Click same enemy** (sole selection, no Shift) | Deselect |
| **Click player** | Clear selection |
| **Keys 1–4** | Cast ability — **`TargetType = Enemy`** requires enemy selection; **`TargetType = Player`** casts on caster (see below) |

Scene: **1 player + 3 enemies** on a nature-pack arena.

### Targeting and casting (summary)

Cast target is **only** defined by **`Target Type`** on each ability’s `AbilityConfigAsset` (`DemoAbilityTargetResolver`). **No ability-id exceptions** (not for `healing`, not for any other id).

| `Target Type` (config) | Shipped examples (keys) | Enemy selection required? | Resolved cast target(s) |
|---|---|---|---|
| **Enemy** | `dash`, `firewall`, `defenced_attack` (1, 2, 4) | **Yes** | All selected living enemies (Shift multi-select); parallel `CastOnTargetsAsync` |
| **Player** | `healing` (3) — `Healing.asset` today | **No** | Caster (`registry.PlayerId`); current enemy selection is **not** used |

**Examples:**
- **`healing` + `Target Type = Player`** (current shipped config): key **3** heals the player without selecting an enemy. Selecting enemies does **not** redirect the cast — because config says **Player**, not because `healing` is special.
- **`healing` + `Target Type = Enemy`** (change in Inspector): key **3** behaves like `firewall` — requires selected enemy(ies); heal applies to them (component `TargetType` must match for effect placement).

**No auto-targeting:** for **`Target Type = Enemy`**, if selection is empty, cast fails with `InvalidTarget`. No nearest enemy, no last target, no fallback to player.

Full rules: **[Targeting and Casting](Assets/_Project/Docs/Targeting-And-Casting.md)**.

Dash ignores range check (closes gap via movement). Other **`TargetType = Enemy`** abilities fail if target is beyond configured `Range`.

### Input during cast (movement vs rotation)

Each ability configures **independent** movement and rotation blocking via `LockInputComponentData` (`BlockMovement`, `BlockRotation`). Example: shipped **`healing`** (`BlockMovement` only) — cannot walk during cast but can turn toward another enemy; **cast target** still follows config **`Target Type`** (Player today). **`firewall`** blocks both movement and rotation.

This is **by design** — not a global “no movement while any ability runs” rule. Full reference: **[Ability Config Reference](Assets/_Project/Docs/Ability-Config-Reference.md)** and **[Player Input During Cast](Assets/_Project/Docs/Player-Input-During-Cast.md)**.

## Adding a New Component (inline example)

To add a **KnockbackComponent** without changing core ability execution:

1. **Data** — create `KnockbackComponentData : IAbilityComponentData` in `Abilities.Data` with `Force`, `PlayTimeType`, `DelaySeconds`.
2. **Executor** — create `KnockbackComponentExecutor` in `Abilities.Execution/Executors` that calls `presentation.PublishMovement(...)` or `entityState.TryTransition` for displacement.
3. **Register** — add `registry.Register(new KnockbackComponentExecutor())` in `AbilitiesInstaller`.
4. **Config** — add the component to a `AbilityConfigAsset` in the Inspector (ReorderableList).
5. **Test** — add an EditMode test asserting the executor publishes the expected intent.

See [How-To-Add-Ability-Component.md](Assets/_Project/Docs/How-To-Add-Ability-Component.md) for full details.

## Architecture (summary)

| Layer | Assembly | Role |
|---|---|---|
| Domain | `Abilities.Domain` | IDs, events, ports — no Unity refs |
| Data | `Abilities.Data` | ScriptableObject configs + `AssetReference*` component fields |
| Execution | `Abilities.Execution` | Catalog, executors, `AbilitiesService` — engine-free |
| Infrastructure | `Abilities.Infrastructure` | Presentation port (UniRx) |
| Demo | `AbilitiesDemo` | 3D scene, input, presentation bridge, world UI |

**Modules at runtime:** Logger, LifeCycle, Input, Addressables, StateMachine, **Pool**, Audio, Effects, Abilities, AbilitiesDemo.

**Input architecture:** gameplay hotkeys and click routing run through plain C# services (`AbilitiesInputService`, `PlayerInputRouter`) ticked by `LifeCycleModule` — not `MonoBehaviour.Update`. This is stricter than a literal “input on MonoBehaviour” assignment wording; see [Code Style Guide](Assets/_Project/Docs/Code-Style-Guide.md).

**Object pooling:** floating combat numbers use `IPoolFacade` (`PooledCombatLabel`, pool id `demo.combat_float`) with prewarm in `AbilitiesDemoPresentationOrchestrator.BuildWorldAsync`.

## Documentation

| Doc | Description |
|---|---|
| [Abilities Architecture](Assets/_Project/Docs/Abilities-Architecture.md) | Layers, modules, patterns |
| [Abilities System](Assets/_Project/Docs/Abilities-System.md) | Cast pipeline, components, pooling |
| [Ability Config Reference](Assets/_Project/Docs/Ability-Config-Reference.md) | **All SO fields**, lock flags, component tuning, “not a bug” notes |
| [How to Add an Ability](Assets/_Project/Docs/How-To-Add-Ability.md) | Designer workflow (SO + Addressables) |
| [How to Add a Component](Assets/_Project/Docs/How-To-Add-Ability-Component.md) | Developer workflow |
| [Prototype Guide](Assets/_Project/Docs/Prototype-Guide.md) | Demo controls and abilities |
| [Targeting and Casting](Assets/_Project/Docs/Targeting-And-Casting.md) | Selection rules, target resolution, hotkey behaviour |
| [Player Input During Cast](Assets/_Project/Docs/Player-Input-During-Cast.md) | Config-driven movement/rotation locks while abilities run |
| [Entity State Machine](Assets/_Project/Docs/Entity-StateMachine.md) | Per-entity FSM |
| [Third-Party Asset Mapping](Assets/_Project/Docs/ThirdParty-Asset-Mapping.md) | Addressable keys + `AssetReference` groups |
| [LifeCycle Tick Contract](Assets/_Project/Docs/LifeCycle-Tick-Contract.md) | Centralized Update/LateUpdate handlers |
| [Code Style Guide](Assets/_Project/Docs/Code-Style-Guide.md) | `.editorconfig` companion + Unity conventions |
| [Ability Authoring Window](Assets/_Project/Docs/How-To-Use-Ability-Authoring-Window.md) | Editor window for abilities + hotkeys |

## Tests

**Window → General → Test Runner**

- **EditMode** — **153** tests (Unity Test Runner; **all passing** — last verified green)
- **PlayMode** — **15** tests (Unity Test Runner; **all passing**)

Run via **Window → General → Test Runner**. Unity batchmode CI uses `.github/workflows/unity-tests.yml` (project must not be open in another Editor instance).

## Code style

- [`Assets/_Project/.editorconfig`](Assets/_Project/.editorconfig) — formatting, naming, analyzers (AAA production profile, `_Project` only)
- [`.editorconfig`](.editorconfig) — repo-wide baseline (charset, indent); no C# rules on third-party
- [Code Style Guide](Assets/_Project/Docs/Code-Style-Guide.md) — Unity exceptions, DI, modifiers migration

## Source Layout

```
Assets/_Project/
  Configs/Abilities/          Ability ScriptableObjects
  Docs/                       Documentation
  Scenes/BootstrapScene.unity Entry scene
  Source/
    Bootstrap/                AppBootstrap
    Modules/Core/             Logger, Pool, Abilities, …
    Modules/Game/AbilitiesDemo/   3D demo
  Tests/                      EditMode + PlayMode
```
