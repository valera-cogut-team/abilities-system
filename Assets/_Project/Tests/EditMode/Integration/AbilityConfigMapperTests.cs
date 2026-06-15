using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Tests.Shared;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AvantajPrim.Tests.EditMode.Integration
{
    [TestFixture]
    public sealed class AbilityConfigMapperTests
    {
        [Test]
        public void ToDefinition_MapsAbilityIdAndComponents()
        {
            AbilityConfigAsset asset = CreateAsset("dash", new DamageComponentData
            {
                PlayTimeType = AbilityPlayTimeType.OnStart,
                TotalValue = 25f
            }, TestAddressableRefs.CreateSoundComponent(playTime: AbilityPlayTimeType.OnEnd));

            AbilityDefinition definition = AbilityConfigMapper.ToDefinition(asset);

            Assert.AreEqual("dash", definition.Id.Value);
            Assert.AreEqual(2, definition.Components.Count);
            Assert.IsInstanceOf<DamageComponentData>(definition.Components[0]);
            Assert.AreEqual(25f, ((DamageComponentData)definition.Components[0]).TotalValue);
            Assert.IsInstanceOf<SoundComponentData>(definition.Components[1]);
        }

        [Test]
        public void ToDefinition_MapsMetadataFromAsset()
        {
            AbilityConfigAsset asset = CreateAsset("firewall");
            SetPrivateField(asset, "_hotkeySlot", 2);
            SetPrivateField(asset, "_targetType", AbilityTargetType.Enemy);
            SetPrivateField(asset, "_rangeType", AbilityRangeType.Ranged);
            SetPrivateField(asset, "_range", 12f);

            AbilityDefinition definition = AbilityConfigMapper.ToDefinition(asset);

            Assert.AreEqual(2, definition.HotkeySlot);
            Assert.AreEqual(AbilityTargetType.Enemy, definition.TargetType);
            Assert.AreEqual(AbilityRangeType.Ranged, definition.RangeType);
            Assert.AreEqual(12f, definition.Range);
        }

        [Test]
        public void ToDefinition_MapsHotkeyKeyCode()
        {
            AbilityConfigAsset asset = CreateAsset("dash");
            SetPrivateField(asset, "_hotkeyKey", KeyCode.R);

            AbilityDefinition definition = AbilityConfigMapper.ToDefinition(asset);

            Assert.AreEqual((int)KeyCode.R, definition.HotkeyKeyCode);
        }

        [Test]
        public void ToDefinition_PreservesEmptyComponentList()
        {
            AbilityConfigAsset asset = CreateAsset("empty");

            AbilityDefinition definition = AbilityConfigMapper.ToDefinition(asset);

            Assert.AreEqual("empty", definition.Id.Value);
            Assert.AreEqual(0, definition.Components.Count);
        }

        [TestCase("Dash.asset", "dash")]
        [TestCase("Firewall.asset", "firewall")]
        [TestCase("Healing.asset", "healing")]
        [TestCase("DefencedAttack.asset", "defenced_attack")]
        public void ToDefinition_MapsShippedAbilityConfigs(string assetFileName, string expectedAbilityId)
        {
            string path = Path.Combine("Assets/_Project/Configs/Abilities", assetFileName);
            AbilityConfigAsset asset = AssetDatabase.LoadAssetAtPath<AbilityConfigAsset>(path);
            Assert.IsNotNull(asset, $"Missing config at {path}");

            AbilityDefinition definition = AbilityConfigMapper.ToDefinition(asset);

            Assert.AreEqual(expectedAbilityId, definition.Id.Value);
            Assert.Greater(definition.Components.Count, 0);
        }

        [Test]
        public void ToDefinition_HealingAsset_UsesPlayerTargetType()
        {
            AbilityConfigAsset asset = AssetDatabase.LoadAssetAtPath<AbilityConfigAsset>(
                "Assets/_Project/Configs/Abilities/Healing.asset");
            Assert.IsNotNull(asset);

            AbilityDefinition definition = AbilityConfigMapper.ToDefinition(asset);

            Assert.AreEqual("healing", definition.Id.Value);
            Assert.AreEqual(AbilityTargetType.Player, definition.TargetType);
        }

        [Test]
        public void ToDefinition_DefencedAttack_IncludesFreezingAndBleedingStatusComponents()
        {
            AbilityConfigAsset asset = AssetDatabase.LoadAssetAtPath<AbilityConfigAsset>(
                "Assets/_Project/Configs/Abilities/DefencedAttack.asset");
            Assert.IsNotNull(asset);

            AbilityDefinition definition = AbilityConfigMapper.ToDefinition(asset);
            int freezingCount = 0;
            int bleedingCount = 0;
            foreach (IAbilityComponentData component in definition.Components)
            {
                if (component is not StatusEffectComponentData status)
                    continue;

                if (status.EffectType == StatusEffectType.Freezing)
                    freezingCount++;
                if (status.EffectType == StatusEffectType.Bleeding)
                    bleedingCount++;
            }

            Assert.AreEqual(1, freezingCount);
            Assert.AreEqual(1, bleedingCount);
        }

        private static AbilityConfigAsset CreateAsset(string abilityId, params IAbilityComponentData[] components)
        {
            AbilityConfigAsset asset = ScriptableObject.CreateInstance<AbilityConfigAsset>();
            SetPrivateField(asset, "_abilityId", abilityId);
            SetPrivateField(asset, "_components", new List<IAbilityComponentData>(components));
            return asset;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }
    }
}
