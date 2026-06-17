using System;
using AvantajPrim.Abilities.Domain;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AvantajPrim.Abilities.Data
{
    [Serializable]
    public sealed class VisualFxComponentData : IAbilityComponentData
    {
        public AbilityPlayTimeType PlayTimeType = AbilityPlayTimeType.OnStart;
        public float DelaySeconds;
        public AssetReferenceGameObject VfxPrefab;
        public VfxPresentationStyle PresentationStyle = VfxPresentationStyle.Default;
        public AbilityTargetType TargetType = AbilityTargetType.Enemy;
        public AbilityDurationType DurationType = AbilityDurationType.Instant;
        public float Duration = 1f;
        public float OffsetX;
        public float OffsetY;
        public float OffsetZ;
        [SerializeField] private bool _isCasterScoped;

        public string ResolveVfxKey() => AddressableAssetRefUtility.GetRuntimeKey(VfxPrefab);

        AbilityPlayTimeType IAbilityComponentData.PlayTimeType => PlayTimeType;
        float IAbilityComponentData.DelaySeconds => DelaySeconds;
        bool IAbilityComponentData.IsCasterScoped => _isCasterScoped;
    }
}
