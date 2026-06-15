using StateMachine.Facade;

namespace AvantajPrim.Abilities.Domain
{
    public sealed class DisplacementTransitionPayload
    {
        public float OffsetX;
        public float OffsetY;
        public float OffsetZ;
        public float Duration;
        public int CastLifecycleId;

        public DisplacementTransitionPayload(float offsetX, float offsetY, float offsetZ, float duration, int castLifecycleId = 0)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            OffsetZ = offsetZ;
            Duration = duration;
            CastLifecycleId = castLifecycleId;
        }
    }

    public sealed class StatusTransitionPayload : ITransitionDurationProvider
    {
        public StatusEffectType EffectType;
        public float Value;
        public float Duration;
        public float TickInterval;
        public float TotalValue;
        public int CastLifecycleId;

        public float DurationSeconds => Duration;

        public StatusTransitionPayload(
            StatusEffectType effectType,
            float value,
            float duration,
            float tickInterval = 1f,
            float totalValue = 0f,
            int castLifecycleId = 0)
        {
            EffectType = effectType;
            Value = value;
            Duration = duration;
            TickInterval = tickInterval;
            TotalValue = totalValue;
            CastLifecycleId = castLifecycleId;
        }
    }
}
