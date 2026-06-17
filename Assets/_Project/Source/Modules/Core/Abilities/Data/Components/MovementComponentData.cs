using System;
using AvantajPrim.Abilities.Domain;
using UnityEngine;

namespace AvantajPrim.Abilities.Data
{
    [Serializable]
    public sealed class MovementComponentData : IAbilityComponentData
    {
        public AbilityPlayTimeType PlayTimeType = AbilityPlayTimeType.OnStart;
        public float DelaySeconds;
        public float OffsetX;
        public float OffsetY;
        public float OffsetZ;
        public float Duration = AbilityConstants.ComponentDefaults.DefaultMovementDurationSeconds;

        [SerializeField] private bool _isCasterScoped = true;

        AbilityPlayTimeType IAbilityComponentData.PlayTimeType => PlayTimeType;
        float IAbilityComponentData.DelaySeconds => DelaySeconds;
        bool IAbilityComponentData.IsCasterScoped => _isCasterScoped;
    }
}
