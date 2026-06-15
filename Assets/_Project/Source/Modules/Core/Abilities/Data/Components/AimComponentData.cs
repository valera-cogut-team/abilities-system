using System;
using AvantajPrim.Abilities.Domain;

namespace AvantajPrim.Abilities.Data
{
    [Serializable]
    public sealed class AimComponentData : IAbilityComponentData
    {
        public AbilityPlayTimeType PlayTimeType = AbilityPlayTimeType.OnStart;
        public float DelaySeconds;
        public AbilityTargetType TargetType = AbilityTargetType.Enemy;

        AbilityPlayTimeType IAbilityComponentData.PlayTimeType => PlayTimeType;
        float IAbilityComponentData.DelaySeconds => DelaySeconds;
    }
}
