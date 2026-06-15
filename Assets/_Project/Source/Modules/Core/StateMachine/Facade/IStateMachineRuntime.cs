using System;
using System.Collections.Generic;

namespace StateMachine.Facade
{
    public interface IStateMachineRuntime : IDisposable
    {
        IObservable<StateTransitionEvent> TransitionStream { get; }
        IReadOnlyCollection<StatePath> GetActiveStates();
        bool IsInState(StatePath path);
        bool CanTransition(StatePath to, in TransitionContext context = default);
        bool TryTransition(StatePath to, in TransitionContext context = default);
        bool TryDeactivate(StatePath path, in TransitionContext context = default);
        void Tick(float deltaTime);
    }
}
