# Ability Config Reference

Complete reference for **`AbilityConfigAsset`** fields and every built-in **component type**. Use this document when tuning abilities in the Inspector or reviewing Play Mode behaviour — many demo rules are **data-driven by design**, not bugs.

**Related docs:** [Abilities System](Abilities-System.md) (pipeline), [Player Input During Cast](Player-Input-During-Cast.md) (movement/rotation locks in detail), [How to Add an Ability](How-To-Add-Ability.md) (designer workflow).

---

## Ability asset metadata

Set on the root of each ScriptableObject under `Assets/_Project/Configs/Abilities/`.

| Field | Type | Purpose |
|-------|------|---------|
| **Ability Id** | `string` | Unique runtime key (e.g. `firewall`). Used by catalog, hotkeys, logs, Addressables. |
| **Display Name** | `string` | Designer-facing label (editor, docs). |
| **Hotkey Slot** | `int` 1–9 | Fallback keyboard slot → `Alpha1`…`Alpha9` when **Hotkey Key** is `None`. |
| **Hotkey Key** | `KeyCode` | Optional explicit key; overrides slot when set. Remappable in Play Mode via **Ability Authoring Window**. |
| **Target Type** | Ability-level cast resolution: `Player` → caster (no enemy selection); `Enemy` → selected enemies (fail if none). Component-level `TargetType`: `Player` → effect on caster; `Enemy` → effect on cast target. **No per-ability-id exceptions.** |
| **Range Type** | `Melee` / `Ranged` | Design metadata on the asset. |
| **Range** | `float` | Max distance for casts with **`Target Type = Enemy`**. **Exception:** abilities with `MovementComponentData` skip range validation (dash closes gap). |

Runtime model: `AbilityDefinition` (built by `AbilityConfigMapper` at load time).

---

## Shared component timing

Every component implements `IAbilityComponentData` and shares:

| Field | Values | Meaning |
|-------|--------|---------|
| **Play Time Type** | `OnStart`, `Delay`, `OnEnd` | When the executor runs in the cast timeline. |
| **Delay Seconds** | `float` | Extra wait before this component runs (only meaningful for `Delay`; also used as ordering delay within the Delay batch). |

### Execution order (per cast)

1. **OnStart** — all OnStart components run **in list order**. If `AnimationComponentData.WaitUntilEnd` is true, the pipeline waits for the cast animation before the next OnStart component.
2. **Delay** — delayed components run sequentially (caster batch + target batch interleaved by index).
3. **OnEnd** — after a fixed **0.5 s** pause, OnEnd components run.

Component **list order in the Inspector matters** for OnStart (e.g. `LockInput` before `Aim` before `Animation`).

---

## Component types (index)

