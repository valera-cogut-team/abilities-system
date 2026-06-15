using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using StateMachine.Facade;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class EntityStatePort : IEntityStatePort
    {
        private readonly EntityStateRegistry _registry;

        public EntityStatePort(EntityStateRegistry registry)
        {
            _registry = registry;
        }

        public bool TryTransition(EntityId id, StatePath path, in TransitionContext context = default)
        {
            if (!_registry.TryGet(id, out IEntityStateMachineController controller))
            {
#if UNITY_EDITOR && DEMO_FSM_DEBUG
                Debug.LogWarning($"[Demo][FSM] TryTransition missing controller id={id.Value} path={path.Full}");
#endif
                return false;
            }

            return controller.TryTransition(path, context);
        }

        public bool CanTransition(EntityId id, StatePath path, in TransitionContext context = default)
        {
            if (!_registry.TryGet(id, out IEntityStateMachineController controller))
                return false;

            return controller.CanTransition(path, context);
        }

        public bool IsInState(EntityId id, StatePath path)
        {
            if (!_registry.TryGet(id, out IEntityStateMachineController controller))
                return false;

            return controller.IsInState(path);
        }

        public bool TryDeactivate(EntityId id, StatePath path, in TransitionContext context = default)
        {
            if (!_registry.TryGet(id, out IEntityStateMachineController controller))
                return false;

            return controller.TryDeactivate(path, context);
        }

        public void AcquireCastInputLock(EntityId id, bool movement, bool rotation)
        {
            if (_registry.TryGet(id, out IEntityStateMachineController controller))
                controller.AcquireCastInputLock(movement, rotation);
        }

        public void ReleaseCastInputLock(EntityId id, bool movement, bool rotation)
        {
            if (_registry.TryGet(id, out IEntityStateMachineController controller))
                controller.ReleaseCastInputLock(movement, rotation);
        }

        public void ReleaseCastInputLockLayer(EntityId id)
        {
            if (_registry.TryGet(id, out IEntityStateMachineController controller))
                controller.ReleaseCastInputLockLayer();
        }
    }
}
