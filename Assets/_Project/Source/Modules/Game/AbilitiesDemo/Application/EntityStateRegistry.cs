using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class EntityStateRegistry
    {
        private readonly Dictionary<EntityId, IEntityStateMachineController> _controllers =
            new Dictionary<EntityId, IEntityStateMachineController>();

        public void Register(EntityId id, IEntityStateMachineController controller)
        {
            if (controller == null)
                return;

            _controllers[id] = controller;
        }

        public void Unregister(EntityId id) => _controllers.Remove(id);

        public bool TryGet(EntityId id, out IEntityStateMachineController controller) =>
            _controllers.TryGetValue(id, out controller);

        public void Clear() => _controllers.Clear();

        public void TickAll(float deltaTime)
        {
            foreach (IEntityStateMachineController controller in _controllers.Values)
                controller.Tick(deltaTime);
        }
    }
}
