using System;
using AvantajPrim.Abilities.Domain;
using UnityEngine;

namespace AvantajPrim.Abilities.Data
{
    [Serializable]
    public sealed class AimComponentData : IAbilityComponentData
    {
        public AbilityPlayTimeType PlayTimeType = AbilityPlayTimeType.OnStart;
        public float DelaySeconds;
        public AbilityTargetType TargetType = AbilityTargetType.Enemy;
        [SerializeField] private bool _isCasterScoped = true;

        AbilityPlayTimeType IAbilityComponentData.PlayTimeType => PlayTimeType;
        float IAbilityComponentData.DelaySeconds => DelaySeconds;
        bool IAbilityComponentData.IsCasterScoped => _isCasterScoped;
    }
}
