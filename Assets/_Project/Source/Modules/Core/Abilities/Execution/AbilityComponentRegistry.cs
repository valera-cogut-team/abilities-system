using System;
using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;

namespace AvantajPrim.Abilities.Execution
{
    public sealed class AbilityComponentRegistry
    {
        private readonly Dictionary<Type, IAbilityComponentExecutor> _map = new Dictionary<Type, IAbilityComponentExecutor>();

        public void Register(IAbilityComponentExecutor executor)
        {
            if (executor == null) throw new ArgumentNullException(nameof(executor));
            _map[executor.DataType] = executor;
        }

        public bool TryResolve(IAbilityComponentData data, out IAbilityComponentExecutor executor)
        {
            executor = null;
            if (data == null) return false;
            return _map.TryGetValue(data.GetType(), out executor);
        }
    }
}
