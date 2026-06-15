using System;
using AvantajPrim.Abilities.Domain;
using StateMachine.Facade;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    public interface IEntityStateMachineController : IDisposable
    {
        EntityId EntityId { get; }
        IStateMachineRuntime Runtime { get; }

        void OnStatusEnter(StatePath path, TransitionContext context);
        void OnStatusExit(StatePath path);
        void TickStatus(StatePath path, float deltaTime);
        bool IsInState(StatePath path);
        bool CanTransition(StatePath path, in TransitionContext context = default);
        bool IsMovementBlocked { get; }
        bool IsRotationBlocked { get; }
        bool TryTransition(StatePath path, in TransitionContext context = default);
        void ReleasePresentationEffects();
        void AcquireCastInputLock(bool movement, bool rotation);
        void ReleaseCastInputLock(bool movement, bool rotation);
        void ReleaseCastInputLockLayer();
        void ForceReleaseAllInputLocks();
        void ReleaseInputLocks();
        void CompleteCastEffect(int castLifecycleId);
        void ScheduleHitReactExit(float delaySeconds);
        bool TryDeactivate(StatePath path, in TransitionContext context = default);
        void Tick(float deltaTime);
        bool TickCastAnimationWait(string animationName, ref float elapsed, ref int phase, float deltaTime);
        void PlayCastAnimation(string animationName);
    }
}
