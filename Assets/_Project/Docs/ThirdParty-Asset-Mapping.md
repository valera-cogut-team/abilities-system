# Third-Party Asset Mapping

Addressable keys used by the abilities demo and their source paths under `Assets/ThridPartyAssets`.

Component configs reference assets via **`AssetReference*`** fields (not raw strings). Runtime addresses below are resolved through `ResolveClipKey()`, `ResolveVfxKey()`, `ResolveAnimationName()`, etc. See [Ability-Config-Reference.md](Ability-Config-Reference.md#addressables-registration).

## Demo scene

| Addressable key | Asset path | Notes |
|---|---|---|
| `demo_player` | `WizardPolyArt/Prefabs/PolyArtWizardStandardMat.prefab` | Player wizard + `DemoWizardAbilityAnimator.controller` |
| `demo_enemy` | `WizardPolyArt/Prefabs/PolyArtWizardMaskTintMat.prefab` | Reused for 3 enemy instances; `WizardAnimControl_Enemy.controller` |
| `demo_arena` | `SimpleNaturePack/Prefabs/Ground_01.prefab` | Scaled 2×; trees/rocks added at bootstrap |

## VFX (CFXR) — group **VFX**

Cartoon FX Remaster prefabs live under `JMO Assets/Cartoon FX Remaster/CFXR Prefabs/`. Bootstrap copies them into sanitized wrappers at `Assets/_Project/Prefabs/VFX/` (missing third-party scripts stripped) and registers those wrappers in Addressables.

| Addressable key | Wrapper | Third-party source |
|---|---|---|
| `vfx_dash` | `VFX_Dash.prefab` | `CFXR Magic Poof.prefab` |
| `vfx_firewall` | `VFX_Firewall.prefab` | `CFXR2 Firewall A.prefab` |
| `vfx_healing` | `VFX_Healing.prefab` | `CFXR4 Healing.prefab` |
| `vfx_defenced_attack` | `VFX_DefencedAttack.prefab` | `CFXR4 Sword Hit ICE (Cross).prefab` |
| `vfx_freezing` | `VFX_Freezing.prefab` | (status effect VFX) |
| `vfx_bleeding` | `VFX_Bleeding.prefab` | (status effect VFX) |

## SFX (Free Frost Mage) — group **Audio**

| Addressable key | Asset path |
|---|---|
| `sfx_dash` | `Sounds/Free Frost Mage - SFX/ice_teleport_out_02.wav` |
| `sfx_firewall` | `Sounds/Free Frost Mage - SFX/frozen_wall_09.wav` |
| `sfx_healing` | `Sounds/Free Frost Mage - SFX/Crystallize_06.wav` |
| `sfx_defenced_attack` | `Sounds/Free Frost Mage - SFX/frozen_armor_12.wav` |

## Cast animations — group **Animations**

Clips are extracted from wizard FBX files into `Assets/_Project/Animations/Cast/` during bootstrap. Address prefix `anim_` maps to animator trigger name (`anim_Dash` → `Dash`).

| Addressable key | Cast clip | Source FBX / clip |
|---|---|---|
| `anim_Dash` | `Cast_Dash.anim` | `BattleRunForward.fbx` / `BattleRunForward` |
| `anim_Firewall` | `Cast_Firewall.anim` | `Attack02Start.fbx` / `Attack02Start` |
| `anim_Heal` | `Cast_Heal.anim` | `PotionDrink.fbx` / `PotionDrink` |
| `anim_DefencedAttack` | `Cast_DefencedAttack.anim` | `DefendHit.fbx` / `DefendHit` |

## Ability configs — group **Abilities**

| Addressable key | Config asset |
|---|---|
| `Ability_dash` | `Assets/_Project/Configs/Abilities/Dash.asset` |
| `Ability_firewall` | `Assets/_Project/Configs/Abilities/Firewall.asset` |
| `Ability_healing` | `Assets/_Project/Configs/Abilities/Healing.asset` |
| `Ability_defenced_attack` | `Assets/_Project/Configs/Abilities/DefencedAttack.asset` |

All ability configs carry the Addressables label **`ability`** for runtime discovery via `AbilityAddressableDiscovery`.

## Demo catalog

`Assets/_Project/Resources/DemoAddressableCatalog.asset` holds `AssetReference` fields for arena/player/enemy, SFX, VFX, and cast animation clips — used by bootstrap and `AbilityPresentationBridge` preloading.

## Editor bootstrap

Run **AvantajPrim → Project → Setup Addressables Bootstrap** to:

1. Copy third-party assets into `_Project` wrappers (VFX, SFX, cast clips)
2. Register all Addressables groups (**Abilities**, **Demo**, **VFX**, **Audio**, **Animations**)
3. Create `DemoWizardAbilityAnimator.controller` and default ability configs
4. Refresh `DemoAddressableCatalog`
