using System;
using System.Collections.Generic;
using Core.Observable;
using Logger.Facade;
using StateMachine.Facade;

namespace StateMachine.Application
{
    internal sealed class StateMachineRuntime : IStateMachineRuntime
    {
        private readonly StateMachineDefinition _definition;
        private readonly IReadOnlyList<RegionDefinition> _regions;
        private readonly IReadOnlyList<StateTransitionGuard> _globalGuards;
        private readonly ILoggerFacade _logger;
        private readonly object _lock = new object();
        private readonly SafeStream<StateTransitionEvent> _stream;
        private readonly Dictionary<string, RegionRuntime> _regionRuntimes = new Dictionary<string, RegionRuntime>();
        private readonly List<StatePath> _activeSnapshot = new List<StatePath>();
        private bool _disposed;

        public StateMachineRuntime(
            StateMachineDefinition definition,
            IReadOnlyList<RegionDefinition> regions,
            IReadOnlyList<StateTransitionGuard> globalGuards,
            ILoggerFacade logger)
        {
            _definition = definition;
            _regions = regions;
            _globalGuards = globalGuards;
            _logger = logger;
            _stream = new SafeStream<StateTransitionEvent>(ex =>
                _logger?.LogError($"StateMachine runtime stream error: {ex.Message}", ex));

            foreach (RegionDefinition region in regions)
            {
                RegionRuntime runtime = region.Kind == RegionKind.Exclusive
                    ? (RegionRuntime)new ExclusiveRegionRuntime(region, this)
                    : new ParallelRegionRuntime(region, this);
                _regionRuntimes[region.Name] = runtime;
            }
        }

        public IObservable<StateTransitionEvent> TransitionStream => _stream;

        public IReadOnlyCollection<StatePath> GetActiveStates()
        {
            lock (_lock)
            {
                _activeSnapshot.Clear();
                foreach (RegionRuntime region in _regionRuntimes.Values)
                    region.CollectActiveStates(_activeSnapshot);
                return _activeSnapshot;
            }
        }

        public bool IsInState(StatePath path)
        {
            lock (_lock)
            {
                if (!_regionRuntimes.TryGetValue(path.Region, out RegionRuntime region))
                    return false;
                return region.IsInState(path);
            }
        }

        public bool CanTransition(StatePath to, in TransitionContext context = default)
        {
            lock (_lock)
            {
                if (_disposed || !_regionRuntimes.TryGetValue(to.Region, out RegionRuntime region))
                    return false;
                return region.CanTransition(to, context, this);
            }
        }

        public bool TryTransition(StatePath to, in TransitionContext context = default)
        {
            StateTransitionEvent? evt = null;
            lock (_lock)
            {
                if (_disposed || !_regionRuntimes.TryGetValue(to.Region, out RegionRuntime region))
                    return false;

                if (!region.TryTransition(to, context, this, out StateTransitionEvent transitionEvent))
                    return false;

                evt = transitionEvent;
            }

            if (evt.HasValue)
                _stream.Publish(evt.Value);
            return true;
        }

        public bool TryDeactivate(StatePath path, in TransitionContext context = default)
        {
            StateTransitionEvent? evt = null;
            lock (_lock)
            {
                if (_disposed || !_regionRuntimes.TryGetValue(path.Region, out RegionRuntime region))
                    return false;

                if (region is not ParallelRegionRuntime parallel)
                    return false;

                if (!parallel.TryDeactivate(path, context, this, out StateTransitionEvent transitionEvent))
                    return false;

                evt = transitionEvent;
            }

            if (evt.HasValue)
                _stream.Publish(evt.Value);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            List<StateTransitionEvent> events = null;
            lock (_lock)
            {
                if (_disposed)
                    return;

                foreach (RegionRuntime region in _regionRuntimes.Values)
                    region.Tick(deltaTime, this, ref events);
            }

            if (events == null)
                return;

            foreach (StateTransitionEvent evt in events)
                _stream.Publish(evt);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;
                _disposed = true;
            }

            _stream.Dispose();
        }

        internal bool PassesGuards(
            StatePath from,
            StatePath to,
            TransitionContext context,
            StateTransitionGuard ruleGuard)
        {
            if (ruleGuard != null && !ruleGuard(from, to, this, context))
                return false;

            foreach (StateTransitionGuard guard in _globalGuards)
            {
                if (guard != null && !guard(from, to, this, context))
                    return false;
            }

            return true;
        }

