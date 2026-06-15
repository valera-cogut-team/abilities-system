using System;
using StateMachine.Application;

namespace StateMachine.Facade
{
    public sealed class StateMachineFacade : IStateMachineFacade
    {
        private readonly StateMachineService _service;

        public StateMachineFacade(StateMachineService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public IStateMachineDefinition Build(Action<StateMachineBuilder> configure) => _service.Build(configure);

        public IStateMachineRuntime CreateRuntime(IStateMachineDefinition definition) =>
            _service.CreateRuntime(definition);
    }
}
