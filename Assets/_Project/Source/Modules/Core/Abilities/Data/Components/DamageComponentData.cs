using System;
using AvantajPrim.Abilities.Domain;
using UnityEngine;

namespace AvantajPrim.Abilities.Data
{
    [Serializable]
    public sealed class DamageComponentData : IAbilityComponentData
    {
        public AbilityPlayTimeType PlayTimeType = AbilityPlayTimeType.OnStart;
        public float DelaySeconds;

        public float TotalValue = AbilityConstants.ComponentDefaults.DefaultDamageValue;
        public float TickValue = AbilityConstants.ComponentDefaults.DefaultDamageValue;
        public float ApplicationDuration;
        public float TickInterval = 1f;
        [SerializeField] private bool _isCasterScoped;

        AbilityPlayTimeType IAbilityComponentData.PlayTimeType => PlayTimeType;
        float IAbilityComponentData.DelaySeconds => DelaySeconds;
        bool IAbilityComponentData.IsCasterScoped => _isCasterScoped;

        public bool IsGradual => ApplicationDuration > 0f && TickValue > 0f;
    }
}
