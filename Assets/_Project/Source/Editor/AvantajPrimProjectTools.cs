using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EditorTools
{
    public static class AvantajPrimProjectTools
    {
        private const string ConfigDir = "Assets/_Project/Configs/Abilities";
        private const string CatalogPath = "Assets/_Project/Resources/DemoAddressableCatalog.asset";
        private const string PrefabDir = "Assets/_Project/Prefabs/UI";
        private const string DemoPrefabDir = "Assets/_Project/Prefabs/Demo";
        private const string VfxPrefabDir = "Assets/_Project/Prefabs/VFX";
        private const string SfxDir = "Assets/_Project/Audio/SFX";
        private const string AnimCastDir = "Assets/_Project/Animations/Cast";
        private const string ThirdPartyAnimRoot = "Assets/ThridPartyAssets/WizardPolyArt/Animations";
        private const string AddressablesDir = "Assets/_Project/Addressables/Abilities";

        private const string ThirdPartyWizard = "Assets/ThridPartyAssets/WizardPolyArt/Prefabs/PolyArtWizardStandardMat.prefab";
        private const string ThirdPartyWizardEnemy = "Assets/ThridPartyAssets/WizardPolyArt/Prefabs/PolyArtWizardMaskTintMat.prefab";
        private const string ThirdPartyGround = "Assets/ThridPartyAssets/SimpleNaturePack/Prefabs/Ground_01.prefab";
        private const string ThirdPartyTree = "Assets/ThridPartyAssets/SimpleNaturePack/Prefabs/Tree_01.prefab";
        private const string ThirdPartyTree02 = "Assets/ThridPartyAssets/SimpleNaturePack/Prefabs/Tree_02.prefab";
        private const string ThirdPartyTree03 = "Assets/ThridPartyAssets/SimpleNaturePack/Prefabs/Tree_03.prefab";
        private const string ThirdPartyTree04 = "Assets/ThridPartyAssets/SimpleNaturePack/Prefabs/Tree_04.prefab";
        private const string ThirdPartyTree05 = "Assets/ThridPartyAssets/SimpleNaturePack/Prefabs/Tree_05.prefab";
        private const string ThirdPartyBush01 = "Assets/ThridPartyAssets/SimpleNaturePack/Prefabs/Bush_01.prefab";
        private const string ThirdPartyBush02 = "Assets/ThridPartyAssets/SimpleNaturePack/Prefabs/Bush_02.prefab";
        private const string ThirdPartyRock = "Assets/ThridPartyAssets/SimpleNaturePack/Prefabs/Rock_02.prefab";
        private const string PlayerAnimatorController = DemoPrefabDir + "/DemoWizardAbilityAnimator.controller";
        private const string EnemyAnimatorController =
            "Assets/ThridPartyAssets/WizardPolyArt/Animations/WizardAnimControl_Enemy.controller";
        private const string ThirdPartyVfxDash = "Assets/ThridPartyAssets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Magic Poof.prefab";
        private const string ThirdPartyVfxFirewall = "Assets/ThridPartyAssets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR2 Firewall A.prefab";
        private const string ThirdPartyVfxHealing = "Assets/ThridPartyAssets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR4 Healing.prefab";
        private const string ThirdPartyVfxDefAttack = "Assets/ThridPartyAssets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Sword Trails/Ice/CFXR4 Sword Hit ICE (Cross).prefab";
        private const string ThirdPartyVfxFreezing = "Assets/ThridPartyAssets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Sword Trails/Ice/CFXR4 Sword Trail ICE (360 Thin Spiral).prefab";
        private const string ThirdPartyVfxBleeding = "Assets/ThridPartyAssets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Liquids/CFXR2 Blood (Directional).prefab";
        private const string ThirdPartySfxDash = "Assets/ThridPartyAssets/Sounds/Free Frost Mage - SFX/ice_teleport_out_02.wav";
        private const string ThirdPartySfxFirewall = "Assets/ThridPartyAssets/Sounds/Free Frost Mage - SFX/frozen_wall_09.wav";
        private const string ThirdPartySfxHealing = "Assets/ThridPartyAssets/Sounds/Free Frost Mage - SFX/Crystallize_06.wav";
        private const string ThirdPartySfxDefAttack = "Assets/ThridPartyAssets/Sounds/Free Frost Mage - SFX/frozen_armor_12.wav";

        private static class BootstrapAddresses
        {
            public const string Dash = "Ability_dash";
            public const string Firewall = "Ability_firewall";
            public const string Healing = "Ability_healing";
            public const string DefencedAttack = "Ability_defenced_attack";

            public const string Arena = "demo_arena";
            public const string Player = "demo_player";
            public const string Enemy = "demo_enemy";

            public const string NatureTree01 = "nature_tree_01";
            public const string NatureTree02 = "nature_tree_02";
            public const string NatureTree03 = "nature_tree_03";
            public const string NatureTree04 = "nature_tree_04";
            public const string NatureTree05 = "nature_tree_05";
            public const string NatureBush01 = "nature_bush_01";
            public const string NatureBush02 = "nature_bush_02";

            public const string SfxDash = "sfx_dash";
            public const string SfxFirewall = "sfx_firewall";
            public const string SfxHealing = "sfx_healing";
            public const string SfxDefencedAttack = "sfx_defenced_attack";

            public const string VfxDash = "vfx_dash";
            public const string VfxFirewall = "vfx_firewall";
            public const string VfxHealing = "vfx_healing";
            public const string VfxDefencedAttack = "vfx_defenced_attack";
            public const string VfxFreezing = "vfx_freezing";
            public const string VfxBleeding = "vfx_bleeding";

            public const string AnimDash = "anim_Dash";
            public const string AnimFirewall = "anim_Firewall";
            public const string AnimHeal = "anim_Heal";
            public const string AnimDefencedAttack = "anim_DefencedAttack";
        }

        [MenuItem("AvantajPrim/Project/Setup Addressables Bootstrap")]
        public static void MenuSetupAddressablesBootstrap() => SetupAddressablesBootstrap(buildPlayerContent: true);

        [MenuItem("AvantajPrim/Project/Create Default Ability Configs")]
        public static void MenuCreateDefaultAbilityConfigs() => CreateDefaultAbilityConfigs();

        [MenuItem("AvantajPrim/Abilities/Replay Last Cast")]
        public static void MenuReplayLastCast() => ReplayLastCast();

        public static void ReplayLastCastFromMenu() => ReplayLastCast();

        public static void RegisterAbilityAddressable(AbilityConfigAsset asset)
        {
            if (asset == null)
            {
                Debug.LogWarning("[AvantajPrim] Cannot register null ability asset.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[AvantajPrim] Ability asset has no project path.");
                return;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("[AvantajPrim] AddressableAssetSettings could not be loaded.");
                return;
            }

            AddressableAssetGroup abilitiesGroup = settings.FindGroup(DemoConstants.AddressableGroups.Abilities)
                                                   ?? settings.CreateGroup(DemoConstants.AddressableGroups.Abilities, false, false, true, null,
                                                       typeof(BundledAssetGroupSchema));

            string guid = AssetDatabase.AssetPathToGUID(path);
            string address = $"Ability_{asset.AbilityId}";
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, abilitiesGroup, false, false);
            entry.SetAddress(address, false);
            entry.SetLabel(AbilityAddressableDiscovery.AbilityLabel, true, true);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AvantajPrim] Registered Addressable '{address}' for {path}");
        }

        [MenuItem("AvantajPrim/Project/Refresh Demo Entity Prefabs")]
        public static void MenuRefreshDemoEntityPrefabs() => RefreshDemoEntityPrefabs();

        [MenuItem("AvantajPrim/Project/Refresh Demo Arena Decor")]
        public static void MenuRefreshDemoArenaDecor() => RefreshDemoArenaDecor();

        public static void BatchBootstrap()
        {
            try
            {
                SetupAddressablesBootstrap(buildPlayerContent: true);
                CreateDefaultAbilityConfigs();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        public static void SetupAddressablesBootstrap(bool buildPlayerContent)
        {
            EnsureFolder("Assets/_Project/Addressables");
            EnsureFolder(AddressablesDir);
            EnsureFolder(PrefabDir);
            EnsureFolder(DemoPrefabDir);
            EnsureFolder(VfxPrefabDir);
            EnsureFolder(SfxDir);
            EnsureFolder(AnimCastDir);
            EnsureFolder(ConfigDir);
            EnsureFolder("Assets/_Project/Resources");

            DemoWizardAnimatorBuilder.CreateIfMissing();
            CreateDemoPrefabs();
            CreateVfxPrefabs();
            CreateSfxPlaceholders();
            CreateCastAnimationClips();
            CreateDefaultAbilityConfigs();
            RegisterAddressablesEntries();
            CreateOrUpdateDemoAddressableCatalog();
            EnsureAddressablesProfile(AddressableAssetSettingsDefaultObject.Settings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (buildPlayerContent)
                AddressableAssetSettings.BuildPlayerContent();

            Debug.Log("[AvantajPrim] Addressables bootstrap finished.");
        }

        public static void CreateDefaultAbilityConfigs()
        {
            EnsureFolder(ConfigDir);

            CreateOrUpdateAbilityConfig(
                ConfigDir + "/Dash.asset",
                DemoConstants.AbilityIds.Dash,
                "Dash",
                1,
                AbilityTargetType.Enemy,
                AbilityRangeType.Melee,
                DemoConstants.AbilityAuthoring.DashRange,
                new IAbilityComponentData[]
                {
                    new LockInputComponentData(),
                    new VisualFxComponentData
                    {
                        VfxPrefab = GameObjectRef(VfxPrefabDir + "/VFX_Dash.prefab", ThirdPartyVfxDash),
                        TargetType = AbilityTargetType.Player
                    },
                    new SoundComponentData { SoundClip = AudioClipRef(SfxDir + "/SFX_Dash.wav", ThirdPartySfxDash) },
                    new AnimationComponentData { CastClip = AnimationClipRef(AnimCastDir + "/Cast_Dash.anim") },
                    new AimComponentData(),
                    new MovementComponentData
                    {
                        OffsetZ = DemoConstants.AbilityAuthoring.DashOffsetZ,
                        Duration = DemoConstants.AbilityAuthoring.DashDurationSeconds
                    }
                });

            CreateOrUpdateAbilityConfig(
                ConfigDir + "/Firewall.asset",
                DemoConstants.AbilityIds.Firewall,
                "Firewall",
                2,
                AbilityTargetType.Enemy,
                AbilityRangeType.Ranged,
                DemoConstants.AbilityAuthoring.FirewallRange,
                new IAbilityComponentData[]
                {
                    new LockInputComponentData { BlockRotation = true },
                    new AimComponentData(),
                    new SoundComponentData { SoundClip = AudioClipRef(SfxDir + "/SFX_Firewall.wav", ThirdPartySfxFirewall) },
                    new AnimationComponentData
                    {
                        CastClip = AnimationClipRef(AnimCastDir + "/Cast_Firewall.anim"),
                        WaitUntilEnd = true
                    },
                    new VisualFxComponentData
                    {
                        PlayTimeType = AbilityPlayTimeType.Delay,
                        DelaySeconds = DemoConstants.AbilityAuthoring.FirewallVfxDelaySeconds,
                        VfxPrefab = GameObjectRef(VfxPrefabDir + "/VFX_Firewall.prefab", ThirdPartyVfxFirewall),
                        DurationType = AbilityDurationType.Continuous,
                        Duration = DemoConstants.AbilityAuthoring.FirewallCombustionDurationSeconds
                    },
                    new DamageComponentData
                    {
                        PlayTimeType = AbilityPlayTimeType.Delay,
                        DelaySeconds = DemoConstants.AbilityAuthoring.FirewallDamageDelaySeconds,
                        TotalValue = DemoConstants.AbilityAuthoring.FirewallDamageTotal,
                        TickValue = DemoConstants.AbilityAuthoring.FirewallDamageTotal
                    },
                    new StatusEffectComponentData
                    {
                        PlayTimeType = AbilityPlayTimeType.OnEnd,
                        EffectType = StatusEffectType.Combustion,
                        Duration = DemoConstants.AbilityAuthoring.FirewallCombustionDurationSeconds,
                        Value = DemoConstants.AbilityAuthoring.FirewallCombustionValue,
                        AdditionalVfx = GameObjectRef(VfxPrefabDir + "/VFX_Firewall.prefab", ThirdPartyVfxFirewall)
                    }
                });

            CreateOrUpdateAbilityConfig(
                ConfigDir + "/Healing.asset",
                DemoConstants.AbilityIds.Healing,
                "Healing",
                3,
                AbilityTargetType.Player,
                AbilityRangeType.Melee,
                0f,
                new IAbilityComponentData[]
                {
                    new LockInputComponentData(),
                    new VisualFxComponentData
                    {
                        VfxPrefab = GameObjectRef(VfxPrefabDir + "/VFX_Healing.prefab", ThirdPartyVfxHealing),
                        PresentationStyle = VfxPresentationStyle.Healing,
                        TargetType = AbilityTargetType.Player,
                        DurationType = AbilityDurationType.Continuous,
                        Duration = DemoConstants.AbilityAuthoring.HealingDurationSeconds
                    },
                    new SoundComponentData { SoundClip = AudioClipRef(SfxDir + "/SFX_Healing.wav", ThirdPartySfxHealing) },
                    new AnimationComponentData { CastClip = AnimationClipRef(AnimCastDir + "/Cast_Heal.anim") },
                    new StatusEffectComponentData
                    {
                        EffectType = StatusEffectType.Healing,
                        TargetType = AbilityTargetType.Player,
                        Duration = DemoConstants.AbilityAuthoring.HealingDurationSeconds,
                        Value = DemoConstants.AbilityAuthoring.HealingValue
                    }
                });

            CreateOrUpdateAbilityConfig(
                ConfigDir + "/DefencedAttack.asset",
                DemoConstants.AbilityIds.DefencedAttack,
                "Defenced Attack",
                4,
                AbilityTargetType.Enemy,
                AbilityRangeType.Melee,
                DemoConstants.AbilityAuthoring.DefencedAttackRange,
                new IAbilityComponentData[]
                {
                    new LockInputComponentData(),
                    new AnimationComponentData
                    {
                        CastClip = AnimationClipRef(AnimCastDir + "/Cast_DefencedAttack.anim"),
                        WaitUntilEnd = true
                    },
                    new SoundComponentData { SoundClip = AudioClipRef(SfxDir + "/SFX_DefencedAttack.wav", ThirdPartySfxDefAttack) },
                    new VisualFxComponentData { VfxPrefab = GameObjectRef(VfxPrefabDir + "/VFX_DefencedAttack.prefab", ThirdPartyVfxDefAttack) },
                    new DamageComponentData
                    {
                        TotalValue = DemoConstants.AbilityAuthoring.DefencedAttackDamage,
                        TickValue = DemoConstants.AbilityAuthoring.DefencedAttackDamage
                    },
                    new StatusEffectComponentData
                    {
                        EffectType = StatusEffectType.Freezing,
                        Duration = DemoConstants.AbilityAuthoring.FreezingDurationSeconds,
                        Value = DemoConstants.AbilityAuthoring.FreezingValue,
                        AdditionalVfx = GameObjectRef(VfxPrefabDir + "/VFX_Freezing.prefab", ThirdPartyVfxFreezing)
                    },
                    new StatusEffectComponentData
                    {
                        EffectType = StatusEffectType.Bleeding,
                        Duration = DemoConstants.AbilityAuthoring.DefencedAttackBleedDurationSeconds,
                        Value = DemoConstants.AbilityAuthoring.DefencedAttackBleedValue,
                        AdditionalVfx = GameObjectRef(VfxPrefabDir + "/VFX_Bleeding.prefab", ThirdPartyVfxBleeding)
                    }
                });

            AssetDatabase.SaveAssets();
            Debug.Log("[AvantajPrim] Default ability configs created/updated.");
        }

        public static void ReplayLastCast()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[AvantajPrim] Replay Last Cast requires Play Mode.");
                return;
            }

            AbilityActivationReplayService replay = AbilityEditorPlayAccess.Replay;
            if (replay == null)
            {
                Debug.LogWarning("[AvantajPrim] AbilityActivationReplayService is not available.");
                return;
            }

            if (replay.Log.Casts.Count == 0 && replay.Log.Frames.Count == 0)
            {
                Debug.LogWarning("[AvantajPrim] Ability activation log is empty — cast an ability first.");
                return;
            }

            replay.ReplayLastAsync().Forget();
            Debug.Log("[AvantajPrim] Replaying last cast.");
        }

        private static void CreateOrUpdateAbilityConfig(string assetPath, string abilityId, string displayName,
            int hotkeySlot, AbilityTargetType targetType, AbilityRangeType rangeType, float range,
            IReadOnlyList<IAbilityComponentData> components)
        {
            AbilityConfigAsset asset = AssetDatabase.LoadAssetAtPath<AbilityConfigAsset>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AbilityConfigAsset>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_abilityId").stringValue = abilityId;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_hotkeySlot").intValue = hotkeySlot;
            serialized.FindProperty("_hotkeyKey").enumValueIndex = (int)KeyCode.None;
            serialized.FindProperty("_targetType").enumValueIndex = (int)targetType;
            serialized.FindProperty("_rangeType").enumValueIndex = (int)rangeType;
            serialized.FindProperty("_range").floatValue = range;

            SerializedProperty componentsProp = serialized.FindProperty("_components");
            componentsProp.ClearArray();
            for (int i = 0; i < components.Count; i++)
            {
                componentsProp.InsertArrayElementAtIndex(i);
                componentsProp.GetArrayElementAtIndex(i).managedReferenceValue = components[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void RegisterAddressablesEntries()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("[AvantajPrim] AddressableAssetSettings could not be created.");
                return;
            }

            AddressableAssetGroup abilitiesGroup = settings.FindGroup(DemoConstants.AddressableGroups.Abilities)
                                                   ?? settings.CreateGroup(DemoConstants.AddressableGroups.Abilities, false, false, true, null,
                                                       typeof(BundledAssetGroupSchema));
            AddressableAssetGroup demoGroup = settings.FindGroup(DemoConstants.AddressableGroups.Demo)
                                              ?? settings.CreateGroup(DemoConstants.AddressableGroups.Demo, false, false, true, null,
                                                  typeof(BundledAssetGroupSchema));
            AddressableAssetGroup vfxGroup = settings.FindGroup(DemoConstants.AddressableGroups.Vfx)
                                             ?? settings.CreateGroup(DemoConstants.AddressableGroups.Vfx, false, false, true, null,
                                                 typeof(BundledAssetGroupSchema));
            AddressableAssetGroup audioGroup = settings.FindGroup(DemoConstants.AddressableGroups.Audio)
                                               ?? settings.CreateGroup(DemoConstants.AddressableGroups.Audio, false, false, true, null,
                                                   typeof(BundledAssetGroupSchema));
            AddressableAssetGroup animGroup = settings.FindGroup(DemoConstants.AddressableGroups.Animations)
                                              ?? settings.CreateGroup(DemoConstants.AddressableGroups.Animations, false, false, true, null,
                                                  typeof(BundledAssetGroupSchema));

            PruneStaleAddressableEntries(settings);

            void Ensure(string path, string address, AddressableAssetGroup group, string fallbackPath = null)
            {
                string resolved = ResolveAssetPath(path, fallbackPath);
                if (string.IsNullOrEmpty(resolved))
                {
                    Debug.LogError($"[AvantajPrim] Missing asset for Addressables: {path}");
                    return;
                }

                string guid = AssetDatabase.AssetPathToGUID(resolved);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogError($"[AvantajPrim] Missing asset for Addressables: {resolved}");
                    return;
                }

                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.SetAddress(address, false);

                if (group == abilitiesGroup)
                    entry.SetLabel(AbilityAddressableDiscovery.AbilityLabel, true, true);
            }

            Ensure(ConfigDir + "/Dash.asset", BootstrapAddresses.Dash, abilitiesGroup);
            Ensure(ConfigDir + "/Firewall.asset", BootstrapAddresses.Firewall, abilitiesGroup);
            Ensure(ConfigDir + "/Healing.asset", BootstrapAddresses.Healing, abilitiesGroup);
            Ensure(ConfigDir + "/DefencedAttack.asset", BootstrapAddresses.DefencedAttack, abilitiesGroup);

            Ensure(DemoPrefabDir + "/DemoArena.prefab", BootstrapAddresses.Arena, demoGroup, ThirdPartyGround);
            Ensure(DemoPrefabDir + "/DemoPlayer.prefab", BootstrapAddresses.Player, demoGroup, ThirdPartyWizard);
            Ensure(DemoPrefabDir + "/DemoEnemy.prefab", BootstrapAddresses.Enemy, demoGroup, ThirdPartyWizardEnemy);

            Ensure(ThirdPartyTree, BootstrapAddresses.NatureTree01, demoGroup);
            Ensure(ThirdPartyTree02, BootstrapAddresses.NatureTree02, demoGroup);
            Ensure(ThirdPartyTree03, BootstrapAddresses.NatureTree03, demoGroup);
            Ensure(ThirdPartyTree04, BootstrapAddresses.NatureTree04, demoGroup);
            Ensure(ThirdPartyTree05, BootstrapAddresses.NatureTree05, demoGroup);
            Ensure(ThirdPartyBush01, BootstrapAddresses.NatureBush01, demoGroup);
            Ensure(ThirdPartyBush02, BootstrapAddresses.NatureBush02, demoGroup);

            Ensure(VfxPrefabDir + "/VFX_Dash.prefab", BootstrapAddresses.VfxDash, vfxGroup, ThirdPartyVfxDash);
            Ensure(VfxPrefabDir + "/VFX_Firewall.prefab", BootstrapAddresses.VfxFirewall, vfxGroup, ThirdPartyVfxFirewall);
            Ensure(VfxPrefabDir + "/VFX_Healing.prefab", BootstrapAddresses.VfxHealing, vfxGroup, ThirdPartyVfxHealing);
            Ensure(VfxPrefabDir + "/VFX_DefencedAttack.prefab", BootstrapAddresses.VfxDefencedAttack, vfxGroup,
                ThirdPartyVfxDefAttack);
            Ensure(VfxPrefabDir + "/VFX_Freezing.prefab", BootstrapAddresses.VfxFreezing, vfxGroup, ThirdPartyVfxFreezing);
            Ensure(VfxPrefabDir + "/VFX_Bleeding.prefab", BootstrapAddresses.VfxBleeding, vfxGroup, ThirdPartyVfxBleeding);

            Ensure(SfxDir + "/SFX_Dash.wav", BootstrapAddresses.SfxDash, audioGroup, ThirdPartySfxDash);
            Ensure(SfxDir + "/SFX_Firewall.wav", BootstrapAddresses.SfxFirewall, audioGroup, ThirdPartySfxFirewall);
            Ensure(SfxDir + "/SFX_Healing.wav", BootstrapAddresses.SfxHealing, audioGroup, ThirdPartySfxHealing);
            Ensure(SfxDir + "/SFX_DefencedAttack.wav", BootstrapAddresses.SfxDefencedAttack, audioGroup,
                ThirdPartySfxDefAttack);

            Ensure(AnimCastDir + "/Cast_Dash.anim", BootstrapAddresses.AnimDash, animGroup);
            Ensure(AnimCastDir + "/Cast_Firewall.anim", BootstrapAddresses.AnimFirewall, animGroup);
            Ensure(AnimCastDir + "/Cast_Heal.anim", BootstrapAddresses.AnimHeal, animGroup);
            Ensure(AnimCastDir + "/Cast_DefencedAttack.anim", BootstrapAddresses.AnimDefencedAttack, animGroup);

            EditorUtility.SetDirty(settings);
        }

        private static void CreateDemoPrefabs()
        {
            CreateDemoArenaPrefabIfMissing(DemoPrefabDir + "/DemoArena.prefab", ThirdPartyGround);
            CreateDemoEntityPrefabIfMissing(DemoPrefabDir + "/DemoPlayer.prefab", "DemoPlayer", ThirdPartyWizard,
                PlayerAnimatorController, new Color(0.2f, 0.85f, 0.95f));
            CreateDemoEntityPrefabIfMissing(DemoPrefabDir + "/DemoEnemy.prefab", "DemoEnemy", ThirdPartyWizardEnemy,
                EnemyAnimatorController, new Color(0.95f, 0.25f, 0.2f));
            RefreshDemoEntityPrefabs();
        }

        private static void RefreshDemoEntityPrefabs()
        {
            RefreshDemoEntityPrefab(DemoPrefabDir + "/DemoPlayer.prefab", PlayerAnimatorController);
            RefreshDemoEntityPrefab(DemoPrefabDir + "/DemoEnemy.prefab", EnemyAnimatorController);
        }

        private static void RefreshDemoEntityPrefab(string prefabPath, string animatorControllerPath)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                Debug.LogWarning($"[AvantajPrim] Demo prefab not found: {prefabPath}");
                return;
            }

            try
            {
                WireEntityPrefab(prefabRoot, animatorControllerPath);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log($"[AvantajPrim] Refreshed demo entity prefab: {prefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void CreateDemoArenaPrefabIfMissing(string prefabPath, string sourcePrefabPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                return;

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
            GameObject go;
            if (source != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(source);
                go.name = "DemoArena";
                go.transform.localScale = new Vector3(2f, 1f, 2f);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                go.name = "DemoArena";
                go.transform.localScale = new Vector3(2f, 1f, 2f);
            }

            AddArenaDecor(go);
            SavePrefab(go, prefabPath);
        }

        private static void RefreshDemoArenaDecor()
        {
            string prefabPath = DemoPrefabDir + "/DemoArena.prefab";
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                Debug.LogWarning($"[AvantajPrim] Demo arena prefab not found: {prefabPath}");
                return;
            }

            try
            {
                RemoveArenaDecorChildren(prefabRoot.transform);
                AddArenaDecor(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log($"[AvantajPrim] Refreshed demo arena decor: {prefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void RemoveArenaDecorChildren(Transform arenaRoot)
        {
            for (int i = arenaRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = arenaRoot.GetChild(i);
                string name = child.name;
                if (name.StartsWith(DemoConstants.DecorNaming.TreePrefix, StringComparison.Ordinal) ||
                    name.StartsWith(DemoConstants.DecorNaming.BushPrefix, StringComparison.Ordinal) ||
                    name.StartsWith(DemoConstants.DecorNaming.RockPrefix, StringComparison.Ordinal) ||
                    name.StartsWith(DemoConstants.DecorNaming.StumpPrefix, StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void AddArenaDecor(GameObject arenaRoot)
        {
            PlaceDecorIfAvailable(arenaRoot.transform, ThirdPartyTree, new Vector3(-6f, 0f, 4f), Quaternion.Euler(0f, 25f, 0f));
            PlaceDecorIfAvailable(arenaRoot.transform, ThirdPartyTree02, new Vector3(-8f, 0f, -3f), Quaternion.Euler(0f, 20f, 0f), 1.1f);
            PlaceDecorIfAvailable(arenaRoot.transform, ThirdPartyTree03, new Vector3(7f, 0f, 6f), Quaternion.Euler(0f, -35f, 0f), 0.95f);
            PlaceDecorIfAvailable(arenaRoot.transform, ThirdPartyTree04, new Vector3(-5f, 0f, 7f), Quaternion.Euler(0f, 55f, 0f), 1.15f);
            PlaceDecorIfAvailable(arenaRoot.transform, ThirdPartyTree05, new Vector3(8f, 0f, 1f), Quaternion.Euler(0f, -70f, 0f), 1.05f);
            PlaceDecorIfAvailable(arenaRoot.transform, ThirdPartyBush01, new Vector3(-3f, 0f, -6f), Quaternion.Euler(0f, 10f, 0f), 1.2f);
            PlaceDecorIfAvailable(arenaRoot.transform, ThirdPartyBush02, new Vector3(3f, 0f, -7f), Quaternion.Euler(0f, -15f, 0f), 1.1f);
            PlaceDecorIfAvailable(arenaRoot.transform, ThirdPartyRock, new Vector3(6f, 0f, -4f), Quaternion.identity);
            PlaceDecorIfAvailable(arenaRoot.transform, ThirdPartyTree, new Vector3(5f, 0f, 5f), Quaternion.Euler(0f, -40f, 0f));
        }

        private static void PlaceDecorIfAvailable(Transform parent, string prefabPath, Vector3 localPosition, Quaternion localRotation,
            float scale = 1f)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (source == null)
                return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            if (Mathf.Abs(scale - 1f) > 0.001f)
                instance.transform.localScale *= scale;
        }

        private static void CreateDemoEntityPrefabIfMissing(string prefabPath, string rootName, string sourcePrefabPath,
            string animatorControllerPath, Color color)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                return;

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
            GameObject go;
            if (source != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(source);
                go.name = rootName;
                if (go.GetComponent<EntityView>() == null)
                    go.AddComponent<EntityView>();
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = rootName;
                go.AddComponent<EntityView>();
            }

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    var material = new Material(shader) { color = color };
                    renderer.sharedMaterial = material;
                }
            }

            WireEntityPrefab(go, animatorControllerPath);
            SavePrefab(go, prefabPath);
        }

        private static void WireEntityPrefab(GameObject go, string animatorControllerPath)
        {
            if (go.GetComponent<EntityView>() == null)
                go.AddComponent<EntityView>();

            if (go.GetComponentInChildren<CapsuleCollider>() == null)
            {
                CapsuleCollider collider = go.AddComponent<CapsuleCollider>();
                collider.height = 2f;
                collider.radius = 0.45f;
                collider.center = new Vector3(0f, 1f, 0f);
            }

            if (go.transform.Find("VfxSpawnPoint") == null)
            {
                var spawn = new GameObject("VfxSpawnPoint");
                spawn.transform.SetParent(go.transform, false);
                spawn.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            }

            Animator animator = go.GetComponentInChildren<Animator>();
            if (animator == null)
                return;

            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorControllerPath);
            if (controller != null)
                animator.runtimeAnimatorController = controller;
        }

        private static void CreateVfxPrefabs()
        {
            SanitizeVfxWrapper(ThirdPartyVfxDash, VfxPrefabDir + "/VFX_Dash.prefab", "VFX_Dash",
                new Color(0.3f, 0.8f, 1f, 0.85f));
            SanitizeVfxWrapper(ThirdPartyVfxFirewall, VfxPrefabDir + "/VFX_Firewall.prefab", "VFX_Firewall",
                new Color(1f, 0.45f, 0.1f, 0.9f));
            SanitizeVfxWrapper(ThirdPartyVfxHealing, VfxPrefabDir + "/VFX_Healing.prefab", "VFX_Healing",
                new Color(0.3f, 1f, 0.45f, 0.9f));
            SanitizeVfxWrapper(ThirdPartyVfxDefAttack, VfxPrefabDir + "/VFX_DefencedAttack.prefab", "VFX_DefencedAttack",
                new Color(0.6f, 0.85f, 1f, 0.9f));
            SanitizeVfxWrapper(ThirdPartyVfxFreezing, VfxPrefabDir + "/VFX_Freezing.prefab", "VFX_Freezing",
                new Color(0.5f, 0.9f, 1f, 0.9f));
            SanitizeVfxWrapper(ThirdPartyVfxBleeding, VfxPrefabDir + "/VFX_Bleeding.prefab", "VFX_Bleeding",
                new Color(0.95f, 0.15f, 0.1f, 0.9f));
        }

        private static void SanitizeVfxWrapper(string sourcePath, string wrapperPath, string rootName, Color fallbackColor)
        {
            GameObject instance = null;
            try
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (source != null)
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                    instance.name = rootName;
                    RemoveMissingScriptsRecursive(instance);
                }
                else if (IsValidAssetPath(wrapperPath))
                {
                    return;
                }
                else
                {
                    instance = CreateVfxSpherePlaceholder(rootName, fallbackColor);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(wrapperPath) ?? VfxPrefabDir);
                if (!PrefabUtility.SaveAsPrefabAsset(instance, wrapperPath))
                    Debug.LogError($"[AvantajPrim] Failed to save sanitized VFX wrapper: {wrapperPath}");
            }
            finally
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static GameObject CreateVfxSpherePlaceholder(string rootName, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = rootName;
            go.transform.localScale = Vector3.one * DemoConstants.Presentation.VfxWrapperScale;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                    renderer.sharedMaterial = new Material(shader) { color = color };
            }

            return go;
        }

        private static void RemoveMissingScriptsRecursive(GameObject root)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
        }

        private static void EnsureAddressablesProfile(AddressableAssetSettings settings)
        {
            if (settings == null)
                return;

            AddressableAssetProfileSettings profiles = settings.profileSettings;
            string profileId = settings.activeProfileId;
            if (string.IsNullOrEmpty(profileId))
                profileId = profiles.GetProfileId("Default");

            AddressableAssetProfileSettings.ProfileIdData remoteLoad = profiles.GetProfileDataByName(AddressableAssetSettings.kRemoteLoadPath);
            if (remoteLoad != null)
            {
                string remoteLoadValue = profiles.GetValueById(profileId, remoteLoad.Id);
                if (string.IsNullOrEmpty(remoteLoadValue) || remoteLoadValue == "<undefined>")
                {
                    profiles.SetValue(profileId, remoteLoad.Id,
                        "http://[PrivateIpAddress]:[HostingServicePort]");
                }
            }

            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            EditorUtility.SetDirty(settings);
        }

        private static void CreateSfxPlaceholders()
        {
            CopyAudioIfMissing(SfxDir + "/SFX_Dash.wav", ThirdPartySfxDash);
            CopyAudioIfMissing(SfxDir + "/SFX_Firewall.wav", ThirdPartySfxFirewall);
            CopyAudioIfMissing(SfxDir + "/SFX_Healing.wav", ThirdPartySfxHealing);
            CopyAudioIfMissing(SfxDir + "/SFX_DefencedAttack.wav", ThirdPartySfxDefAttack);
        }

        private static void CopyAudioIfMissing(string destPath, string sourcePath)
        {
            if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
            {
                bool shouldCopy = !File.Exists(destPath);
                if (!shouldCopy && File.Exists(destPath))
                {
                    var destInfo = new FileInfo(destPath);
                    var sourceInfo = new FileInfo(sourcePath);
                    shouldCopy = destInfo.Length < 16_384 && sourceInfo.Length > destInfo.Length;
                }

                if (shouldCopy)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? SfxDir);
                    File.Copy(sourcePath, destPath, true);
                    AssetDatabase.ImportAsset(destPath);
                }

                return;
            }

            CreateSilentWavIfMissing(destPath);
        }

        private static void CreateSilentWavIfMissing(string assetPath)
        {
            if (File.Exists(assetPath))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? SfxDir);
            WriteSilentWav(assetPath, sampleCount: 2205, sampleRate: 22050);
            AssetDatabase.ImportAsset(assetPath);
        }

        private static void WriteSilentWav(string path, int sampleCount, int sampleRate)
        {
            const short channels = 1;
            const short bitsPerSample = 16;
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            short blockAlign = (short)(channels * bitsPerSample / 8);
            int dataSize = sampleCount * blockAlign;
            int fileSize = 36 + dataSize;

            using FileStream stream = File.Create(path);
            using var writer = new BinaryWriter(stream);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(fileSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);
            for (int i = 0; i < dataSize; i++)
                writer.Write((byte)0);
        }

        private static void PruneStaleAddressableEntries(AddressableAssetSettings settings)
        {
            var removeGuids = new HashSet<string>();

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null || group.ReadOnly)
                    continue;

                var byAddress = new Dictionary<string, List<AddressableAssetEntry>>();

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry == null)
                        continue;

                    string path = AssetDatabase.GUIDToAssetPath(entry.guid);
                    if (string.IsNullOrEmpty(path) || AssetDatabase.LoadMainAssetAtPath(path) == null)
                    {
                        removeGuids.Add(entry.guid);
                        Debug.LogWarning($"[AvantajPrim] Removing stale Addressables entry '{entry.address}' ({entry.guid}).");
                        continue;
                    }

                    if (!byAddress.TryGetValue(entry.address, out List<AddressableAssetEntry> list))
                    {
                        list = new List<AddressableAssetEntry>();
                        byAddress[entry.address] = list;
                    }

                    list.Add(entry);
                }

                foreach (List<AddressableAssetEntry> duplicates in byAddress.Values.Where(list => list.Count > 1))
                {
                    AddressableAssetEntry keep = duplicates
                        .OrderByDescending(e => AssetDatabase.GUIDToAssetPath(e.guid).Contains("/_Project/"))
                        .ThenByDescending(e => AssetDatabase.GUIDToAssetPath(e.guid).Contains("ThridPartyAssets"))
                        .ThenByDescending(e => IsValidAssetPath(AssetDatabase.GUIDToAssetPath(e.guid)))
                        .First();

                    foreach (AddressableAssetEntry entry in duplicates)
                    {
                        if (entry.guid == keep.guid)
                            continue;

                        removeGuids.Add(entry.guid);
                        Debug.LogWarning(
                            $"[AvantajPrim] Removing duplicate Addressables entry '{entry.address}' ({entry.guid}).");
                    }
                }
            }

            foreach (string guid in removeGuids)
                settings.RemoveAssetEntry(guid, false);
        }

        private static void CreateOrUpdateDemoAddressableCatalog()
        {
            DemoAddressableCatalog catalog = AssetDatabase.LoadAssetAtPath<DemoAddressableCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DemoAddressableCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Arena = GameObjectRef(DemoPrefabDir + "/DemoArena.prefab", ThirdPartyGround);
            catalog.Player = GameObjectRef(DemoPrefabDir + "/DemoPlayer.prefab", ThirdPartyWizard);
            catalog.Enemy = GameObjectRef(DemoPrefabDir + "/DemoEnemy.prefab", ThirdPartyWizardEnemy);
            catalog.ExtraDecor = new[]
            {
                new DemoAddressableCatalog.DecorEntry
                {
                    Prefab = GameObjectRef(ThirdPartyTree02),
                    LocalPosition = new Vector3(-8f, 0f, -3f),
                    RotationY = 20f,
                    Scale = 1.1f
                },
                new DemoAddressableCatalog.DecorEntry
                {
                    Prefab = GameObjectRef(ThirdPartyTree03),
                    LocalPosition = new Vector3(7f, 0f, 6f),
                    RotationY = -35f,
                    Scale = 0.95f
                },
                new DemoAddressableCatalog.DecorEntry
                {
                    Prefab = GameObjectRef(ThirdPartyTree04),
                    LocalPosition = new Vector3(-5f, 0f, 7f),
                    RotationY = 55f,
                    Scale = 1.15f
                },
                new DemoAddressableCatalog.DecorEntry
                {
                    Prefab = GameObjectRef(ThirdPartyTree05),
                    LocalPosition = new Vector3(8f, 0f, 1f),
                    RotationY = -70f,
                    Scale = 1.05f
                },
                new DemoAddressableCatalog.DecorEntry
                {
                    Prefab = GameObjectRef(ThirdPartyBush01),
                    LocalPosition = new Vector3(-3f, 0f, -6f),
                    RotationY = 10f,
                    Scale = 1.2f
                },
                new DemoAddressableCatalog.DecorEntry
                {
                    Prefab = GameObjectRef(ThirdPartyBush02),
                    LocalPosition = new Vector3(3f, 0f, -7f),
                    RotationY = -15f,
                    Scale = 1.1f
                }
            };
            catalog.SoundClips = new[]
            {
                AudioClipRef(SfxDir + "/SFX_Dash.wav", ThirdPartySfxDash),
                AudioClipRef(SfxDir + "/SFX_Firewall.wav", ThirdPartySfxFirewall),
                AudioClipRef(SfxDir + "/SFX_Healing.wav", ThirdPartySfxHealing),
                AudioClipRef(SfxDir + "/SFX_DefencedAttack.wav", ThirdPartySfxDefAttack)
            };
            catalog.VfxPrefabs = new[]
            {
                GameObjectRef(VfxPrefabDir + "/VFX_Dash.prefab", ThirdPartyVfxDash),
                GameObjectRef(VfxPrefabDir + "/VFX_Firewall.prefab", ThirdPartyVfxFirewall),
                GameObjectRef(VfxPrefabDir + "/VFX_Healing.prefab", ThirdPartyVfxHealing),
                GameObjectRef(VfxPrefabDir + "/VFX_DefencedAttack.prefab", ThirdPartyVfxDefAttack),
                GameObjectRef(VfxPrefabDir + "/VFX_Freezing.prefab", ThirdPartyVfxFreezing),
                GameObjectRef(VfxPrefabDir + "/VFX_Bleeding.prefab", ThirdPartyVfxBleeding)
            };
            catalog.CastAnimationClips = new[]
            {
                AnimationClipRef(AnimCastDir + "/Cast_Dash.anim"),
                AnimationClipRef(AnimCastDir + "/Cast_Firewall.anim"),
                AnimationClipRef(AnimCastDir + "/Cast_Heal.anim"),
                AnimationClipRef(AnimCastDir + "/Cast_DefencedAttack.anim")
            };

            EditorUtility.SetDirty(catalog);
        }

        private static AssetReferenceGameObject GameObjectRef(string assetPath, string fallbackPath = null)
        {
            string guid = AssetPathToGuid(assetPath, fallbackPath);
            return string.IsNullOrEmpty(guid) ? null : new AssetReferenceGameObject(guid);
        }

        private static AssetReferenceT<AudioClip> AudioClipRef(string assetPath, string fallbackPath = null)
        {
            string guid = AssetPathToGuid(assetPath, fallbackPath);
            return string.IsNullOrEmpty(guid) ? null : new AssetReferenceT<AudioClip>(guid);
        }

        private static AssetReferenceT<AnimationClip> AnimationClipRef(string assetPath, string fallbackPath = null)
        {
            string guid = AssetPathToGuid(assetPath, fallbackPath);
            return string.IsNullOrEmpty(guid) ? null : new AssetReferenceT<AnimationClip>(guid);
        }

        private static void CreateCastAnimationClips()
        {
            CreateCastAnimationClipIfMissing(
                ThirdPartyAnimRoot + "/BattleRunForward.fbx",
                "BattleRunForward",
                AnimCastDir + "/Cast_Dash.anim");
            CreateCastAnimationClipIfMissing(
                ThirdPartyAnimRoot + "/Attack02Start.fbx",
                "Attack02Start",
                AnimCastDir + "/Cast_Firewall.anim");
            CreateCastAnimationClipIfMissing(
                ThirdPartyAnimRoot + "/PotionDrink.fbx",
                "PotionDrink",
                AnimCastDir + "/Cast_Heal.anim");
            CreateCastAnimationClipIfMissing(
                ThirdPartyAnimRoot + "/DefendHit.fbx",
                "DefendHit",
                AnimCastDir + "/Cast_DefencedAttack.anim");
        }

        private static void CreateCastAnimationClipIfMissing(string sourceFbxPath, string clipName, string destAnimPath)
        {
            if (IsValidAssetPath(destAnimPath))
                return;

            AnimationClip sourceClip = LoadFbxAnimationClip(sourceFbxPath, clipName);
            if (sourceClip == null)
            {
                Debug.LogWarning($"[AvantajPrim] Could not extract animation '{clipName}' from {sourceFbxPath}.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destAnimPath) ?? AnimCastDir);
            AnimationClip copy = UnityEngine.Object.Instantiate(sourceClip);
            copy.name = Path.GetFileNameWithoutExtension(destAnimPath);
            AssetDatabase.CreateAsset(copy, destAnimPath);
            AssetDatabase.ImportAsset(destAnimPath);
            Debug.Log($"[AvantajPrim] Created cast animation clip: {destAnimPath}");
        }

        private static AnimationClip LoadFbxAnimationClip(string fbxPath, string clipName)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (asset is AnimationClip clip && clip.name == clipName)
                    return clip;
            }

            return null;
        }

        private static string AssetPathToGuid(string assetPath, string fallbackPath = null)
        {
            string resolved = ResolveAssetPath(assetPath, fallbackPath);
            return string.IsNullOrEmpty(resolved) ? null : AssetDatabase.AssetPathToGUID(resolved);
        }

        private static string ResolveAssetPath(string primaryPath, string fallbackPath)
        {
            if (IsValidAssetPath(primaryPath))
                return primaryPath;
            if (IsValidAssetPath(fallbackPath))
                return fallbackPath;
            return primaryPath;
        }

        private static bool IsValidAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
                return false;

            return AssetDatabase.LoadMainAssetAtPath(assetPath) != null;
        }

        private static void RemoveBrokenPrefabIfPresent(string prefabPath)
        {
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(prefabPath)))
                return;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                return;

            if (AssetDatabase.DeleteAsset(prefabPath))
                Debug.LogWarning($"[AvantajPrim] Removed broken prefab wrapper: {prefabPath}");
        }

        private static void SavePrefab(GameObject go, string prefabPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath) ?? PrefabDir);

            if (!PrefabUtility.SaveAsPrefabAsset(go, prefabPath))
            {
                Debug.LogError($"[AvantajPrim] Failed to save prefab: {prefabPath}");
                UnityEngine.Object.DestroyImmediate(go);
                return;
            }

            UnityEngine.Object.DestroyImmediate(go);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            string parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            string name = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
