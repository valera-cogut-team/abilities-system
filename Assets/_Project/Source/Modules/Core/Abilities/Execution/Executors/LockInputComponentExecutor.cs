using System;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;

namespace AvantajPrim.Abilities.Execution.Executors
{
    public sealed class LockInputComponentExecutor : IAbilityComponentExecutor
    {
        public Type DataType => typeof(LockInputComponentData);

        public void Execute(
            IAbilityComponentData data,
            AbilityExecutionContext context,
            IAbilityPresentationPort presentation,
            IEntityStatePort entityState)
        {
            if (data is not LockInputComponentData d)
                return;

            if (d.BlockMovement || d.BlockRotation)
            {
                entityState.AcquireCastInputLock(context.CasterId, d.BlockMovement, d.BlockRotation);
                return;
            }

            entityState.ReleaseCastInputLock(context.CasterId, true, true);
        }
    }
}
