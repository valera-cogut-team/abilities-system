# Code Style Guide — Avantaj Prim

Human-readable companion to [`Assets/_Project/.editorconfig`](../.editorconfig). **Scope:** production C# rules apply only under `Assets/_Project/`; third-party assets (TextMesh Pro, Cartoon FX, etc.) use the minimal repo-root [`.editorconfig`](../../../.editorconfig) baseline only. IDE and CI enforce formatting and analyzers from EditorConfig; this document explains rationale, Unity exceptions, and team conventions.

## Philosophy

- **Explicit over implicit** — every member has an access modifier; DI fields are `private readonly`.
- **Readonly by default** — mutate only when lifecycle requires it (Unity `[SerializeField]`, caches updated after `Awake`).
- **Ports over statics** — gameplay uses facades and Zenject injection; static bridges are limited to documented editor tooling (`AbilityEditorPlayAccess`).
- **Single tick** — no ad-hoc `Update()` in gameplay; register on `ILifeCycleFacade`.

## File layout

1. **Usings** — `System` → `UnityEngine` → third-party (UniRx, UniTask, Zenject, DOTween) → project namespaces.
2. **One primary type per file** — file name matches type name.
3. **Namespace** — matches folder / assembly (`AvantajPrim.*`, module facades use short names e.g. `Effects.Facade`).

## Unity conventions

| Rule | Example |
|------|---------|
| Serialized fields | `[SerializeField] private Animator _animator;` — **not** `readonly` |
| Public MonoBehaviour API | Minimal; prefer facades and presentation adapters |
| `Find*` / `GetComponent` | Cache in `Awake`/`Initialize`; no per-frame lookup |
| Lifecycle | `AppEntryPoint`, `IModule`, `IUpdateHandler` — not scattered `MonoBehaviour.Update` |

## DI / Zenject

- Constructor injection for services (`AbilitiesDemoInstaller`, module installers).
- `[Inject]` only where Unity requires `MonoBehaviour` construction order.
- No service locator in runtime gameplay code.

## Async

- Prefer **UniTask** over `Task` for Unity main-thread work.
- `.Forget()` only for fire-and-forget with local error handling (e.g. `EntityDespawnService.ScheduleDespawnAsync`).
- Do not use `async void` except documented UniTaskVoid / Unity event handlers.

## Naming (summary)

| Symbol | Pattern | Example |
|--------|---------|---------|
| Interface | `I` + PascalCase | `IAbilitiesFacade` |
| Type | PascalCase | `EntityDespawnService` |
| Method / property / event | PascalCase | `ScheduleDespawn` |
| Private field | `_camelCase` | `_combatRegistry`, `_abilityId` |
| Parameter / local | camelCase | `targetId` |
| Async method | `*Async` suffix | `CastAsync` |

Full naming rules live in `Assets/_Project/.editorconfig` (`dotnet_naming_*`).

## Access modifiers & readonly

| Construct | Rule |
|-----------|------|
| DI / ctor-injected field | `private readonly Type _field` |
| `[SerializeField]` | `private` explicitly; not `readonly` |
| Helper methods | `private` explicitly |
| Interface members | No access modifier (implicit public) |
| Nested types | `private struct` / `private sealed class` with explicit access on all members |
| Nested struct fields | `private` or `internal` — no `public` mutable fields; prefer copy-update for dictionary-backed state |

### Readonly heuristics

Use `private readonly` when a field is assigned once (ctor, or single `Initialize`/`Bind` before use) and never reassigned:

- DI dependencies, facades, registries wired at construction
- Presentation orchestrator deps (`AbilitiesDemoPresentationOrchestrator`)
- `AbilitiesDemoTickHandler` registry + facade (ctor injection)

**Keep mutable** when lifecycle requires reassignment or counters: `_hitReactCts`, `*Depth`, `*Count`, `*Cts`, `_subscriptions`, runtime caches, module `_context` (assigned in `Initialize`, not ctor).

Automation: `Tools/add_readonly_fields.py` (safe heuristics; review diff before commit).

## MonoBehaviour policy

Only two runtime `MonoBehaviour` types in `Assets/_Project`:

| Type | Role |
|------|------|
| `AppEntryPoint` | Bootstrap entry, module enable/disable |
| `EntityView` | Entity anchor (animator, collider, transform) |

All other presentation (FSM controller, health bars, combat float text, VFX pool handles, targeting ring, world UI, presentation bridge) is **plain C#** wired via Zenject and `AbilitiesDemoPresentationOrchestrator`. `GameObject`/`Transform` are held as references only.

Cleanup uses explicit `IDisposable` (`EntityStateMachineController`, `AbilityPresentationBridge`, orchestrator) plus `EntityDespawnService` on entity removal — not `OnDestroy` on helper scripts.

## Intentional exceptions

| Rule | Reason |
|------|--------|
| `CA2007` disabled | Unity main thread; no `ConfigureAwait` |
| `CA1707` on public API | Underscores disallowed on public types/members; `api_surface = public` skips private `_fields` |
| `CA1707` in tests | NUnit method names may use underscores (`severity = none`) |
| `async void` banned | Use `UniTask` / `UniTaskVoid` |
| Generated / `.meta` / `.asset` | Excluded from C# analyzer sections in EditorConfig |
| Third-party `Assets/**` (outside `_Project`) | Repo-root EditorConfig only — no AAA C# analyzers |

## Migration checklist (modifiers pass)

Order used for explicit `private` / `readonly` rollout:

1. `Abilities.Domain` → Data → Execution → Infrastructure → Abilities module  
2. Core modules: LifeCycle, Input, Pool, Effects, Audio, Addressables, StateMachine, Logger  
3. `AbilitiesDemo`: Application → Presentation → Facade → Installers  
4. Bootstrap + Editor  
5. Tests (EditMode + PlayMode)  

After each assembly: `dotnet build <Assembly>.csproj` or `Tests.EditMode.csproj` / `Tests.PlayMode.csproj`.

Automation: `Tools/add_explicit_access_modifiers.py` (all nested type bodies, `brace_depth >= 2`; **never** apply to interface bodies).

### Post-phase verification

1. Rider/ReSharper: 0 style warnings in `Assets/_Project` (except intentional `[Serializable]` component data).
2. `grep -r ": MonoBehaviour" Assets/_Project` → only `AppEntryPoint`, `EntityView`.
3. `grep -r "using System.Threading.Tasks" Assets/_Project` → empty.
4. EditMode + PlayMode tests green.
5. Manual smoke: bootstrap → spawn → 4 abilities → despawn → targeting ring.

## IDE setup

| IDE | Action |
|-----|--------|
| **Rider** | Enable EditorConfig; Code Cleanup profile = EditorConfig |
| **VS / VS Code** | Format on Save; C# Dev Kit reads `.editorconfig` (scoped under `Assets/_Project`) |
| **Unity** | External Script Editor = Rider/VS; do not override indent in Unity preferences |

Optional CI: `dotnet format --verify-no-changes` on `_Project` scope (see `.github/workflows/unity-tests.yml`).
