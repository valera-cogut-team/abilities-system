using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class DemoCombatRegistry
    {
        private readonly Dictionary<EntityId, EntityCombatState> _states = new Dictionary<EntityId, EntityCombatState>();

        public void Register(EntityCombatState state) => _states[state.EntityId] = state;

        public bool TryGet(EntityId id, out EntityCombatState state) => _states.TryGetValue(id, out state);

        public void Remove(EntityId id) => _states.Remove(id);

        public void Clear() => _states.Clear();

        public IEnumerable<EntityCombatState> All => _states.Values;
    }
}