        internal StateNodeDefinition GetNode(StatePath path)
        {
            if (!_regionRuntimes.TryGetValue(path.Region, out RegionRuntime region))
                return null;
            return region.GetNode(path);
        }

        private abstract class RegionRuntime
        {
            protected readonly RegionDefinition _regionDefinition;

            protected RegionRuntime(RegionDefinition definition)
            {
                _regionDefinition = definition;
            }

            public abstract void CollectActiveStates(List<StatePath> target);
            public abstract bool IsInState(StatePath path);
            public abstract bool CanTransition(StatePath to, TransitionContext context, StateMachineRuntime runtime);
            public abstract bool TryTransition(
                StatePath to,
                TransitionContext context,
                StateMachineRuntime runtime,
                out StateTransitionEvent transitionEvent);
            public abstract void Tick(float deltaTime, StateMachineRuntime runtime, ref List<StateTransitionEvent> events);
            public StateNodeDefinition GetNode(StatePath path) =>
                _regionDefinition.States.TryGetValue(path.Full, out StateNodeDefinition node) ? node : null;
        }

        private sealed class ExclusiveRegionRuntime : RegionRuntime
        {
            private StatePath _current;

            public ExclusiveRegionRuntime(RegionDefinition definition, StateMachineRuntime runtime)
                : base(definition)
            {
                _current = definition.Initial;
                StateNodeDefinition node = GetNode(_current);
                node?.OnEnter?.Invoke(runtime, TransitionContext.Empty);
            }

            public override void CollectActiveStates(List<StatePath> target) => target.Add(_current);

            public override bool IsInState(StatePath path) => _current.Matches(path) || _current.IsChildOf(path);

            public override bool CanTransition(StatePath to, TransitionContext context, StateMachineRuntime runtime)
            {
                if (to.Region != _regionDefinition.Name)
                    return false;
                if (_current == to)
                    return false;
                return MatchesRule(_current, to, context, runtime, out _);
            }

            public override bool TryTransition(
                StatePath to,
                TransitionContext context,
                StateMachineRuntime runtime,
                out StateTransitionEvent transitionEvent)
            {
                transitionEvent = default;
                if (!CanTransition(to, context, runtime))
                    return false;

                StatePath from = _current;
                GetNode(from)?.OnExit?.Invoke(runtime, context);
                _current = to;
                GetNode(to)?.OnEnter?.Invoke(runtime, context);
                transitionEvent = new StateTransitionEvent(_regionDefinition.Name, from, to, context);
                return true;
            }

            public override void Tick(float deltaTime, StateMachineRuntime runtime, ref List<StateTransitionEvent> events)
            {
                GetNode(_current)?.OnUpdate?.Invoke(runtime, deltaTime);
            }

            private bool MatchesRule(
                StatePath from,
                StatePath to,
                TransitionContext context,
                StateMachineRuntime runtime,
                out TransitionRule matchedRule)
            {
                matchedRule = null;
                foreach (TransitionRule rule in _regionDefinition.Rules)
                {
                    if (!RuleMatches(rule, from, to))
                        continue;

                    if (!runtime.PassesGuards(from, to, context, rule.Guard))
                        continue;

                    matchedRule = rule;
                    return true;
                }

                return false;
            }

            private static bool RuleMatches(TransitionRule rule, StatePath from, StatePath to)
            {
                switch (rule.Kind)
                {
                    case TransitionRuleKind.Exclusive:
                        return from.Matches(rule.From) && to.Matches(rule.To);
                    case TransitionRuleKind.ExclusiveAnyTo:
                        return from.Matches(rule.From) && to.Region == rule.From.Region;
                    case TransitionRuleKind.ExclusiveAnyFrom:
                        return to.Matches(rule.To);
                    default:
                        return false;
                }
            }
        }

        private sealed class ParallelRegionRuntime : RegionRuntime
        {
            private readonly HashSet<StatePath> _active = new HashSet<StatePath>();
            private readonly Dictionary<StatePath, float> _remainingDuration = new Dictionary<StatePath, float>();
            private readonly List<StatePath> _pendingDeactivate = new List<StatePath>(4);
            private readonly List<StatePath> _tickSnapshot = new List<StatePath>(4);

            public ParallelRegionRuntime(RegionDefinition definition, StateMachineRuntime runtime)
                : base(definition)
            {
            }

            public override void CollectActiveStates(List<StatePath> target)
            {
                foreach (StatePath path in _active)
                    target.Add(path);
            }

