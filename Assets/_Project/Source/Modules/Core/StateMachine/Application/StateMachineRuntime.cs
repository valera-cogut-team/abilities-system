using System;
using System.Collections.Generic;
using Core.Observable;
using Logger.Facade;
using StateMachine.Facade;

namespace StateMachine.Application
{
    /// <summary>
    /// Thread-safe hierarchical state machine runtime.
    /// All state mutations happen inside a lock, but OnEnter/OnExit/OnUpdate callbacks
    /// are invoked OUTSIDE the lock to prevent deadlocks when callbacks trigger nested transitions.
    /// </summary>
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

            // Create region runtimes first WITHOUT invoking callbacks
            var initialCallbacks = new List<Action>(regions.Count);
            foreach (RegionDefinition region in regions)
            {
                RegionRuntime runtime = region.Kind == RegionKind.Exclusive
                    ? (RegionRuntime)new ExclusiveRegionRuntime(region, this)
                    : new ParallelRegionRuntime(region, this);
                _regionRuntimes[region.Name] = runtime;

                // Collect initial OnEnter callbacks
                if (runtime is ExclusiveRegionRuntime exclusive)
                {
                    Action<StateMachineRuntime> initCallback = exclusive.GetInitialEnterCallback();
                    if (initCallback != null)
                        initialCallbacks.Add(() => initCallback(this));
                }
            }

            // Invoke initial callbacks OUTSIDE the lock
            foreach (Action callback in initialCallbacks)
                callback();
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
            Action<StateMachineRuntime> onEnterCallback = null;
            Action<StateMachineRuntime> onExitCallback = null;
            StateTransitionEvent? evt = null;

            lock (_lock)
            {
                if (_disposed || !_regionRuntimes.TryGetValue(to.Region, out RegionRuntime region))
                    return false;

                if (!region.TryTransition(to, context, this, out StateTransitionEvent transitionEvent,
                    out onExitCallback, out onEnterCallback))
                    return false;

                evt = transitionEvent;
            }

            // Invoke callbacks OUTSIDE lock to prevent deadlock
            onExitCallback?.Invoke(this);
            onEnterCallback?.Invoke(this);

