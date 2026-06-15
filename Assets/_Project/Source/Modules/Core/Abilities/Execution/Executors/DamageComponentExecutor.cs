using System;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using StateMachine.Facade;

namespace AvantajPrim.Abilities.Execution.Executors
{
    public sealed class DamageComponentExecutor : IAbilityComponentExecutor
    {
        private readonly IAbilityCastLifecycle _lifecycle;

        public DamageComponentExecutor(IAbilityCastLifecycle lifecycle = null)
        {
            _lifecycle = lifecycle ?? NullAbilityCastLifecycle.Instance;
        }

        public Type DataType => typeof(DamageComponentData);

        public void Execute(
            IAbilityComponentData data,
            AbilityExecutionContext context,
            IAbilityPresentationPort presentation,
            IEntityStatePort entityState)
        {
            if (data is not DamageComponentData d)
                return;

            if (entityState.IsInState(context.TargetId, AbilityStatePaths.ActionHitReact))
                entityState.TryTransition(context.TargetId, AbilityStatePaths.ActionNone);

            entityState.TryTransition(context.TargetId, AbilityStatePaths.ActionHitReact);

            if (d.IsGradual)
            {
                if (context.CastLifecycleId > 0)
                    _lifecycle.RegisterPendingEffect(context.CastLifecycleId);

                presentation.PublishDamage(new DamageRequestedEvent(
                    context.CasterId,
                    context.TargetId,
                    d.TotalValue,
                    d.TickValue,
                    d.ApplicationDuration,
                    d.TickInterval,
                    context.CastLifecycleId));
                return;
            }

            presentation.PublishDamage(new DamageRequestedEvent(context.CasterId, context.TargetId, d.TotalValue));
        }
    }
}