            public override bool IsInState(StatePath path)
            {
                foreach (StatePath active in _active)
                {
                    if (active.Matches(path) || path.Matches(active))
                        return true;
                }

                return _active.Count > 0 && path.Full.EndsWith(".*", StringComparison.Ordinal) &&
                       path.Full.StartsWith(_regionDefinition.Name + ".", StringComparison.Ordinal);
            }

            public override bool CanTransition(StatePath to, TransitionContext context, StateMachineRuntime runtime)
            {
                if (to.Region != _regionDefinition.Name)
                    return false;
                if (_active.Contains(to))
                    return false;
                return MatchesEnterRule(to, context, runtime);
            }

            public override bool TryTransition(
                StatePath to,
                TransitionContext context,
                StateMachineRuntime runtime,
                out StateTransitionEvent transitionEvent)
            {
                transitionEvent = default;
                if (!CanTransition(to, context, runtime))
                    return false;

                Activate(to, context, runtime);
                transitionEvent = new StateTransitionEvent(
                    _regionDefinition.Name,
                    default,
                    to,
                    context,
                    isParallelEnter: true);
                return true;
            }

            public bool TryDeactivate(
                StatePath path,
                TransitionContext context,
                StateMachineRuntime runtime,
                out StateTransitionEvent transitionEvent)
            {
                transitionEvent = default;
                if (!_active.Contains(path))
                    return false;

                Deactivate(path, context, runtime);
                transitionEvent = new StateTransitionEvent(
                    _regionDefinition.Name,
                    path,
                    default,
                    context,
                    isParallelExit: true);
                return true;
            }

            public override void Tick(float deltaTime, StateMachineRuntime runtime, ref List<StateTransitionEvent> events)
            {
                if (_active.Count == 0)
                    return;

                _pendingDeactivate.Clear();
                _tickSnapshot.Clear();
                foreach (StatePath path in _active)
                    _tickSnapshot.Add(path);

                foreach (StatePath path in _tickSnapshot)
                {
                    if (!_active.Contains(path))
                        continue;

                    StateNodeDefinition node = GetNode(path);
                    node?.OnUpdate?.Invoke(runtime, deltaTime);

                    if (!_active.Contains(path))
                        continue;

                    if (!_remainingDuration.TryGetValue(path, out float remaining))
                    {
                        if (!node?.DurationSeconds.HasValue ?? true)
                            continue;

                        remaining = node.DurationSeconds.Value;
                        _remainingDuration[path] = remaining;
                    }

                    remaining -= deltaTime;
                    if (remaining <= 0f)
                        _pendingDeactivate.Add(path);
                    else
                        _remainingDuration[path] = remaining;
                }

                if (_pendingDeactivate.Count == 0)
                    return;

                foreach (StatePath path in _pendingDeactivate)
                {
                    if (!_active.Contains(path))
                        continue;

                    Deactivate(path, TransitionContext.Empty, runtime);
                    events ??= new List<StateTransitionEvent>();
                    events.Add(new StateTransitionEvent(
                        _regionDefinition.Name,
                        path,
                        default,
                        TransitionContext.Empty,
                        isParallelExit: true));
                }
            }

            private void Activate(StatePath path, TransitionContext context, StateMachineRuntime runtime)
            {
                _active.Add(path);
                StateNodeDefinition node = GetNode(path);
                if (context.Payload is ITransitionDurationProvider durationProvider)
                    _remainingDuration[path] = durationProvider.DurationSeconds;
                else if (node?.DurationSeconds.HasValue == true)
                    _remainingDuration[path] = node.DurationSeconds.Value;
                node?.OnEnter?.Invoke(runtime, context);
            }

            private void Deactivate(StatePath path, TransitionContext context, StateMachineRuntime runtime)
            {
                if (!_active.Remove(path))
                    return;

                _remainingDuration.Remove(path);
                GetNode(path)?.OnExit?.Invoke(runtime, context);
            }

            private bool MatchesEnterRule(StatePath to, TransitionContext context, StateMachineRuntime runtime)
            {
                foreach (TransitionRule rule in _regionDefinition.Rules)
                {
                    if (rule.Kind != TransitionRuleKind.ParallelEnter)
                        continue;
                    if (!to.Matches(rule.To))
                        continue;
                    if (!runtime.PassesGuards(default, to, context, rule.Guard))
                        continue;
                    return true;
                }

                return false;
            }
        }
    }
}