| Component | Executor | Primary effect |
|-----------|----------|----------------|
| `LockInputComponentData` | `LockInputComponentExecutor` | Player input locks (movement / rotation) — see [§ Lock input](#lockinputcomponentdata) |
| `AnimationComponentData` | `AnimationComponentExecutor` | FSM cast state + animator trigger |
| `AimComponentData` | `AimComponentExecutor` | Caster faces target (ability-driven, not player input) |
| `SoundComponentData` | `SoundComponentExecutor` | One-shot sound |
| `VisualFxComponentData` | `VisualFxComponentExecutor` | Spawn VFX prefab |
| `MovementComponentData` | `MovementComponentExecutor` | Dash / displacement tween |
| `DamageComponentData` | `DamageComponentExecutor` | Instant or gradual damage + hit react |
| `StatusEffectComponentData` | `StatusEffectComponentExecutor` | DoT, heal-over-time, freezing, combustion, etc. |

---

## LockInputComponentData

Controls **player-initiated** ground movement and facing. This is the main requirement for independent movement vs rotation blocking.

| Field | Default | FSM state | Effect on player |
|-------|---------|-----------|------------------|
| **Block Movement** | `true` | Parallel `Input.Movement` | Blocks click-to-move and ongoing walk. Stops current walk when lock is acquired. |
| **Block Rotation** | `false` | Parallel `Input.Rotation` | Blocks **player** rotation: clicking an enemy to face them, and rotation while walking toward a ground point. |

### Four configurations (intentional)

| Block Movement | Block Rotation | Player can… | Player cannot… |
|:--:|:--:|---|---|
| ☐ | ☐ | Move and rotate freely during executor | — |
| ☑ | ☐ | Select enemies and **turn toward them** while casting | Click ground / walk |
| ☐ | ☑ | Click ground and **walk** (without turning while moving) | Turn toward enemy on click; no walk-facing slerp |
| ☑ | ☑ | Select enemies (selection list updates) | Move or rotate |

### What is **not** blocked by these flags

| Action | Why |
|--------|-----|
| **Ability aim on cast** | `AbilityCastingService.FaceTarget` runs **before** the executor; `AimComponentData` rotates via presentation bridge — this is ability logic, not player input. |
| **Enemy selection** (click / Shift+click) | Targeting always works; only **facing** the enemy is blocked when `BlockRotation` is on. |
| **Hotkey cast** on free targets | Cast occupancy is separate (`AlreadyCasting` per target). |
| **Cast animation / dash displacement** | Driven by FSM `Action.Casting.*` / `Locomotion.Displaced`, not input locks. |

### Lock duration

- Locks are acquired when the **`LockInputComponentData` OnStart** executor runs.
- Locks are released on ability phase **`"Complete"`** — after all pending cast effects finish (combustion DoT, gradual damage, displacement tween, status duration on targets). One refcount layer per cast.
- Phase **`"End"`** only resets cast animation (`Action.None` / `Locomotion.Idle`); input locks stay until **`"Complete"`**.
- While an enemy still burns from Firewall, the player **remains movement/rotation locked** if those flags were enabled on that ability.

Parallel casts use **reference counting** per layer (`Input.Movement` depth, `Input.Rotation` depth). Two casts with `BlockMovement` require two **`"Complete"`** events before movement unlocks.

### Demo presets

| Ability | Block Movement | Block Rotation |
|---------|:--------------:|:--------------:|
| Dash | ☑ | ☐ |
| Firewall | ☑ | ☑ |
| Healing | ☑ | ☐ |
| Defenced Attack | ☑ | ☐ |

Configs: `Assets/_Project/Configs/Abilities/*.asset`.

### Healing ability preset (`healing`)

| Component | Notable fields |
|-----------|----------------|
| `LockInputComponentData` | `BlockMovement=☑`, `BlockRotation=☐` |
| `VisualFxComponentData` | `VfxPrefab` → `vfx_healing`, `PresentationStyle=Healing`, `TargetType=Player` |
| `SoundComponentData` | `SoundClip` → `sfx_healing` |
| `AnimationComponentData` | `CastClip` → `anim_Heal` |
| `StatusEffectComponentData` | `EffectType=Healing`, `TargetType=Player` |

Shipped **`Healing.asset`**: ability-level `TargetType=Player` → resolver returns caster. Change to `Enemy` in Inspector → same rules as `firewall` (selection required).

### Implementation map

| Layer | Type | Role |
|-------|------|------|
| Config | `LockInputComponentData` | Designer flags |
| Execution | `LockInputComponentExecutor` | `AcquireCastInputLock(caster, movement, rotation)` |
| FSM | `EntityStateMachineController` | Parallel `Input.Movement` / `Input.Rotation`, refcount |
| Input | `PlayerInputRouter` | `IsMovementBlocked` / `IsRotationBlocked` on click |
| Input | `PlayerMovementService` | Same checks for walk + walk rotation |
| Cast | `AbilityCastingService` | `CancelCurrent()` only if `AbilityInputLockResolver.BlocksMovement` |
| Resolver | `AbilityInputLockResolver` | Reads OnStart `LockInputComponentData` from catalog |

---

## AnimationComponentData

| Field | Purpose |
|-------|---------|
| **Cast Clip** | `AssetReferenceT<AnimationClip>` — Addressables group **Animations**, address `anim_{TriggerName}` (e.g. `anim_Firewall` → trigger `Firewall`). `ResolveAnimationName()` derives the animator trigger / FSM path suffix. |
| **Wait Until End** | When true, executor blocks until cast animation completes (`IAbilityAnimationAwaiter`) before the next OnStart component. |

Cast animation does **not** by itself block player movement — use `LockInputComponentData` for that.

---

## AimComponentData

| Field | Purpose |
|-------|---------|
| **Target Type** | `Player` → face self; `Enemy` → face current cast target. |

Publishes `PresentationAimIntent`; bridge sets caster rotation. Runs even when `BlockRotation` is true (ability-driven aim).

---

## SoundComponentData

| Field | Purpose |
|-------|---------|
| **Sound Clip** | `AssetReferenceT<AudioClip>` — Addressables group **Audio** (e.g. address `sfx_firewall`). Runtime key via `ResolveClipKey()`. |
| **Volume** | 0–1 multiplier. |

---

## VisualFxComponentData

| Field | Purpose |
|-------|---------|
| **Vfx Prefab** | `AssetReferenceGameObject` — Addressables group **VFX** (e.g. `vfx_firewall`). Runtime key via `ResolveVfxKey()`. |
| **Presentation Style** | `Default` or `Healing` (parent-attach + lifetime for heal VFX in demo bridge). |
| **Target Type** | `Player` → spawn on caster; `Enemy` → spawn on cast target. Resolved via `AbilityTargetIdResolver` in executors. |
| **Duration Type** | `Instant` vs `Continuous` (lifetime). |
| **Duration** | Seconds for continuous VFX; despawn scheduling in demo bridge. |
| **Offset X / Y / Z** | Local/world offset from entity VFX anchor. |

Healing VFX uses parent attach + style; combat VFX spawn at world position and are tracked per entity for cleanup on death/despawn.

---

## MovementComponentData

| Field | Purpose |
|-------|---------|
| **Offset X / Y / Z** | Displacement vector (caster-local for dash). |
| **Duration** | Tween duration (`Locomotion.Displaced` + DOTween). |

Abilities with this component **ignore range check** in `AbilityCastingService` (dash closes distance).

---

## DamageComponentData

| Field | Purpose |
|-------|---------|
| **Total Value** | Damage amount (instant if no gradual application). |
| **Tick Value** | Per-tick damage when gradual. |
| **Application Duration** | If &gt; 0 with **Tick Value** &gt; 0 → gradual DoT via `GradualCombatApplier`. |
| **Tick Interval** | Seconds between gradual ticks. |

Target enters `Action.HitReact` on damage. Instant vs gradual is `IsGradual` = `ApplicationDuration > 0 && TickValue > 0`.

---

## StatusEffectComponentData

| Field | Purpose |
|-------|---------|
| **Effect Type** | `Combustion` (**burning** / DoT), `Freezing`, `Healing`, `Bleeding`. See [Abilities-System.md](Abilities-System.md#terminology-combustion--burning). |
| **Target Type** | `Player` → apply to caster; `Enemy` → apply to cast target. |
| **Duration Type** | Usually `Continuous`. |
| **Duration** | Status FSM lifetime (seconds). |
| **Value** | Per-tick magnitude (damage or heal). |
| **Total Value** | Optional cap on total damage/heal from this status. |
| **Tick Interval** | Seconds between FSM status ticks. |
| **Additional Vfx** | Optional `AssetReferenceGameObject` spawned on status apply. Runtime key via `ResolveAdditionalVfxKey()`. |

**Freezing** also enters parallel `Input.Movement` on the **affected entity** (blocks their walk until status ends) — independent of caster `LockInput`.

**Healing** ability uses this with `EffectType = Healing` (not negative damage). See [Abilities-System.md](Abilities-System.md#healing-design-choice-demo).

---

## Cast occupancy vs input locks (do not confuse)

| Mechanism | Scope | Purpose |
|-----------|-------|---------|
| **`LockInputComponentData`** | Player caster | Configurable movement/rotation block for **full cast lifecycle** (until `"Complete"`) |
| **`HasActiveCasts(caster)`** | Caster session | Counts active cast sessions on the caster (`_activeCastSessionsByCaster`); **not** the same as per-target **`AlreadyCasting`** |
| **`_busyTargets` (internal)** | Per target entity | Second cast on same enemy → **`AlreadyCasting`** |

So: **enemy still burning after cast animation ends** while **player cannot walk** is expected when `BlockMovement` is enabled — locks last until phase **`Complete`**.

## “Looks like a bug” — expected behaviour

| Observation | Explanation |
|-------------|-------------|
| Can turn to another enemy while Healing but cannot walk | Healing has `BlockMovement=☑`, `BlockRotation=☐` — by config. |
| Cannot walk or turn during entire Firewall burn on enemy | Locks held until **`Complete`** when all pending effects (status DoT) finish. |
| Caster snaps to face target on hotkey despite `BlockRotation` | Pre-cast `FaceTarget` + `AimComponent` are ability aim, not player rotation input. |
| Shift+click works but player does not face new enemy | `BlockRotation` is active — selection updates, facing is blocked until **`Complete`**. |
| Two Firewall casts in a row — still cannot move after first burn ends | Second cast may still hold movement lock refcount; or first cast **`Complete`** not yet received. |
| Firewall shows **-25** then **-8** on enemy | **Not a bug.** Instant `DamageComponentData` (25) + first `Combustion` status tick (8). Different colors: red Damage vs orange Dot. See [Prototype-Guide.md](Prototype-Guide.md#combat--floating-numbers-expected-behaviour). |
| Player and enemy both lose HP when bodies overlap | **Not a bug.** `EntityContactDamageService` applies **8** damage to **both** sides every ~1s within 0.9 units — symmetric contact damage (L3). |

---

## Addressables registration

### Ability configs

Each config must be in Addressables with:

- **Group:** `Abilities`
- **Address:** `Ability_{ability_id}`
- **Label:** **`ability`**

Loaded by `AbilityAddressableDiscovery` into `AbilityCatalog` at boot.

### Media assets (component `AssetReference` fields)

| Group | Used by | Example addresses |
|-------|---------|-------------------|
| **Audio** | `SoundComponentData.SoundClip` | `sfx_dash`, `sfx_firewall`, … |
| **VFX** | `VisualFxComponentData.VfxPrefab`, `StatusEffectComponentData.AdditionalVfx` | `vfx_dash`, `vfx_firewall`, … |
| **Animations** | `AnimationComponentData.CastClip` | `anim_Dash`, `anim_Firewall`, `anim_Heal`, `anim_DefencedAttack` |
| **Demo** | `DemoAddressableCatalog` (arena, player, enemy prefabs) | `demo_arena`, `demo_player`, … |

Authoring uses `AssetReference*` on component data; **Execution** resolves to string keys via `Resolve*Key()` / `ResolveAnimationName()` and never references `Unity.Addressables` directly.

Run **AvantajPrim → Project → Setup Addressables Bootstrap** to create wrapper assets, cast animation clips, and register all groups.
