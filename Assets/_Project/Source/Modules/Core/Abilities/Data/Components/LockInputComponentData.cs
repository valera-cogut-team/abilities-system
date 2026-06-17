using System;
using AvantajPrim.Abilities.Domain;
using UnityEngine;

namespace AvantajPrim.Abilities.Data
{
    [Serializable]
    public sealed class LockInputComponentData : IAbilityComponentData
    {
        public AbilityPlayTimeType PlayTimeType = AbilityPlayTimeType.OnStart;
        public float DelaySeconds;
        public bool BlockMovement = true;
        public bool BlockRotation;
        [SerializeField] private bool _isCasterScoped = true;

        AbilityPlayTimeType IAbilityComponentData.PlayTimeType => PlayTimeType;
        float IAbilityComponentData.DelaySeconds => DelaySeconds;
        bool IAbilityComponentData.IsCasterScoped => _isCasterScoped;
    }
}
