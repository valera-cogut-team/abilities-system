using EntityId = AvantajPrim.Abilities.Domain.EntityId;

namespace AvantajPrim.AbilitiesDemo.Application
{
    /// <summary>Filters entity ids that can be resolved as ability targets.</summary>
    public interface IEntityTargetFilter
    {
        bool IsValidTarget(EntityId id);
    }

    public sealed class RegisteredEnemyTargetFilter : IEntityTargetFilter
    {
        private readonly DemoEntityRegistry _registry;

        public RegisteredEnemyTargetFilter(DemoEntityRegistry registry)
        {
            _registry = registry;
        }

        public bool IsValidTarget(EntityId id) => _registry.IsEnemy(id);
    }
}
