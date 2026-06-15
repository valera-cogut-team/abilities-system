using System;
using AvantajPrim.Abilities.Domain;
using UnityEngine.AddressableAssets;

namespace AvantajPrim.Abilities.Data
{
    [Serializable]
    public sealed class StatusEffectComponentData : IAbilityComponentData
    {
        public AbilityPlayTimeType PlayTimeType = AbilityPlayTimeType.OnStart;
        public float DelaySeconds;
        public StatusEffectType EffectType = StatusEffectType.Combustion;
        public AbilityTargetType TargetType = AbilityTargetType.Enemy;
        public AbilityDurationType DurationType = AbilityDurationType.Continuous;
        public float Duration = AbilityConstants.ComponentDefaults.DefaultStatusDurationSeconds;
        public float Value = AbilityConstants.ComponentDefaults.DefaultStatusValue;
        public float TotalValue;
        public float TickInterval = AbilityConstants.Execution.DefaultStatusTickIntervalSeconds;
        public AssetReferenceGameObject AdditionalVfx;

        public string ResolveAdditionalVfxKey() => AddressableAssetRefUtility.GetRuntimeKey(AdditionalVfx);

        AbilityPlayTimeType IAbilityComponentData.PlayTimeType => PlayTimeType;
        float IAbilityComponentData.DelaySeconds => DelaySeconds;
    }
}
