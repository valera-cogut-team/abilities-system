# How to Add an Ability Component Type (Developer)

Guide for adding a new `IAbilityComponentData` type and its runtime executor.

## Overview

1. Add a **data class** in `Abilities.Data` (serializable, no Unity calls).
2. Add an **executor** in `Abilities.Execution/Executors/`.
3. **Register** the executor in `AbilitiesInstaller`.
4. (Optional) Extend **presentation port** if the component needs new side effects.

## 1. Data class

Create a file under `Assets/_Project/Source/Modules/Core/Abilities/Data/Components/`:

```csharp
using System;
using AvantajPrim.Abilities.Domain;

namespace AvantajPrim.Abilities.Data
{
    [Serializable]
    public sealed class MyComponentData : IAbilityComponentData
    {
        public AbilityPlayTimeType PlayTimeType = AbilityPlayTimeType.OnStart;
        public float DelaySeconds;
        public float MyParameter = 1f;

        AbilityPlayTimeType IAbilityComponentData.PlayTimeType => PlayTimeType;
        float IAbilityComponentData.DelaySeconds => DelaySeconds;
    }
}
```

Rules:

- Implement `IAbilityComponentData` explicitly for `PlayTimeType` / `DelaySeconds`.
- Use `[Serializable]` so Unity can store it in `AbilityConfigAsset`.
- Keep fields designer-friendly (public or `[SerializeField]`).

The custom editor on `AbilityConfigAsset` auto-discovers all non-abstract `IAbilityComponentData` types in `Abilities.Data`.

## 2. Executor

Create a class in `Execution/Executors/`:

```csharp
using System;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Domain.Ports;

namespace AvantajPrim.Abilities.Execution.Executors
{
    public sealed class MyComponentExecutor : IAbilityComponentExecutor
    {
        public Type DataType => typeof(MyComponentData);

        public void Execute(IAbilityComponentData data, AbilityExecutionContext context,
            IAbilityPresentationPort presentation, IEntityStatePort entityState)
        {
            if (data is not MyComponentData d) return;
            // Use context.CasterId, context.TargetId, d.MyParameter
            // Call presentation.Publish* or entityState.TryTransition as needed
        }
    }
}
```

`AbilityExecutor` calls `TryResolve` by **runtime type** of each component instance.

## 3. Register in installer

In `AbilitiesInstaller.Install`:

```csharp
registry.Register(new MyComponentExecutor());
```

Order of registration does not matter; lookup is by `DataType`.

## 4. Presentation (if needed)

If the component affects visuals/audio/movement:

1. Add a domain intent struct in `Domain/AbilityEvents.cs` (or a dedicated file).
2. Add `Publish*` + observable on `IAbilityPresentationPort` and `AbilityPresentationPort`.
3. Subscribe in `AbilityPresentationBridge` (demo) or your game view layer.

Keep **game rules** in executors; keep **Unity reactions** in presentation subscribers.

## 5. Tests

Add EditMode coverage under `Assets/_Project/Tests/EditMode/`:

- Registry resolves your data type.
- Executor performs expected port calls with a `RecordingPresentationPort` test double.

## Assembly constraints

| Layer | Constraint |
|---|---|
| `Abilities.Domain` | No Unity references |
| `Abilities.Execution` | No Unity references — use ports only; resolve asset keys via Data `Resolve*()` methods |
| `Abilities.Data` | May use `UnityEngine` + `Unity.Addressables` for `ScriptableObject` and `AssetReference*` authoring |

For components that reference Addressable assets (sound, VFX, animation), add `AssetReferenceT<T>` / `AssetReferenceGameObject` fields and `Resolve*Key()` helpers using `AddressableAssetRefUtility` (see existing `SoundComponentData`, `VisualFxComponentData`, `AnimationComponentData`).

## File checklist

- [ ] `Data/Components/MyComponentData.cs`
- [ ] `Execution/Executors/MyComponentExecutor.cs`
- [ ] `Installers/AbilitiesInstaller.cs` — register executor
- [ ] (Optional) port + presentation bridge
- [ ] (Optional) unit test

Designer-facing field documentation for all built-in components: [Ability-Config-Reference.md](Ability-Config-Reference.md).
