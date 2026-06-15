using System;
using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.AbilitiesDemo.Application;

namespace AvantajPrim.AbilitiesDemo.Facade
{
    public interface IAbilitiesDemoFacade
    {
        EntityId PlayerId { get; }
        IReadOnlyList<EntityId> EnemyIds { get; }
        EntityId? SelectedTarget { get; }
        IReadOnlyList<EntityId> SelectedTargets { get; }
        DemoCombatRegistry CombatRegistry { get; }
        event Action SelectedTargetsChanged;
        event Action<bool> GameplayActiveChanged;
        event Action<bool> WorldVisibleChanged;

        void ClearSelectedTargets();
        bool TryGetCombatState(EntityId id, out EntityCombatState state);

        bool IsGameplayActive { get; }
        bool IsWorldVisible { get; }
        void SetGameplayActive(bool active);
        void SetWorldVisible(bool visible);
    }
}
