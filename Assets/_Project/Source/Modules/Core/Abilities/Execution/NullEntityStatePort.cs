using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using StateMachine.Facade;

namespace AvantajPrim.Abilities.Execution
{
    public sealed class NullEntityStatePort : IEntityStatePort
    {
        public static readonly NullEntityStatePort Instance = new NullEntityStatePort();

        private NullEntityStatePort() { }

        public bool TryTransition(EntityId id, StatePath path, in TransitionContext context = default) => false;
        public bool CanTransition(EntityId id, StatePath path, in TransitionContext context = default) => false;
        public bool IsInState(EntityId id, StatePath path) => false;
        public bool TryDeactivate(EntityId id, StatePath path, in TransitionContext context = default) => false;

        public void AcquireCastInputLock(EntityId id, bool movement, bool rotation) { }

        public void ReleaseCastInputLock(EntityId id, bool movement, bool rotation) { }

        public void ReleaseCastInputLockLayer(EntityId id) { }
    }
}
