using System;
using System.Collections.Generic;
using System.Threading;
using Addressables.Facade;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using Cysharp.Threading.Tasks;
using Effects.Facade;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using StateMachine.Facade;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    public sealed class EntityStateMachineController : IEntityStateMachineController
    {
        private struct ActiveStatusData
        {
            internal StatusEffectType Type;
            internal readonly float Value;
            internal float TickInterval;
            internal float Accumulator;
            internal float RemainingTotal;
            internal int CastLifecycleId;

            internal ActiveStatusData(StatusTransitionPayload payload)
            {
                Type = payload.EffectType;
                Value = payload.Value;
                TickInterval = payload.TickInterval;
                Accumulator = 0f;
                RemainingTotal = payload.TotalValue;
                CastLifecycleId = payload.CastLifecycleId;
            }
        }

        private readonly Dictionary<string, ActiveStatusData> _activeStatuses = new Dictionary<string, ActiveStatusData>();

        private static readonly StatePath[] StatusPaths =
        {
            EntityStatePaths.Status.Combustion,
            EntityStatePaths.Status.Freezing,
            EntityStatePaths.Status.Healing,
            EntityStatePaths.Status.Bleeding
        };

        private readonly EntityView _view;
        private readonly DemoCombatRegistry _combatRegistry;
        private readonly IAbilityPresentationPort _presentation;
        private readonly IAbilityCastLifecycle _castLifecycle;
        private readonly EntityAttachedVfxRegistry _entityVfxRegistry;
        private readonly EntityAnimationPresenter _presenter;
        private readonly IStateMachineRuntime _runtime;
        private CancellationTokenSource _hitReactCts;
        private int _movementInputLockDepth;
        private int _rotationInputLockDepth;
        private bool _disposed;

        public EntityId EntityId { get; }
        public IStateMachineRuntime Runtime => _runtime;

        public EntityStateMachineController(
            EntityId entityId,
            EntityView view,
            bool isPlayer,
            IStateMachineFacade stateMachineFacade,
            EntityStateRegistry registry,
            DemoCombatRegistry combatRegistry,
            IAbilityPresentationPort presentation,
            IAbilityCastLifecycle castLifecycle,
            IEffectsFacade effects,
            IAddressablesFacade addressables,
            EntityAttachedVfxRegistry entityVfxRegistry)
        {
            EntityId = entityId;
            _view = view;
            _combatRegistry = combatRegistry;
            _presentation = presentation;
            _castLifecycle = castLifecycle ?? NullAbilityCastLifecycle.Instance;
            _entityVfxRegistry = entityVfxRegistry;
            _presenter = new EntityAnimationPresenter(view);
            view.BindStateMachine(this);

            IStateMachineDefinition definition = EntityStateMachineFactory.CreateDefinition(isPlayer, _presenter, this);
            _runtime = stateMachineFacade.CreateRuntime(definition);
            registry?.Register(entityId, this);
        }

        public void OnStatusEnter(StatePath path, TransitionContext context)
        {
            if (context.Payload is StatusTransitionPayload payload)
                _activeStatuses[path.Full] = new ActiveStatusData(payload);

            if (path.Equals(EntityStatePaths.Status.Freezing))
                TryTransition(EntityStatePaths.Input.Movement);
        }

        public void OnStatusExit(StatePath path)
        {
            if (_activeStatuses.TryGetValue(path.Full, out ActiveStatusData data))
                CompleteCastEffect(data.CastLifecycleId);

            _activeStatuses.Remove(path.Full);

            if (path.Equals(EntityStatePaths.Status.Freezing))
                TryDeactivate(EntityStatePaths.Input.Movement);
        }

        public void TickStatus(StatePath path, float deltaTime)
        {
            if (!_activeStatuses.TryGetValue(path.Full, out ActiveStatusData data))
                return;

            data.Accumulator += deltaTime;
            if (data.Accumulator < data.TickInterval)
            {
                _activeStatuses[path.Full] = data;
                return;
            }

            data.Accumulator = 0f;
            ApplyStatusTick(ref data);
            _activeStatuses[path.Full] = data;
        }

        private void ApplyStatusTick(ref ActiveStatusData data)
        {
            float amount = data.Value;
            if (data.RemainingTotal > 0f)
            {
                amount = Math.Min(amount, data.RemainingTotal);
                data.RemainingTotal -= amount;
            }

            if (amount <= 0f)
                return;

            float tweenDuration = GradualCombatApplier.ComputeTweenDuration(data.TickInterval);
            switch (data.Type)
            {
                case StatusEffectType.Combustion:
                case StatusEffectType.Bleeding:
                    _presentation?.PublishDamage(new DamageRequestedEvent(default(EntityId), EntityId, amount));
                    break;
                case StatusEffectType.Healing:
                    if (_combatRegistry != null && _combatRegistry.TryGet(EntityId, out EntityCombatState combat))
                        combat.ApplyHeal(amount, tweenDuration);
                    break;
            }
        }

        public bool IsInState(StatePath path) => _runtime != null && _runtime.IsInState(path);

        public bool CanTransition(StatePath path, in TransitionContext context = default) =>
            _runtime != null && _runtime.CanTransition(path, context);

        public bool IsMovementBlocked => IsInState(EntityStatePaths.Input.Movement);

        public bool IsRotationBlocked => IsInState(EntityStatePaths.Input.Rotation);

        public bool TryTransition(StatePath path, in TransitionContext context = default)
        {
            if (_runtime == null || !_runtime.TryTransition(path, context))
                return false;

            if (path.Full.StartsWith(Domain.EntityStatePaths.Action.CastingPathPrefix, StringComparison.Ordinal))
            {
                if (IsInState(EntityStatePaths.Locomotion.Walking))
                    TryTransition(EntityStatePaths.Locomotion.Idle);

                string trigger = path.Full.Substring(Domain.EntityStatePaths.Action.CastingPathPrefix.Length);
                _presenter?.PlayCastTrigger(trigger);
            }

            return true;
        }

        public void ReleasePresentationEffects()
        {
            ClearAllActiveStatuses();
            _entityVfxRegistry?.ReleaseAll(EntityId);
        }

        private void ClearAllActiveStatuses()
        {
            for (int i = 0; i < StatusPaths.Length; i++)
            {
                StatePath path = StatusPaths[i];
                if (IsInState(path))
                    TryDeactivate(path);
            }
        }

        public void AcquireCastInputLock(bool movement, bool rotation)
        {
            if (movement)
            {
                if (_movementInputLockDepth++ == 0)
                    TryTransition(EntityStatePaths.Input.Movement);
            }

            if (rotation)
            {
                if (_rotationInputLockDepth++ == 0)
                    TryTransition(EntityStatePaths.Input.Rotation);
            }
        }

        public void ReleaseCastInputLock(bool movement, bool rotation)
        {
            if (movement)
                ReleaseMovementInputLockLayer();

            if (rotation)
                ReleaseRotationInputLockLayer();
        }

        public void ReleaseCastInputLockLayer()
        {
            ReleaseMovementInputLockLayer();
            ReleaseRotationInputLockLayer();
        }

        public void ForceReleaseAllInputLocks()
        {
            _movementInputLockDepth = 0;
            _rotationInputLockDepth = 0;
            TryDeactivate(EntityStatePaths.Input.Movement);
            TryDeactivate(EntityStatePaths.Input.Rotation);
        }

        public void ReleaseInputLocks() => ForceReleaseAllInputLocks();

        private void ReleaseMovementInputLockLayer()
        {
            if (_movementInputLockDepth <= 0)
                return;

            if (--_movementInputLockDepth == 0)
                TryDeactivate(EntityStatePaths.Input.Movement);
        }

        private void ReleaseRotationInputLockLayer()
        {
            if (_rotationInputLockDepth <= 0)
                return;

            if (--_rotationInputLockDepth == 0)
                TryDeactivate(EntityStatePaths.Input.Rotation);
        }

        public void CompleteCastEffect(int castLifecycleId)
        {
            if (castLifecycleId <= 0)
                return;

            _castLifecycle?.CompletePendingEffect(castLifecycleId);
        }

        public void ScheduleHitReactExit(float delaySeconds)
        {
            _hitReactCts?.Cancel();
            _hitReactCts?.Dispose();
            _hitReactCts = new CancellationTokenSource();
            ExitHitReactAfterDelay(delaySeconds, _hitReactCts.Token).Forget();
        }

        private async UniTaskVoid ExitHitReactAfterDelay(float delaySeconds, CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: cancellationToken);
            if (IsInState(EntityStatePaths.Action.HitReact))
                TryTransition(EntityStatePaths.Action.None);
        }

        public bool TryDeactivate(StatePath path, in TransitionContext context = default) =>
            _runtime != null && _runtime.TryDeactivate(path, context);

        public void Tick(float deltaTime)
        {
            if (_activeStatuses.Count == 0)
                return;

            _runtime?.Tick(deltaTime);
        }

        public bool TickCastAnimationWait(string animationName, ref float elapsed, ref int phase, float deltaTime) =>
            _presenter != null &&
            _presenter.TickCastAnimationWait(animationName, ref elapsed, ref phase, deltaTime);

        public void PlayCastAnimation(string animationName) => _presenter?.PlayCastTrigger(animationName);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _hitReactCts?.Cancel();
            _hitReactCts?.Dispose();
            _hitReactCts = null;
            ReleasePresentationEffects();
            ForceReleaseAllInputLocks();
            _presenter?.Dispose();
            _runtime?.Dispose();
        }
    }
}
