using System;
using AvantajPrim.Abilities.Domain;

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

        AbilityPlayTimeType IAbilityComponentData.PlayTimeType => PlayTimeType;
        float IAbilityComponentData.DelaySeconds => DelaySeconds;
    }
}
