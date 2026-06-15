using System;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Execution;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;

namespace AvantajPrim.Abilities.Execution.Executors
{
    public sealed class VisualFxComponentExecutor : IAbilityComponentExecutor
    {
        public Type DataType => typeof(VisualFxComponentData);

        public void Execute(
            IAbilityComponentData data,
            AbilityExecutionContext context,
            IAbilityPresentationPort presentation,
            IEntityStatePort entityState)
        {
            if (data is not VisualFxComponentData d)
                return;

            string prefabKey = d.ResolveVfxKey();
            if (string.IsNullOrEmpty(prefabKey))
                return;

            EntityId targetId = ResolveVfxTarget(d.TargetType, context);
            presentation.PublishVfx(new PresentationVfxIntent(
                targetId,
                prefabKey,
                d.PresentationStyle,
                d.TargetType,
                d.DurationType,
                d.Duration,
                d.DelaySeconds,
                d.OffsetX,
                d.OffsetY,
                d.OffsetZ));
        }

        internal static EntityId ResolveVfxTarget(AbilityTargetType targetType, AbilityExecutionContext context) =>
            AbilityTargetIdResolver.Resolve(targetType, context);
    }
}
