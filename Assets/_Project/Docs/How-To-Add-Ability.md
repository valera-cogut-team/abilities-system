# How to Add an Ability (Designer / No Code)

Add a new ability using only ScriptableObjects and the Unity Inspector.

## Prerequisites

- Unity **6000.3.15f1**
- Ability component types already exist in the project (see [How-To-Add-Ability-Component.md](How-To-Add-Ability-Component.md) for new types).

## Steps

### 1. Duplicate an existing config

1. In the Project window, go to `Assets/_Project/Configs/Abilities/`.
2. Duplicate an ability close to what you need (e.g. `Dash.asset`).
3. Rename the asset (e.g. `MyAbility.asset`).

### 2. Set ability metadata

Select the asset and set:

| Field | Guidance |
|---|---|
| **Ability Id** | Unique lowercase id with underscores (e.g. `my_ability`). Must match what code/UI uses when casting. |
| **Display Name** | Designer-facing label (logs, editor, docs). |
| **Hotkey Slot** | Fallback keyboard slot `1–9` (`Alpha1`…`Alpha9`) when Hotkey Key is `None`. |
| **Hotkey Key** | Optional explicit `KeyCode`; overrides slot when set. Remap in Play Mode via **Ability Authoring Window**. |
| **Target Type** | `Player` = cast on caster (no enemy selection). `Enemy` = cast on selected enemies (fail if none). Same rules for every ability id. |
| **Range Type** | `Melee` or `Ranged`. |
| **Range** | Distance for range validation when `TargetType = Enemy`. Ignored when ability has `MovementComponentData` (dash). |

See [Ability-Config-Reference.md](Ability-Config-Reference.md) for every metadata and component field.

### 3. Edit components

The **Components** list uses `[SerializeReference]` entries.

- Expand each component to set **Play Time Type** (`OnStart`, `OnEnd`, `Delay`) and **Delay Seconds**.
- Tune type-specific fields (damage value, status duration, etc.).
- Assign **AssetReference** fields in the Inspector (not string keys): `SoundClip`, `VfxPrefab`, `CastClip`, `AdditionalVfx` — see [Ability-Config-Reference.md](Ability-Config-Reference.md).
- Use **Add Component** at the bottom of the custom inspector to insert a new component type.

Typical order for a damaging skill:

1. `LockInputComponentData` — OnStart (`BlockMovement` / `BlockRotation` — see [Ability-Config-Reference.md](Ability-Config-Reference.md#lockinputcomponentdata))  
2. `AimComponentData` — OnStart (optional; caster faces target)  
3. `AnimationComponentData` — OnStart (`CastClip` = Addressables animation clip, address `anim_{TriggerName}`)  
4. `VisualFxComponentData` — OnStart or Delay  
5. `DamageComponentData` — OnStart or Delay  
6. `SoundComponentData` — OnStart or OnEnd  

### 4. Register the asset for loading

The runtime loads abilities by **Addressable address**, not file path.

1. Open **Window → Asset Management → Addressables → Groups**.
2. Add your asset to the **Abilities** group.
3. Set the address to `Ability_<your_ability_id>` (e.g. `Ability_my_ability`).
4. Add the label **`ability`** to the entry (same as other configs).

Register any **Sound / VFX / Animation** clips used by components in the matching groups (**Audio**, **VFX**, **Animations**) — see [ThirdParty-Asset-Mapping.md](ThirdParty-Asset-Mapping.md).

At runtime, `AbilityAddressableDiscovery` loads every config with label `ability` into `AbilityCatalog`. Assign **Hotkey Slot** (1–4) in the config for keyboard casting in the demo — no C# changes required for a new ability.

Set **Hotkey Slot** if the ability should respond to a number key in `AbilitiesInputService`.

### 5. Verify in Play Mode

1. Open `Assets/_Project/Scenes/BootstrapScene.unity`.
2. Press Play — the demo starts immediately.
3. Cast the ability and confirm VFX, sound, damage, and status effects behave as expected.

## Checklist

- [ ] Unique **Ability Id** set on the asset  
- [ ] Components ordered with correct **Play Time Type**  
- [ ] `AssetReference` fields assigned (Sound / VFX / Animation) and registered in Addressables  
- [ ] Ability config Addressable entry with `Ability_` prefix + label `ability`  
- [ ] Tested in Play Mode  

## Common Mistakes

- **Duplicate Ability Id** — second ability overwrites the first in `AbilityCatalog`.
- **Wrong Play Time** — `OnEnd` components wait an extra 0.5s; instant damage should usually be `OnStart`.
- **Missing Addressable** — asset exists but is never loaded at runtime.