            if (evt.HasValue)
                _stream.Publish(evt.Value);
            return true;
        }

        public bool TryDeactivate(StatePath path, in TransitionContext context = default)
        {
            Action<StateMachineRuntime> onExitCallback = null;
            StateTransitionEvent? evt = null;

            lock (_lock)
            {
                if (_disposed || !_regionRuntimes.TryGetValue(path.Region, out RegionRuntime region))
                    return false;

                if (region is not ParallelRegionRuntime parallel)
                    return false;

                if (!parallel.TryDeactivate(path, context, this, out StateTransitionEvent transitionEvent,
                    out onExitCallback))
                    return false;

                evt = transitionEvent;
            }

            // Invoke callbacks OUTSIDE lock to prevent deadlock
            onExitCallback?.Invoke(this);

            if (evt.HasValue)
                _stream.Publish(evt.Value);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            List<Action<StateMachineRuntime>> tickCallbacks = null;
            List<StateTransitionEvent> events = null;

            lock (_lock)
            {
                if (_disposed)
                    return;

                foreach (RegionRuntime region in _regionRuntimes.Values)
                    region.Tick(deltaTime, this, ref tickCallbacks, ref events);
            }

            // Invoke tick (OnUpdate) callbacks OUTSIDE lock
            if (tickCallbacks != null)
            {
                foreach (Action<StateMachineRuntime> callback in tickCallbacks)
                    callback(this);
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

        /// <summary>
        /// Abstract base for region runtimes.
        /// Callback actions are returned as out parameters and invoked outside the lock.
        /// </summary>
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
                out StateTransitionEvent transitionEvent,
                out Action<StateMachineRuntime> onExitCallback,
                out Action<StateMachineRuntime> onEnterCallback);
            public abstract void Tick(float deltaTime, StateMachineRuntime runtime,
                ref List<Action<StateMachineRuntime>> tickCallbacks, ref List<StateTransitionEvent> events);
            public StateNodeDefinition GetNode(StatePath path) =>
                _regionDefinition.States.TryGetValue(path.Full, out StateNodeDefinition node) ? node : null;
        }

        private sealed class ExclusiveRegionRuntime : RegionRuntime
        {
            private StatePath _current;
            private Action<StateMachineRuntime> _initialEnterCallback;

            public ExclusiveRegionRuntime(RegionDefinition definition, StateMachineRuntime runtime)
                : base(definition)
            {
                _current = definition.Initial;
                StateNodeDefinition node = GetNode(_current);
                // Store the callback for later invocation outside the lock
                if (node?.OnEnter != null)
                {
                    Action<IStateMachineRuntime, TransitionContext> handler = node.OnEnter;
                    _initialEnterCallback = (rt) => handler(rt, TransitionContext.Empty);
                }
            }

            /// <summary>Returns the initial OnEnter callback to be invoked outside the lock.</summary>
            public Action<StateMachineRuntime> GetInitialEnterCallback() => _initialEnterCallback;

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
                out StateTransitionEvent transitionEvent,
                out Action<StateMachineRuntime> onExitCallback,
                out Action<StateMachineRuntime> onEnterCallback)
            {
                transitionEvent = default;
                onExitCallback = null;
                onEnterCallback = null;

                if (!CanTransition(to, context, runtime))
                    return false;

                StatePath from = _current;
                StateNodeDefinition fromNode = GetNode(from);
                StateNodeDefinition toNode = GetNode(to);

                // Capture callbacks for invocation OUTSIDE the lock
                if (fromNode?.OnExit != null)
                {
                    Action<IStateMachineRuntime, TransitionContext> handler = fromNode.OnExit;
                    onExitCallback = (rt) => handler(rt, context);
                }

                _current = to;

                if (toNode?.OnEnter != null)
                {
                    Action<IStateMachineRuntime, TransitionContext> handler = toNode.OnEnter;
                    onEnterCallback = (rt) => handler(rt, context);
                }

                transitionEvent = new StateTransitionEvent(_regionDefinition.Name, from, to, context);
                return true;
            }

            public override void Tick(float deltaTime, StateMachineRuntime runtime,
                ref List<Action<StateMachineRuntime>> tickCallbacks, ref List<StateTransitionEvent> events)
            {
                StateNodeDefinition node = GetNode(_current);
                if (node?.OnUpdate != null)
                {
                    Action<IStateMachineRuntime, float> handler = node.OnUpdate;
                    tickCallbacks ??= new List<Action<StateMachineRuntime>>();
                    tickCallbacks.Add((rt) => handler(rt, deltaTime));
                }
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
                out StateTransitionEvent transitionEvent,
                out Action<StateMachineRuntime> onExitCallback,
                out Action<StateMachineRuntime> onEnterCallback)
            {
                transitionEvent = default;
                onExitCallback = null;
                onEnterCallback = null;

                if (!CanTransition(to, context, runtime))
                    return false;

                Activate(to, context, runtime, out onEnterCallback);
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
                out StateTransitionEvent transitionEvent,
                out Action<StateMachineRuntime> onExitCallback)
            {
                transitionEvent = default;
                onExitCallback = null;

                if (!_active.Contains(path))
                    return false;

                Deactivate(path, context, runtime, out onExitCallback);
                transitionEvent = new StateTransitionEvent(
                    _regionDefinition.Name,
                    path,
                    default,
                    context,
                    isParallelExit: true);
                return true;
            }

            public override void Tick(float deltaTime, StateMachineRuntime runtime,
                ref List<Action<StateMachineRuntime>> tickCallbacks, ref List<StateTransitionEvent> events)
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
                    if (node?.OnUpdate != null)
                    {
                        Action<IStateMachineRuntime, float> handler = node.OnUpdate;
                        tickCallbacks ??= new List<Action<StateMachineRuntime>>();
                        tickCallbacks.Add((rt) => handler(rt, deltaTime));
                    }

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

                    Deactivate(path, TransitionContext.Empty, runtime, out Action<StateMachineRuntime> onExit);
                    // Collect deactivation callbacks for invocation outside lock
                    if (onExit != null)
                    {
                        tickCallbacks ??= new List<Action<StateMachineRuntime>>();
                        tickCallbacks.Add(onExit);
                    }

                    events ??= new List<StateTransitionEvent>();
                    events.Add(new StateTransitionEvent(
                        _regionDefinition.Name,
                        path,
                        default,
                        TransitionContext.Empty,
                        isParallelExit: true));
                }
            }

            private void Activate(StatePath path, TransitionContext context, StateMachineRuntime runtime,
                out Action<StateMachineRuntime> onEnterCallback)
            {
                onEnterCallback = null;
                _active.Add(path);
                StateNodeDefinition node = GetNode(path);
                if (context.Payload is ITransitionDurationProvider durationProvider)
                    _remainingDuration[path] = durationProvider.DurationSeconds;
                else if (node?.DurationSeconds.HasValue == true)
                    _remainingDuration[path] = node.DurationSeconds.Value;

                if (node?.OnEnter != null)
                {
                    Action<IStateMachineRuntime, TransitionContext> handler = node.OnEnter;
                    onEnterCallback = (rt) => handler(rt, context);
                }
            }

            private void Deactivate(StatePath path, TransitionContext context, StateMachineRuntime runtime,
                out Action<StateMachineRuntime> onExitCallback)
            {
                onExitCallback = null;
                if (!_active.Remove(path))
                    return;

                _remainingDuration.Remove(path);
                StateNodeDefinition node = GetNode(path);
                if (node?.OnExit != null)
                {
                    Action<IStateMachineRuntime, TransitionContext> handler = node.OnExit;
                    onExitCallback = (rt) => handler(rt, context);
                }
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