using System;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using StateMachine.Facade;

namespace AvantajPrim.Abilities.Execution.Executors
{
    public sealed class AnimationComponentExecutor : IAbilityComponentExecutor
    {
        public Type DataType => typeof(AnimationComponentData);

        public void Execute(
            IAbilityComponentData data,
            AbilityExecutionContext context,
            IAbilityPresentationPort presentation,
            IEntityStatePort entityState)
        {
            if (data is not AnimationComponentData d)
                return;

            string animationName = d.ResolveAnimationName();
            if (string.IsNullOrEmpty(animationName))
                return;

            entityState.TryTransition(
                context.CasterId,
                StatePath.Parse($"{AbilityConstants.Casting.PathPrefix}{animationName}"));

            presentation.PublishAnimation(new PresentationAnimationIntent(
                context.CasterId,
                animationName,
                d.WaitUntilEnd));
        }
    }
}
