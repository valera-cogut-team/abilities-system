using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    public sealed class CastPhasePresentationHandler
    {
        private readonly EntityStateRegistry _entityStateRegistry;
        private readonly Dictionary<EntityId, int> _activeCastCountByCaster = new Dictionary<EntityId, int>();

        public CastPhasePresentationHandler(EntityStateRegistry entityStateRegistry)
        {
            _entityStateRegistry = entityStateRegistry;
        }

        public void HandlePhaseChanged(AbilityPhaseChangedEvent phaseEvent)
        {
            if (phaseEvent.PhaseName == AbilityConstants.Phases.Start)
            {
                IncrementActiveCastCount(phaseEvent.CasterId);
                return;
            }

            if (phaseEvent.PhaseName == AbilityConstants.Phases.End)
            {
                if (!_entityStateRegistry.TryGet(phaseEvent.CasterId, out IEntityStateMachineController endController))
                    return;

                if (DecrementActiveCastCount(phaseEvent.CasterId) == 0)
                {
                    endController.TryTransition(EntityStatePaths.Action.None);
                    endController.TryTransition(EntityStatePaths.Locomotion.Idle);
                }

                return;
            }

            if (phaseEvent.PhaseName != AbilityConstants.Phases.Complete)
                return;

            if (!_entityStateRegistry.TryGet(phaseEvent.CasterId, out IEntityStateMachineController controller))
                return;

            controller.ReleaseCastInputLockLayer();
        }

        private void IncrementActiveCastCount(EntityId casterId)
        {
            if (_activeCastCountByCaster.TryGetValue(casterId, out int count))
                _activeCastCountByCaster[casterId] = count + 1;
            else
                _activeCastCountByCaster[casterId] = 1;
        }

        private int DecrementActiveCastCount(EntityId casterId)
        {
            if (!_activeCastCountByCaster.TryGetValue(casterId, out int count))
                return 0;

            if (count <= 1)
            {
                _activeCastCountByCaster.Remove(casterId);
                return 0;
            }

            int next = count - 1;
            _activeCastCountByCaster[casterId] = next;
            return next;
        }
    }
}
