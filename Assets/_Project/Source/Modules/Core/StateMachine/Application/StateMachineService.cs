using System;
using StateMachine.Facade;

namespace StateMachine.Application
{
    public sealed class StateMachineService
    {
        public IStateMachineDefinition Build(Action<StateMachineBuilder> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var builder = StateMachineBuilder.Create("StateMachine");
            configure(builder);
            return builder.Build();
        }

        public IStateMachineRuntime CreateRuntime(IStateMachineDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (definition is StateMachineDefinition concrete)
                return concrete.CreateRuntime();

            return definition.CreateRuntime();
        }
    }
}
