using System;
using System.Collections.Generic;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using Cysharp.Threading.Tasks;

namespace AvantajPrim.Abilities.Execution
{
    public sealed class AbilityExecutor
    {
        private readonly AbilityComponentRegistry _registry;
        private readonly IAbilityPresentationPort _presentation;
        private readonly IEntityStatePort _entityState;
        private readonly IAbilityAnimationAwaiter _animationAwaiter;
        private readonly List<IAbilityComponentData> _onStart = new List<IAbilityComponentData>(8);
        private readonly List<IAbilityComponentData> _onEnd = new List<IAbilityComponentData>(4);
        private readonly List<IAbilityComponentData> _delayed = new List<IAbilityComponentData>(8);

        public AbilityExecutor(
            AbilityComponentRegistry registry,
            IAbilityPresentationPort presentation,
            IEntityStatePort entityState,
            IAbilityAnimationAwaiter animationAwaiter = null)
        {
            _registry = registry;
            _presentation = presentation;
            _entityState = entityState;
            _animationAwaiter = animationAwaiter ?? ImmediateAnimationAwaiter.Instance;
        }

        public async UniTask ExecuteAsync(
            AbilityDefinition definition,
            AbilityExecutionContext context,
            System.Threading.CancellationToken cancellationToken = default)
        {
            PartitionComponents(definition.Components, out List<IAbilityComponentData> casterOnStart, out List<IAbilityComponentData> targetOnStart,
                out List<IAbilityComponentData> casterDelayed, out List<IAbilityComponentData> targetDelayed, out List<IAbilityComponentData> casterOnEnd, out List<IAbilityComponentData> targetOnEnd);

            await RunComponentsAsync(casterOnStart, context, cancellationToken);
            await RunTargetComponentsParallel(targetOnStart, context, cancellationToken);
            await RunDelayedComponentsAsync(casterDelayed, targetDelayed, context, cancellationToken);
            await RunOnEndComponentsAsync(casterOnEnd, targetOnEnd, context, cancellationToken);
        }

        public async UniTask ExecuteMultiTargetAsync(
            AbilityDefinition definition,
            EntityId casterId,
            IAbilityEntity caster,
            IReadOnlyList<EntityId> targetIds,
            Func<EntityId, IAbilityEntity> resolveEntity,
            int castLifecycleId = 0,
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (targetIds == null || targetIds.Count == 0)
                return;

            EntityId primaryTargetId = targetIds[0];
            resolveEntity(primaryTargetId);
            var primaryContext = new AbilityExecutionContext(
                definition.Id,
                casterId,
                primaryTargetId,
                caster,
                resolveEntity(primaryTargetId),
                castLifecycleId);

            PartitionComponents(definition.Components, out List<IAbilityComponentData> casterOnStart, out List<IAbilityComponentData> targetOnStart,
                out List<IAbilityComponentData> casterDelayed, out List<IAbilityComponentData> targetDelayed, out List<IAbilityComponentData> casterOnEnd, out List<IAbilityComponentData> targetOnEnd);

            await RunComponentsAsync(casterOnStart, primaryContext, cancellationToken);
            await RunTargetComponentsParallel(targetOnStart, primaryContext, targetIds, resolveEntity, cancellationToken);
            await RunDelayedComponentsMultiAsync(
                casterDelayed, targetDelayed, primaryContext, targetIds, resolveEntity, cancellationToken);
            await RunOnEndComponentsMultiAsync(
                casterOnEnd, targetOnEnd, primaryContext, targetIds, resolveEntity, cancellationToken);
        }

