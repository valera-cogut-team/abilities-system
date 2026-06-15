# Ability Authoring Window

Open **AvantajPrim → Abilities → Authoring Window** for a single place to manage ability configs.

## Edit Mode

- **Refresh** — reload configs from `Assets/_Project/Configs/Abilities/`.
- **Create** — new `AbilityConfigAsset` with a unique id.
- **Duplicate** — copy the selected config.
- **Validate** — checks duplicate ids, hotkey slots, assigned `AssetReference` on Animation + Sound components, and recommended component presence.
- **Register Addressable** — adds `Ability_<id>` address and `ability` label for runtime loading.

Use the embedded inspector to edit metadata, **Hotkey Slot** (1–9 fallback), **Hotkey Key** (explicit `KeyCode`), and components.

## Play Mode

While the demo is running:

1. Open the Authoring Window.
2. Click **Bind** next to an ability, then press a key — binding applies immediately (no restart).
3. **Clear** removes the runtime override for one ability; **Clear All Runtime Overrides** resets all.
4. **Save Hotkey To Asset** writes the runtime override into the ScriptableObject `_hotkeyKey` field.
5. **Replay Last Cast** — same as **AvantajPrim → Abilities → Replay Last Cast**.

Runtime resolution order: **runtime override** → **Hotkey Key** on asset → **Hotkey Slot** (`Alpha1`…`Alpha9`).
