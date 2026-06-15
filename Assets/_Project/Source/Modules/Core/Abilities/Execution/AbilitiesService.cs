using System;
using System.Collections.Generic;
using System.Threading;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using Cysharp.Threading.Tasks;

namespace AvantajPrim.Abilities.Execution
{
    public sealed class AbilitiesService
    {
        private readonly AbilityCatalog _catalog;
        private readonly AbilityExecutor _executor;
        private readonly AbilityActivationLog _activationLog;
        private readonly IAbilityPhaseNotifier _phaseNotifier;
        private readonly IAbilityCastLifecycle _castLifecycle;
        private readonly IAbilityCastCompletionAwaiter _castCompletionAwaiter;
        private readonly Dictionary<EntityId, IAbilityEntity> _entities = new Dictionary<EntityId, IAbilityEntity>();
        private readonly HashSet<EntityId> _busyTargets = new HashSet<EntityId>();
        private readonly Dictionary<EntityId, int> _activeCastSessionsByCaster = new Dictionary<EntityId, int>();

        public AbilityActivationLog ActivationLog => _activationLog;

        public bool HasActiveCasts(EntityId casterId) =>
            _activeCastSessionsByCaster.TryGetValue(casterId, out int count) && count > 0;

        public AbilitiesService(
            AbilityCatalog catalog,
            AbilityExecutor executor,
            AbilityActivationLog activationLog,
            IAbilityPhaseNotifier phaseNotifier,
            IAbilityCastLifecycle castLifecycle = null,
            IAbilityCastCompletionAwaiter castCompletionAwaiter = null)
        {
            _catalog = catalog;
            _executor = executor;
            _activationLog = activationLog;
            _phaseNotifier = phaseNotifier;
            _castLifecycle = castLifecycle ?? NullAbilityCastLifecycle.Instance;
            _castCompletionAwaiter = castCompletionAwaiter
                ?? castLifecycle as IAbilityCastCompletionAwaiter
                ?? NullAbilityCastLifecycle.Instance;
        }

        public void RegisterEntity(IAbilityEntity entity) => _entities[entity.Id] = entity;

        public void UnregisterEntity(EntityId id)
        {
            _entities.Remove(id);
            _busyTargets.Remove(id);
        }
        public bool TryGetEntity(EntityId id, out IAbilityEntity entity) => _entities.TryGetValue(id, out entity);

        public async UniTask<CastAbilityResult> CastAsync(
            AbilityId abilityId,
            EntityId casterId,
            EntityId targetId,
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (!_catalog.TryGet(abilityId, out AbilityDefinition definition))
                return CastAbilityResult.Fail(CastAbilityErrorCode.UnknownAbility);

            if (!_entities.TryGetValue(casterId, out IAbilityEntity caster))
                return CastAbilityResult.Fail(CastAbilityErrorCode.InvalidCaster);

            if (!TryOccupyTarget(targetId))
                return CastAbilityResult.Fail(CastAbilityErrorCode.AlreadyCasting);

            _entities.TryGetValue(targetId, out IAbilityEntity target);
            BeginCastSession(casterId);
            try
            {
                _activationLog.RecordCast(abilityId, casterId, new[] { targetId });
                return await RunCastAsync(
                    definition,
                    abilityId,
                    casterId,
                    targetId,
                    caster,
                    target,
                    cancellationToken);
            }
            finally
            {
                ReleaseTarget(targetId);
                EndCastSession(casterId);
            }
        }

        public async UniTask<CastAbilityResult> CastOnTargetsAsync(
            AbilityId abilityId,
            EntityId casterId,
            IReadOnlyList<EntityId> targetIds,
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (targetIds == null || targetIds.Count == 0)
                return CastAbilityResult.Fail(CastAbilityErrorCode.InvalidTarget);

            if (targetIds.Count == 1)
                return await CastAsync(abilityId, casterId, targetIds[0], cancellationToken);

            if (!_catalog.TryGet(abilityId, out AbilityDefinition definition))
                return CastAbilityResult.Fail(CastAbilityErrorCode.UnknownAbility);

            if (!_entities.TryGetValue(casterId, out IAbilityEntity caster))
                return CastAbilityResult.Fail(CastAbilityErrorCode.InvalidCaster);

            if (!TryOccupyTargets(targetIds))
                return CastAbilityResult.Fail(CastAbilityErrorCode.AlreadyCasting);

            BeginCastSession(casterId);
            try
            {
                int castId = 0;
                bool executeSucceeded = false;
                try
                {
                    castId = _castLifecycle.BeginCast(abilityId, casterId);
                    NotifyPhase(abilityId, casterId, AbilityConstants.Phases.Start, castId);
                    _activationLog.RecordCast(abilityId, casterId, targetIds);

                    for (int i = 0; i < targetIds.Count; i++)
                    {
                        foreach (IAbilityComponentData c in definition.Components)
                            _activationLog.Record(abilityId, casterId, targetIds[i], c.GetType().Name);
                    }

                    await _executor.ExecuteMultiTargetAsync(
                        definition,
                        casterId,
                        caster,
                        targetIds,
                        ResolveEntity,
                        castId,
                        cancellationToken);

                    executeSucceeded = true;
                }
                finally
                {
                    if (castId > 0)
                    {
                        _castLifecycle.MarkExecutionFinished(castId);
                        NotifyPhase(abilityId, casterId, AbilityConstants.Phases.End, castId);
                        if (!executeSucceeded)
                            _castLifecycle.ForceComplete(castId);
                    }
                }

                if (castId > 0)
                {
                    await WaitForCastCompletionAsync(castId, cancellationToken);
                    NotifyPhase(abilityId, casterId, AbilityConstants.Phases.Complete, castId);
                }

                return CastAbilityResult.Ok();
            }
            finally
            {
                ReleaseTargets(targetIds);
                EndCastSession(casterId);
            }
        }