        private static void PartitionComponents(
            IReadOnlyList<IAbilityComponentData> components,
            out List<IAbilityComponentData> casterOnStart,
            out List<IAbilityComponentData> targetOnStart,
            out List<IAbilityComponentData> casterDelayed,
            out List<IAbilityComponentData> targetDelayed,
            out List<IAbilityComponentData> casterOnEnd,
            out List<IAbilityComponentData> targetOnEnd)
        {
            casterOnStart = new List<IAbilityComponentData>(4);
            targetOnStart = new List<IAbilityComponentData>(4);
            casterDelayed = new List<IAbilityComponentData>(4);
            targetDelayed = new List<IAbilityComponentData>(4);
            casterOnEnd = new List<IAbilityComponentData>(4);
            targetOnEnd = new List<IAbilityComponentData>(4);

            for (int i = 0; i < components.Count; i++)
            {
                IAbilityComponentData c = components[i];
                bool isCasterScoped = IsCasterScoped(c);
                switch (c.PlayTimeType)
                {
                    case AbilityPlayTimeType.OnStart:
                        (isCasterScoped ? casterOnStart : targetOnStart).Add(c);
                        break;
                    case AbilityPlayTimeType.Delay:
                        (isCasterScoped ? casterDelayed : targetDelayed).Add(c);
                        break;
                    case AbilityPlayTimeType.OnEnd:
                        (isCasterScoped ? casterOnEnd : targetOnEnd).Add(c);
                        break;
                }
            }
        }

        private static bool IsCasterScoped(IAbilityComponentData data) => data.IsCasterScoped;

        private async UniTask RunTargetComponentsParallel(
            IReadOnlyList<IAbilityComponentData> components,
            AbilityExecutionContext primaryContext,
            System.Threading.CancellationToken cancellationToken)
        {
            if (components.Count == 0)
                return;

            await RunComponentsAsync(components, primaryContext, cancellationToken);
        }

