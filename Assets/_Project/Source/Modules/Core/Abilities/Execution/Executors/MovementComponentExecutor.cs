using System;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using Cysharp.Threading.Tasks;
using StateMachine.Facade;

namespace AvantajPrim.Abilities.Execution.Executors
{
    public sealed class MovementComponentExecutor : IAbilityComponentExecutor
    {
        private readonly IAbilityCastLifecycle _lifecycle;

        public MovementComponentExecutor(IAbilityCastLifecycle lifecycle = null)
        {
            _lifecycle = lifecycle ?? NullAbilityCastLifecycle.Instance;
        }

        public Type DataType => typeof(MovementComponentData);

        public UniTask ExecuteAsync(
            IAbilityComponentData data,
            AbilityExecutionContext context,
            IAbilityPresentationPort presentation,
            IEntityStatePort entityState)
        {
            if (data is not MovementComponentData d)
                return UniTask.CompletedTask;

            var payload = new DisplacementTransitionPayload(
                d.OffsetX,
                d.OffsetY,
                d.OffsetZ,
                d.Duration,
                context.CastLifecycleId);
            var transition = new TransitionContext(payload);

            if (entityState.IsInState(context.CasterId, AbilityStatePaths.LocomotionDisplaced))
                entityState.TryTransition(context.CasterId, AbilityStatePaths.LocomotionIdle, transition);

            if (!entityState.TryTransition(context.CasterId, AbilityStatePaths.LocomotionDisplaced, transition))
                return UniTask.CompletedTask;

            if (context.CastLifecycleId > 0 && d.Duration > 0f)
                _lifecycle.RegisterPendingEffect(context.CastLifecycleId);

            presentation.PublishMovement(new PresentationMovementIntent(
                context.CasterId,
                d.OffsetX,
                d.OffsetY,
                d.OffsetZ,
                d.Duration));
            return UniTask.CompletedTask;
        }
    }
}