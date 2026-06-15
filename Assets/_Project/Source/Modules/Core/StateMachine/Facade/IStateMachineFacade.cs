using System;
using StateMachine.Application;

namespace StateMachine.Facade
{
    public interface IStateMachineFacade
    {
        IStateMachineDefinition Build(Action<StateMachineBuilder> configure);
        IStateMachineRuntime CreateRuntime(IStateMachineDefinition definition);
    }
}
