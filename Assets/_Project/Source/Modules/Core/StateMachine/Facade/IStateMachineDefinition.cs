namespace StateMachine.Facade
{
    public interface IStateMachineDefinition
    {
        string Name { get; }
        IStateMachineRuntime CreateRuntime();
    }
}
