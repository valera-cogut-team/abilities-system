using System;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Execution;

namespace AvantajPrim.Abilities.Execution.Executors
{
    public sealed class AimComponentExecutor : IAbilityComponentExecutor
    {
        public Type DataType => typeof(AimComponentData);

        public void Execute(
            IAbilityComponentData data,
            AbilityExecutionContext context,
            IAbilityPresentationPort presentation,
            IEntityStatePort entityState)
        {
            if (data is not AimComponentData d)
                return;

            var targetId = AbilityTargetIdResolver.Resolve(d.TargetType, context);
            presentation.PublishAim(new PresentationAimIntent(context.CasterId, targetId, d.TargetType));
        }
    }
}
