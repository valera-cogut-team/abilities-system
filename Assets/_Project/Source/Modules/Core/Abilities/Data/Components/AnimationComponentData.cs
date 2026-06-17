using System;
using AvantajPrim.Abilities.Domain;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AvantajPrim.Abilities.Data
{
    [Serializable]
    public sealed class AnimationComponentData : IAbilityComponentData
    {
        public AbilityPlayTimeType PlayTimeType = AbilityPlayTimeType.OnStart;
        public float DelaySeconds;
        public AssetReferenceT<AnimationClip> CastClip;
        public bool WaitUntilEnd;
        [SerializeField] private bool _isCasterScoped = true;

        public string ResolveClipKey() => AddressableAssetRefUtility.GetRuntimeKey(CastClip);

        public string ResolveAnimationName() => AbilityAnimationRefUtility.ResolveAnimationName(CastClip);

        AbilityPlayTimeType IAbilityComponentData.PlayTimeType => PlayTimeType;
        float IAbilityComponentData.DelaySeconds => DelaySeconds;
        bool IAbilityComponentData.IsCasterScoped => _isCasterScoped;
    }
}
