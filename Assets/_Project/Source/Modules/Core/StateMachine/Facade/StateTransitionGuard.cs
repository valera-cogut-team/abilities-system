namespace StateMachine.Facade
{
    public delegate bool StateTransitionGuard(
        StatePath from,
        StatePath to,
        IStateMachineRuntime runtime,
        in TransitionContext context);
}
