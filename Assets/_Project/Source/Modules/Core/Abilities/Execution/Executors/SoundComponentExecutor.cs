using System;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using Cysharp.Threading.Tasks;

namespace AvantajPrim.Abilities.Execution.Executors
{
    public sealed class SoundComponentExecutor : IAbilityComponentExecutor
    {
        public Type DataType => typeof(SoundComponentData);

        public UniTask ExecuteAsync(
            IAbilityComponentData data,
            AbilityExecutionContext context,
            IAbilityPresentationPort presentation,
            IEntityStatePort entityState)
        {
            if (data is not SoundComponentData d)
                return UniTask.CompletedTask;

            string clipKey = d.ResolveClipKey();
            if (string.IsNullOrEmpty(clipKey))
                return UniTask.CompletedTask;

            presentation.PublishSound(new PresentationSoundIntent(clipKey, d.Volume));
            return UniTask.CompletedTask;
        }
    }
}