        private async UniTask RunTargetComponentsParallel(
            IReadOnlyList<IAbilityComponentData> components,
            AbilityExecutionContext primaryContext,
            IReadOnlyList<EntityId> targetIds,
            Func<EntityId, IAbilityEntity> resolveEntity,
            System.Threading.CancellationToken cancellationToken)
        {
            if (components.Count == 0 || targetIds.Count <= 1)
            {
                await RunTargetComponentsParallel(components, primaryContext, cancellationToken);
                return;
            }

            var tasks = new UniTask[targetIds.Count];
            for (int i = 0; i < targetIds.Count; i++)
            {
                EntityId targetId = targetIds[i];
                var context = new AbilityExecutionContext(
                    primaryContext.AbilityId,
                    primaryContext.CasterId,
                    targetId,
                    primaryContext.Caster,
                    resolveEntity(targetId),
                    primaryContext.CastLifecycleId);
                tasks[i] = RunComponentsAsync(components, context, cancellationToken);
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask RunDelayedComponentsAsync(
            IReadOnlyList<IAbilityComponentData> casterDelayed,
            IReadOnlyList<IAbilityComponentData> targetDelayed,
            AbilityExecutionContext context,
            System.Threading.CancellationToken cancellationToken)
        {
            int delayedCount = Math.Max(casterDelayed.Count, targetDelayed.Count);
            for (int i = 0; i < delayedCount; i++)
            {
                if (i < casterDelayed.Count)
                {
                    IAbilityComponentData c = casterDelayed[i];
                    if (c.DelaySeconds > 0f)
                        await UniTask.Delay(System.TimeSpan.FromSeconds(c.DelaySeconds), cancellationToken: cancellationToken);
                    await RunComponentAsync(c, context, cancellationToken);
                }

                if (i < targetDelayed.Count)
                {
                    IAbilityComponentData c = targetDelayed[i];
                    if (c.DelaySeconds > 0f)
                        await UniTask.Delay(System.TimeSpan.FromSeconds(c.DelaySeconds), cancellationToken: cancellationToken);
                    await RunComponentAsync(c, context, cancellationToken);
                }
            }
        }

        private async UniTask RunDelayedComponentsMultiAsync(
            IReadOnlyList<IAbilityComponentData> casterDelayed,
            IReadOnlyList<IAbilityComponentData> targetDelayed,
            AbilityExecutionContext primaryContext,
            IReadOnlyList<EntityId> targetIds,
            Func<EntityId, IAbilityEntity> resolveEntity,
            System.Threading.CancellationToken cancellationToken)
        {
            int delayedCount = Math.Max(casterDelayed.Count, targetDelayed.Count);
            for (int i = 0; i < delayedCount; i++)
            {
                if (i < casterDelayed.Count)
                {
                    IAbilityComponentData c = casterDelayed[i];
                    if (c.DelaySeconds > 0f)
                        await UniTask.Delay(System.TimeSpan.FromSeconds(c.DelaySeconds), cancellationToken: cancellationToken);
                    await RunComponentAsync(c, primaryContext, cancellationToken);
                }

                if (i < targetDelayed.Count)
                {
                    IAbilityComponentData c = targetDelayed[i];
                    if (c.DelaySeconds > 0f)
                        await UniTask.Delay(System.TimeSpan.FromSeconds(c.DelaySeconds), cancellationToken: cancellationToken);

                    if (targetIds.Count <= 1)
                    {
                        await RunComponentAsync(c, primaryContext, cancellationToken);
                        continue;
                    }

                    var tasks = new UniTask[targetIds.Count];
                    for (int t = 0; t < targetIds.Count; t++)
                    {
                        EntityId targetId = targetIds[t];
                        var context = new AbilityExecutionContext(
                            primaryContext.AbilityId,
                            primaryContext.CasterId,
                            targetId,
                            primaryContext.Caster,
                            resolveEntity(targetId),
                            primaryContext.CastLifecycleId);
                        tasks[t] = RunComponentAsync(c, context, cancellationToken);
                    }

                    await UniTask.WhenAll(tasks);
                }
            }
        }

        private async UniTask RunOnEndComponentsAsync(
            IReadOnlyList<IAbilityComponentData> casterOnEnd,
            IReadOnlyList<IAbilityComponentData> targetOnEnd,
            AbilityExecutionContext context,
            System.Threading.CancellationToken cancellationToken)
        {
            if (casterOnEnd.Count == 0 && targetOnEnd.Count == 0)
                return;

            await UniTask.Delay(System.TimeSpan.FromSeconds(AbilityConstants.Execution.OnEndComponentDelaySeconds), cancellationToken: cancellationToken);
            await RunComponentsAsync(casterOnEnd, context, cancellationToken);
            await RunComponentsAsync(targetOnEnd, context, cancellationToken);
        }

        private async UniTask RunOnEndComponentsMultiAsync(
            IReadOnlyList<IAbilityComponentData> casterOnEnd,
            IReadOnlyList<IAbilityComponentData> targetOnEnd,
            AbilityExecutionContext primaryContext,
            IReadOnlyList<EntityId> targetIds,
            Func<EntityId, IAbilityEntity> resolveEntity,
            System.Threading.CancellationToken cancellationToken)
        {
            if (casterOnEnd.Count == 0 && targetOnEnd.Count == 0)
                return;

            await UniTask.Delay(System.TimeSpan.FromSeconds(AbilityConstants.Execution.OnEndComponentDelaySeconds), cancellationToken: cancellationToken);
            await RunComponentsAsync(casterOnEnd, primaryContext, cancellationToken);
            await RunTargetComponentsParallel(targetOnEnd, primaryContext, targetIds, resolveEntity, cancellationToken);
        }

        private async UniTask RunComponentsAsync(
            IReadOnlyList<IAbilityComponentData> components,
            AbilityExecutionContext context,
            System.Threading.CancellationToken cancellationToken)
        {
            for (int i = 0; i < components.Count; i++)
                await RunComponentAsync(components[i], context, cancellationToken);
        }

        private async UniTask RunComponentAsync(
            IAbilityComponentData data,
            AbilityExecutionContext context,
            System.Threading.CancellationToken cancellationToken)
        {
            if (!_registry.TryResolve(data, out IAbilityComponentExecutor executor))
                return;

            await executor.ExecuteAsync(data, context, _presentation, _entityState);

            if (data is AnimationComponentData animationData &&
                animationData.WaitUntilEnd &&
                !string.IsNullOrEmpty(animationData.ResolveAnimationName()))
            {
                await _animationAwaiter.WaitForAnimationAsync(
                    context.CasterId,
                    animationData.ResolveAnimationName(),
                    cancellationToken);
            }
        }
    }
}
