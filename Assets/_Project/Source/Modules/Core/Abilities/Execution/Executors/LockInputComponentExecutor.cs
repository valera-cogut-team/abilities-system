using System;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using Cysharp.Threading.Tasks;

namespace AvantajPrim.Abilities.Execution.Executors
{
    public sealed class LockInputComponentExecutor : IAbilityComponentExecutor
    {
        public Type DataType => typeof(LockInputComponentData);

        public UniTask ExecuteAsync(
            IAbilityComponentData data,
            AbilityExecutionContext context,
            IAbilityPresentationPort presentation,
            IEntityStatePort entityState)
        {
            if (data is not LockInputComponentData d)
                return UniTask.CompletedTask;

            if (d.BlockMovement || d.BlockRotation)
            {
                entityState.AcquireCastInputLock(context.CasterId, d.BlockMovement, d.BlockRotation);
                return UniTask.CompletedTask;
            }

            entityState.ReleaseCastInputLock(context.CasterId, true, true);
            return UniTask.CompletedTask;
        }
    }
}