using System;
using System.Collections.Generic;
using StateMachine.Facade;

namespace StateMachine.Application
{
    public sealed class StateMachineBuilder
    {
        private readonly string _name;
        private readonly List<RegionDefinition> _regions = new List<RegionDefinition>();
        private readonly List<StateTransitionGuard> _globalGuards = new List<StateTransitionGuard>();

        private StateMachineBuilder(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public static StateMachineBuilder Create(string name) => new StateMachineBuilder(name);

        public ExclusiveRegionBuilder ExclusiveRegion(string regionName)
        {
            var region = new RegionDefinition(regionName, RegionKind.Exclusive);
            _regions.Add(region);
            return new ExclusiveRegionBuilder(this, region);
        }

        public ParallelRegionBuilder ParallelRegion(string regionName)
        {
            var region = new RegionDefinition(regionName, RegionKind.Parallel);
            _regions.Add(region);
            return new ParallelRegionBuilder(this, region);
        }

        public StateMachineBuilder GlobalGuard(StateTransitionGuard guard)
        {
            if (guard != null)
                _globalGuards.Add(guard);
            return this;
        }

        public IStateMachineDefinition Build()
        {
            foreach (RegionDefinition region in _regions)
            {
                if (region.Kind == RegionKind.Exclusive && !region.HasInitial)
                    throw new InvalidOperationException($"Exclusive region '{region.Name}' requires Initial state.");
            }

            return new StateMachineDefinition(_name, _regions, _globalGuards);
        }

        public sealed class ExclusiveRegionBuilder
        {
            private readonly StateMachineBuilder _owner;
            private readonly RegionDefinition _region;

            internal ExclusiveRegionBuilder(StateMachineBuilder owner, RegionDefinition region)
            {
                _owner = owner;
                _region = region;
            }

            public ExclusiveRegionBuilder Initial(StatePath state)
            {
                _region.Initial = state;
                _region.HasInitial = true;
                EnsureState(state);
                return this;
            }

            public StateBuilder State(StatePath path)
            {
                EnsureState(path);
                return new StateBuilder(_owner, _region, path);
            }

            public ExclusiveRegionBuilder Allow(StatePath from, StatePath to, StateTransitionGuard guard = null)
            {
                _region.Rules.Add(new TransitionRule(from, to, guard, TransitionRuleKind.Exclusive));
                return this;
            }

            public ExclusiveRegionBuilder AllowAny(StatePath from, StateTransitionGuard guard = null)
            {
                _region.Rules.Add(new TransitionRule(from, StatePath.Parse(from.Region + ".*"), guard, TransitionRuleKind.ExclusiveAnyTo));
                return this;
            }

            public ExclusiveRegionBuilder AllowTo(StatePath to, StateTransitionGuard guard = null)
            {
                _region.Rules.Add(new TransitionRule(StatePath.Parse(to.Region + ".*"), to, guard, TransitionRuleKind.ExclusiveAnyFrom));
                return this;
            }

            public StateMachineBuilder EndRegion() => _owner;

            private void EnsureState(StatePath path)
            {
                if (!_region.States.ContainsKey(path.Full))
                    _region.States[path.Full] = new StateNodeDefinition(path);
            }
        }

        public sealed class ParallelRegionBuilder
        {
            private readonly StateMachineBuilder _owner;
            private readonly RegionDefinition _region;

            internal ParallelRegionBuilder(StateMachineBuilder owner, RegionDefinition region)
            {
                _owner = owner;
                _region = region;
            }

            public StateBuilder State(StatePath path)
            {
                if (!_region.States.ContainsKey(path.Full))
                    _region.States[path.Full] = new StateNodeDefinition(path);
                return new StateBuilder(_owner, _region, path);
            }

            public ParallelRegionBuilder AllowEnter(StatePath to, StateTransitionGuard guard = null)
            {
                _region.Rules.Add(new TransitionRule(default, to, guard, TransitionRuleKind.ParallelEnter));
                return this;
            }

            public StateMachineBuilder EndRegion() => _owner;
        }

        public sealed class StateBuilder
        {
            private readonly StateMachineBuilder _owner;
            private readonly RegionDefinition _region;
            private readonly StateNodeDefinition _node;

            internal StateBuilder(StateMachineBuilder owner, RegionDefinition region, StatePath path)
            {
                _owner = owner;
                _region = region;
                _node = region.States[path.Full];
            }

            public StateBuilder OnEnter(Action<IStateMachineRuntime, TransitionContext> handler)
            {
                _node.OnEnter = handler;
                return this;
            }

            public StateBuilder OnExit(Action<IStateMachineRuntime, TransitionContext> handler)
            {
                _node.OnExit = handler;
                return this;
            }

            public StateBuilder OnUpdate(Action<IStateMachineRuntime, float> handler)
            {
                _node.OnUpdate = handler;
                return this;
            }

            public StateBuilder Duration(float seconds)
            {
                _node.DurationSeconds = seconds;
                return this;
            }

            public ExclusiveRegionBuilder And() =>
                new ExclusiveRegionBuilder(_owner, _region);

            public ParallelRegionBuilder AndParallel() =>
                new ParallelRegionBuilder(_owner, _region);

            public StateMachineBuilder Done() => _owner;
        }
    }

    internal enum RegionKind
    {
        Exclusive,
        Parallel
    }

    internal enum TransitionRuleKind
    {
        Exclusive,
        ExclusiveAnyTo,
        ExclusiveAnyFrom,
        ParallelEnter
    }

    internal sealed class TransitionRule
    {
        public StatePath From { get; }
        public StatePath To { get; }
        public StateTransitionGuard Guard { get; }
        public TransitionRuleKind Kind { get; }

        public TransitionRule(StatePath from, StatePath to, StateTransitionGuard guard, TransitionRuleKind kind)
        {
            From = from;
            To = to;
            Guard = guard;
            Kind = kind;
        }
    }

    internal sealed class StateNodeDefinition
    {
        public StatePath Path { get; }
        public Action<IStateMachineRuntime, TransitionContext> OnEnter;
        public Action<IStateMachineRuntime, TransitionContext> OnExit;
        public Action<IStateMachineRuntime, float> OnUpdate;
        public float? DurationSeconds;

        public StateNodeDefinition(StatePath path)
        {
            Path = path;
        }
    }

    internal sealed class RegionDefinition
    {
        public string Name { get; }
        public RegionKind Kind { get; }
        public StatePath Initial;
        public bool HasInitial;
        public Dictionary<string, StateNodeDefinition> States { get; } = new Dictionary<string, StateNodeDefinition>();
        public List<TransitionRule> Rules { get; } = new List<TransitionRule>();

        public RegionDefinition(string name, RegionKind kind)
        {
            Name = name;
            Kind = kind;
        }
    }
}
