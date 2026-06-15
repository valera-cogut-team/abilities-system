using StateMachine.Facade;

namespace AvantajPrim.Abilities.Domain.Ports
{
    public interface IEntityStatePort
    {
        bool TryTransition(EntityId id, StatePath path, in TransitionContext context = default);
        bool CanTransition(EntityId id, StatePath path, in TransitionContext context = default);
        bool IsInState(EntityId id, StatePath path);
        bool TryDeactivate(EntityId id, StatePath path, in TransitionContext context = default);
        void AcquireCastInputLock(EntityId id, bool movement, bool rotation);
        void ReleaseCastInputLock(EntityId id, bool movement, bool rotation);
        void ReleaseCastInputLockLayer(EntityId id);
    }
}
