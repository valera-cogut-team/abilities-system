namespace AvantajPrim.Abilities.Domain
{
    public readonly struct DamageRequestedEvent
    {
        public readonly EntityId SourceId;
        public readonly EntityId TargetId;
        public readonly float Value;
        public readonly float TotalValue;
        public readonly float TickValue;
        public readonly float ApplicationDuration;
        public readonly float TickInterval;
        public readonly int CastLifecycleId;

        public bool IsGradual => ApplicationDuration > 0f && TickValue > 0f;

        public DamageRequestedEvent(EntityId sourceId, EntityId targetId, float value, int castLifecycleId = 0)
        {
            SourceId = sourceId;
            TargetId = targetId;
            Value = value;
            TotalValue = value;
            TickValue = value;
            ApplicationDuration = 0f;
            TickInterval = 0f;
            CastLifecycleId = castLifecycleId;
        }

        public DamageRequestedEvent(
            EntityId sourceId,
            EntityId targetId,
            float totalValue,
            float tickValue,
            float applicationDuration,
            float tickInterval,
            int castLifecycleId = 0)
        {
            SourceId = sourceId;
            TargetId = targetId;
            TotalValue = totalValue;
            TickValue = tickValue;
            ApplicationDuration = applicationDuration;
            TickInterval = tickInterval;
            Value = tickValue;
            CastLifecycleId = castLifecycleId;
        }
    }

    public readonly struct AbilityPhaseChangedEvent
    {
        public readonly AbilityId AbilityId;
        public readonly EntityId CasterId;
        public readonly string PhaseName;
        public readonly int CastLifecycleId;

        public AbilityPhaseChangedEvent(AbilityId abilityId, EntityId casterId, string phaseName, int castLifecycleId = 0)
        {
            AbilityId = abilityId;
            CasterId = casterId;
            PhaseName = phaseName;
            CastLifecycleId = castLifecycleId;
        }
    }

    public readonly struct PresentationAnimationIntent
    {
        public readonly EntityId EntityId;
        public readonly string AnimationName;
        public readonly bool WaitUntilEnd;
        public PresentationAnimationIntent(EntityId entityId, string animationName, bool waitUntilEnd)
        {
            EntityId = entityId;
            AnimationName = animationName;
            WaitUntilEnd = waitUntilEnd;
        }
    }

    public readonly struct PresentationSoundIntent
    {
        public readonly string ClipKey;
        public readonly float Volume;
        public PresentationSoundIntent(string clipKey, float volume)
        {
            ClipKey = clipKey;
            Volume = volume;
        }
    }

    public readonly struct PresentationVfxIntent
    {
        public readonly EntityId TargetId;
        public readonly string PrefabKey;
        public readonly VfxPresentationStyle PresentationStyle;
        public readonly AbilityTargetType TargetType;
        public readonly AbilityDurationType DurationType;
        public readonly float Duration;
        public readonly float Delay;
        public readonly float OffsetX;
        public readonly float OffsetY;
        public readonly float OffsetZ;

        public PresentationVfxIntent(EntityId targetId, string prefabKey, VfxPresentationStyle presentationStyle,
            AbilityTargetType targetType,
            AbilityDurationType durationType, float duration, float delay, float ox, float oy, float oz)
        {
            TargetId = targetId;
            PrefabKey = prefabKey;
            PresentationStyle = presentationStyle;
            TargetType = targetType;
            DurationType = durationType;
            Duration = duration;
            Delay = delay;
            OffsetX = ox;
            OffsetY = oy;
            OffsetZ = oz;
        }
    }

    public readonly struct PresentationMovementIntent
    {
        public readonly EntityId EntityId;
        public readonly float OffsetX;
        public readonly float OffsetY;
        public readonly float OffsetZ;
        public readonly float Duration;
        public PresentationMovementIntent(EntityId entityId, float ox, float oy, float oz, float duration)
        {
            EntityId = entityId;
            OffsetX = ox;
            OffsetY = oy;
            OffsetZ = oz;
            Duration = duration;
        }
    }

    public readonly struct PresentationAimIntent
    {
        public readonly EntityId CasterId;
        public readonly EntityId TargetId;
        public readonly AbilityTargetType TargetType;

        public PresentationAimIntent(EntityId casterId, EntityId targetId, AbilityTargetType targetType)
        {
            CasterId = casterId;
            TargetId = targetId;
            TargetType = targetType;
        }
    }
}
