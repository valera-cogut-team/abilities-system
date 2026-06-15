using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public interface IAbilityTargetResolver
    {
        List<EntityId> ResolveTargets(AbilityId abilityId, AbilityDefinition definition);
    }

    /// <summary>Maps ability <see cref="AbilityDefinition.TargetType"/> + selection to cast targets. No per-ability-id exceptions.</summary>
    public sealed class DemoAbilityTargetResolver : IAbilityTargetResolver
    {
        private readonly DemoEntityRegistry _registry;
        private readonly TargetingService _targeting;
        private readonly IEntityTargetFilter _targetFilter;

        public DemoAbilityTargetResolver(
            DemoEntityRegistry registry,
            TargetingService targeting,
            IEntityTargetFilter targetFilter = null)
        {
            _registry = registry;
            _targeting = targeting;
            _targetFilter = targetFilter ?? new RegisteredEnemyTargetFilter(registry);
        }

        public List<EntityId> ResolveTargets(AbilityId abilityId, AbilityDefinition definition)
        {
            if (definition.TargetType == AbilityTargetType.Player)
                return new List<EntityId> { _registry.PlayerId };

            return CollectSelectedEnemies();
        }

        private List<EntityId> CollectSelectedEnemies()
        {
            var result = new List<EntityId>(_targeting.SelectedTargets.Count);
            foreach (EntityId targetId in _targeting.SelectedTargets)
            {
                if (_targetFilter.IsValidTarget(targetId))
                    result.Add(targetId);
            }

            return result;
        }
    }
}
