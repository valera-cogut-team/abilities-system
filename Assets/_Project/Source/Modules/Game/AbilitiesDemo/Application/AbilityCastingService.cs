using System.Collections.Generic;
using System.Threading;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Facade;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using Cysharp.Threading.Tasks;
using Logger.Facade;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class AbilityCastingService
    {
        private readonly IAbilitiesFacade _abilities;
        private readonly AbilityCatalog _catalog;
        private readonly DemoEntityRegistry _registry;
        private readonly EntityStateRegistry _entityStateRegistry;
        private readonly DemoGameplaySession _session;
        private readonly PlayerInputRouter _inputRouter;
        private readonly PlayerMovementService _movement;
        private readonly IAbilityTargetResolver _targetResolver;
        private readonly ILoggerFacade _logger;

        public AbilityCastingService(
            IAbilitiesFacade abilities,
            AbilityCatalog catalog,
            DemoEntityRegistry registry,
            EntityStateRegistry entityStateRegistry,
            DemoGameplaySession session,
            PlayerInputRouter inputRouter,
            PlayerMovementService movement,
            IAbilityTargetResolver targetResolver,
            ILoggerFacade logger)
        {
            _abilities = abilities;
            _catalog = catalog;
            _registry = registry;
            _entityStateRegistry = entityStateRegistry;
            _session = session;
            _inputRouter = inputRouter;
            _movement = movement;
            _targetResolver = targetResolver;
            _logger = logger;
        }

        public async UniTask<CastAbilityResult> CastAsync(string abilityId, CancellationToken cancellationToken = default)
        {
            if (!_session.IsActive)
                return CastAbilityResult.Fail(CastAbilityErrorCode.Blocked);

            if (_registry.PlayerId == default)
            {
                _logger?.LogWarning("[AbilityCasting] Player not registered.");
                return CastAbilityResult.Fail(CastAbilityErrorCode.InvalidCaster);
            }

            var id = new AbilityId(abilityId);
            if (!CanCastByFsm(_registry.PlayerId, id))
                return CastAbilityResult.Fail(CastAbilityErrorCode.Blocked);

            List<EntityId> targets = ResolveTargets(id);
            if (targets.Count == 0)
                return CastAbilityResult.Fail(CastAbilityErrorCode.InvalidTarget);

            if (_catalog.TryGet(id, out AbilityDefinition definition) && ShouldValidateRange(definition))
            {
                foreach (EntityId targetId in targets)
                {
                    if (!IsWithinRange(_registry.PlayerId, targetId, definition.Range))
                        return CastAbilityResult.Fail(CastAbilityErrorCode.InvalidTarget);
                }
            }

            if (_catalog.TryGet(id, out AbilityDefinition lockDefinition) &&
                AbilityInputLockResolver.BlocksMovement(lockDefinition))
            {
                _movement?.CancelCurrent();
            }

            _inputRouter.FaceTarget(targets[0]);

            if (targets.Count == 1)
                return await _abilities.CastAsync(id, _registry.PlayerId, targets[0], cancellationToken);

            return await _abilities.CastOnTargetsAsync(id, _registry.PlayerId, targets, cancellationToken);
        }

        private static bool ShouldValidateRange(AbilityDefinition definition)
        {
            if (definition.Range <= 0f)
                return false;

            foreach (IAbilityComponentData component in definition.Components)
            {
                if (component is MovementComponentData)
                    return false;
            }

            return true;
        }

        private bool CanCastByFsm(EntityId casterId, AbilityId abilityId)
        {
            if (!_entityStateRegistry.TryGet(casterId, out IEntityStateMachineController controller))
                return true;

            if (controller.IsInState(EntityStatePaths.Vitality.Dead) ||
                controller.IsInState(EntityStatePaths.Action.HitReact))
            {
                return false;
            }

            if (controller.IsInState(EntityStatePaths.Action.CastingWildcard))
                return true;

            if (!AbilityCastAnimationResolver.TryGetCastAnimationName(_catalog, abilityId, out string animationName))
                return true;

            return controller.CanTransition(EntityStatePaths.Action.Casting(animationName));
        }

        public EntityId ResolveTarget(AbilityId abilityId)
        {
            List<EntityId> targets = ResolveTargets(abilityId);
            if (targets.Count > 0)
                return targets[0];

            if (_catalog.TryGet(abilityId, out AbilityDefinition definition) &&
                definition.TargetType == AbilityTargetType.Player)
                return _registry.PlayerId;

            return default;
        }

        public List<EntityId> ResolveTargets(AbilityId abilityId)
        {
            if (!_catalog.TryGet(abilityId, out AbilityDefinition definition))
                return new List<EntityId>();

            return _targetResolver.ResolveTargets(abilityId, definition);
        }

        private static bool IsWithinRange(EntityId casterId, EntityId targetId, float range, DemoEntityRegistry registry)
        {
            if (!registry.TryGetView(casterId, out EntityView casterView) ||
                !registry.TryGetView(targetId, out EntityView targetView))
            {
                return true;
            }

            float maxRange = range * range;
            Vector3 delta = targetView.transform.position - casterView.transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= maxRange;
        }

        private bool IsWithinRange(EntityId casterId, EntityId targetId, float range) =>
            IsWithinRange(casterId, targetId, range, _registry);
    }
}
