using System;
using AvantajPrim.Abilities.Domain;

namespace AvantajPrim.Abilities.Data
{
    [Serializable]
    public sealed class LockInputComponentData : IAbilityComponentData
    {
        public AbilityPlayTimeType PlayTimeType = AbilityPlayTimeType.OnStart;
        public float DelaySeconds;
        public bool BlockMovement = true;
        public bool BlockRotation;

        AbilityPlayTimeType IAbilityComponentData.PlayTimeType => PlayTimeType;
        float IAbilityComponentData.DelaySeconds => DelaySeconds;
    }
}
