using System;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;

namespace AvantajPrim.Abilities.Execution.Executors
{
    public sealed class SoundComponentExecutor : IAbilityComponentExecutor
    {
        public Type DataType => typeof(SoundComponentData);

        public void Execute(
            IAbilityComponentData data,
            AbilityExecutionContext context,
            IAbilityPresentationPort presentation,
            IEntityStatePort entityState)
        {
            if (data is not SoundComponentData d)
                return;

            string clipKey = d.ResolveClipKey();
            if (string.IsNullOrEmpty(clipKey))
                return;

            presentation.PublishSound(new PresentationSoundIntent(clipKey, d.Volume));
        }
    }
}