        private IAbilityEntity ResolveEntity(EntityId id) =>
            _entities.TryGetValue(id, out IAbilityEntity entity) ? entity : null;

        private async UniTask<CastAbilityResult> RunCastAsync(
            AbilityDefinition definition,
            AbilityId abilityId,
            EntityId casterId,
            EntityId targetId,
            IAbilityEntity caster,
            IAbilityEntity target,
            System.Threading.CancellationToken cancellationToken)
        {
            int castId = 0;
            bool executeSucceeded = false;
            try
            {
                castId = _castLifecycle.BeginCast(abilityId, casterId);
                NotifyPhase(abilityId, casterId, AbilityConstants.Phases.Start, castId);

                var context = new AbilityExecutionContext(abilityId, casterId, targetId, caster, target, castId);
                foreach (IAbilityComponentData c in definition.Components)
                    _activationLog.Record(abilityId, casterId, targetId, c.GetType().Name);

                await _executor.ExecuteAsync(definition, context, cancellationToken);
                executeSucceeded = true;
            }
            finally
            {
                if (castId > 0)
                {
                    _castLifecycle.MarkExecutionFinished(castId);
                    NotifyPhase(abilityId, casterId, AbilityConstants.Phases.End, castId);
                    if (!executeSucceeded)
                        _castLifecycle.ForceComplete(castId);
                }
            }

            if (castId > 0)
            {
                await WaitForCastCompletionAsync(castId, cancellationToken);
                NotifyPhase(abilityId, casterId, AbilityConstants.Phases.Complete, castId);
            }

            return CastAbilityResult.Ok();
        }

        private async UniTask WaitForCastCompletionAsync(int castId, CancellationToken cancellationToken)
        {
            if (castId <= 0)
                return;

            await _castCompletionAwaiter.WaitForCompletionAsync(castId, cancellationToken);
        }

        private bool TryOccupyTarget(EntityId targetId) => _busyTargets.Add(targetId);

        private bool TryOccupyTargets(IReadOnlyList<EntityId> targetIds)
        {
            for (int i = 0; i < targetIds.Count; i++)
            {
                if (_busyTargets.Contains(targetIds[i]))
                {
                    for (int j = 0; j < i; j++)
                        _busyTargets.Remove(targetIds[j]);
                    return false;
                }
            }

            for (int i = 0; i < targetIds.Count; i++)
                _busyTargets.Add(targetIds[i]);

            return true;
        }

        private void ReleaseTarget(EntityId targetId) => _busyTargets.Remove(targetId);

        private void ReleaseTargets(IReadOnlyList<EntityId> targetIds)
        {
            for (int i = 0; i < targetIds.Count; i++)
            {
                _busyTargets.Remove(targetIds[i]);
            }
        }

        private void NotifyPhase(AbilityId abilityId, EntityId casterId, string phaseName, int castLifecycleId) =>
            _phaseNotifier.NotifyPhaseChanged(new AbilityPhaseChangedEvent(abilityId, casterId, phaseName, castLifecycleId));

        private void BeginCastSession(EntityId casterId)
        {
            if (!_activeCastSessionsByCaster.TryGetValue(casterId, out int count))
            {
                _activeCastSessionsByCaster[casterId] = 1;
            }
            else
            {
                _activeCastSessionsByCaster[casterId] = count + 1;
            }
        }

        private void EndCastSession(EntityId casterId)
        {
            if (!_activeCastSessionsByCaster.TryGetValue(casterId, out int count))
                return;

            if (count <= 1)
                _activeCastSessionsByCaster.Remove(casterId);
            else
                _activeCastSessionsByCaster[casterId] = count - 1;
        }

        public void OnUpdate(float deltaTime) => _activationLog.Tick(deltaTime);
    }
}
