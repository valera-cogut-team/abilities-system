using System;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using StateMachine.Facade;

namespace AvantajPrim.Abilities.Execution.Executors
{
    public sealed class StatusEffectComponentExecutor : IAbilityComponentExecutor
    {
        private readonly IAbilityCastLifecycle _lifecycle;

        public StatusEffectComponentExecutor(IAbilityCastLifecycle lifecycle = null)
        {
            _lifecycle = lifecycle ?? NullAbilityCastLifecycle.Instance;
        }

        public Type DataType => typeof(StatusEffectComponentData);

        public void Execute(
            IAbilityComponentData data,
            AbilityExecutionContext context,
            IAbilityPresentationPort presentation,
            IEntityStatePort entityState)
        {
            if (data is not StatusEffectComponentData d)
                return;

            ApplyStatus(d, context, presentation, entityState, _lifecycle);
        }

        public static void ApplyStatus(
            StatusEffectComponentData d,
            AbilityExecutionContext context,
            IAbilityPresentationPort presentation,
            IEntityStatePort entityState,
            IAbilityCastLifecycle lifecycle = null)
        {
            lifecycle ??= NullAbilityCastLifecycle.Instance;

            if (!TryMapStatusPath(d.EffectType, out StatePath path))
                return;

            EntityId targetId = AbilityTargetIdResolver.Resolve(d.TargetType, context);
            float duration = d.DurationType == AbilityDurationType.Continuous ? d.Duration : AbilityConstants.Execution.InstantStatusDurationSeconds;
            float tickInterval = d.TickInterval > 0f ? d.TickInterval : AbilityConstants.Execution.DefaultStatusTickIntervalSeconds;
            float totalValue = d.TotalValue > 0f
                ? d.TotalValue
                : d.Value * (float)System.Math.Ceiling(duration / tickInterval);

            var payload = new StatusTransitionPayload(
                d.EffectType,
                d.Value,
                duration,
                tickInterval,
                totalValue,
                context.CastLifecycleId);
            var transitionContext = new TransitionContext(payload);

            if (entityState.IsInState(targetId, path))
                entityState.TryDeactivate(targetId, path);

            if (!entityState.TryTransition(targetId, path, transitionContext))
                return;

            if (context.CastLifecycleId > 0 && duration > 0f)
                lifecycle.RegisterPendingEffect(context.CastLifecycleId);

            string additionalVfxKey = d.ResolveAdditionalVfxKey();
            if (string.IsNullOrEmpty(additionalVfxKey))
                return;

            presentation.PublishVfx(new PresentationVfxIntent(
                targetId,
                additionalVfxKey,
                VfxPresentationStyle.Default,
                d.TargetType,
                d.DurationType,
                duration,
                d.DelaySeconds,
                0f,
                0f,
                0f));
        }

        private static bool TryMapStatusPath(StatusEffectType effectType, out StatePath path)
        {
            switch (effectType)
            {
                case StatusEffectType.Combustion:
                    path = AbilityStatePaths.StatusCombustion;
                    return true;
                case StatusEffectType.Freezing:
                    path = AbilityStatePaths.StatusFreezing;
                    return true;
                case StatusEffectType.Healing:
                    path = AbilityStatePaths.StatusHealing;
                    return true;
                case StatusEffectType.Bleeding:
                    path = AbilityStatePaths.StatusBleeding;
                    return true;
                default:
                    path = default;
                    return false;
            }
        }
    }
}
