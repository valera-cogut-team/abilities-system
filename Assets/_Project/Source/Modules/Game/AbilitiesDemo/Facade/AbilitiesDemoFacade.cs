using System;
using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.AbilitiesDemo.Application;

namespace AvantajPrim.AbilitiesDemo.Facade
{
    public sealed class AbilitiesDemoFacade : IAbilitiesDemoFacade
    {
        private readonly DemoEntityRegistry _registry;
        private readonly TargetingService _targeting;
        private readonly DemoCombatRegistry _combatRegistry;
        private readonly DemoGameplaySession _session;

        public AbilitiesDemoFacade(
            DemoEntityRegistry registry,
            TargetingService targeting,
            DemoCombatRegistry combatRegistry,
            DemoGameplaySession session)
        {
            _registry = registry;
            _targeting = targeting;
            _combatRegistry = combatRegistry;
            _session = session;
            _targeting.SelectedTargetsChanged += OnTargetingChanged;
        }

        public event Action SelectedTargetsChanged;
        public event Action<bool> GameplayActiveChanged;
        public event Action<bool> WorldVisibleChanged;

        public EntityId PlayerId => _registry.PlayerId;
        public IReadOnlyList<EntityId> EnemyIds => _registry.EnemyIds;
        public EntityId? SelectedTarget => _targeting.SelectedTarget;
        public IReadOnlyList<EntityId> SelectedTargets => _targeting.SelectedTargets;
        public DemoCombatRegistry CombatRegistry => _combatRegistry;
        public bool IsGameplayActive => _session.IsActive;
        public bool IsWorldVisible => _session.IsWorldVisible;

        public void ClearSelectedTargets() => _targeting.ClearSelection();

        public void SetWorldVisible(bool visible)
        {
            if (_session.IsWorldVisible == visible)
                return;

            _session.SetWorldVisible(visible);
            WorldVisibleChanged?.Invoke(visible);
        }

        public void SetGameplayActive(bool active)
        {
            if (_session.IsActive == active)
                return;

            _session.SetActive(active);
            if (!active)
                ClearSelectedTargets();

            GameplayActiveChanged?.Invoke(active);
        }

        public bool TryGetCombatState(EntityId id, out EntityCombatState state) =>
            _combatRegistry.TryGet(id, out state);

        private void OnTargetingChanged() => SelectedTargetsChanged?.Invoke();
    }
}
