using System;
using System.Collections.Generic;
using Logger.Facade;
using StateMachine.Facade;

namespace StateMachine.Application
{
    internal sealed class StateMachineDefinition : IStateMachineDefinition
    {
        private readonly string _name;
        private readonly IReadOnlyList<RegionDefinition> _regions;
        private readonly IReadOnlyList<StateTransitionGuard> _globalGuards;

        public StateMachineDefinition(
            string name,
            IReadOnlyList<RegionDefinition> regions,
            IReadOnlyList<StateTransitionGuard> globalGuards)
        {
            _name = name;
            _regions = regions;
            _globalGuards = globalGuards;
        }

        public string Name => _name;

        public IStateMachineRuntime CreateRuntime() =>
            new StateMachineRuntime(this, _regions, _globalGuards, logger: null);

        public IStateMachineRuntime CreateRuntime(ILoggerFacade logger) =>
            new StateMachineRuntime(this, _regions, _globalGuards, logger);
    }
}
