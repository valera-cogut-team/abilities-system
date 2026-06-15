using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;

namespace AvantajPrim.Abilities.Execution
{
    public sealed class AbilityCatalog
    {
        private readonly Dictionary<string, AbilityDefinition> _definitions = new Dictionary<string, AbilityDefinition>();

        public void Register(AbilityDefinition definition) => _definitions[definition.Id.Value] = definition;

        public bool TryGet(AbilityId id, out AbilityDefinition definition) => _definitions.TryGetValue(id.Value, out definition);

        public int Count => _definitions.Count;

        public IReadOnlyCollection<string> AllIds => _definitions.Keys;

        public IEnumerable<AbilityDefinition> Enumerate() => _definitions.Values;

        public bool TryGetByHotkeySlot(int hotkeySlot, out AbilityDefinition definition)
        {
            foreach (AbilityDefinition def in _definitions.Values)
            {
                if (def.HotkeySlot == hotkeySlot)
                {
                    definition = def;
                    return true;
                }
            }

            definition = null;
            return false;
        }
    }
}
