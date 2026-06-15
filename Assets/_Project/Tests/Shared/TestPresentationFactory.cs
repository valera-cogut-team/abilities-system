using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Presentation;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using StateMachine.Application;
using StateMachine.Facade;
using UnityEngine;

namespace AvantajPrim.Tests
{
    public static class TestPresentationFactory
    {
        public static IEntityStateMachineController CreateFsmDefinitionStub() => new FsmDefinitionStubController();

        public static EntityStateMachineController CreateController(
            EntityView view,
            EntityId entityId,
            bool isPlayer,
            EntityStateRegistry registry = null,
            DemoCombatRegistry combatRegistry = null,
            IAbilityPresentationPort presentation = null)
        {
            return new EntityStateMachineController(
                entityId,
                view,
                isPlayer,
                new StateMachineFacade(new StateMachineService()),
                registry ?? new EntityStateRegistry(),
                combatRegistry,
                presentation,
                castLifecycle: null,
                effects: null,
                addressables: null,
                entityVfxRegistry: null);
        }

        public static (TargetSelectionIndicator Indicator, GameObject Host) CreateTargetIndicator()
        {
            var go = new GameObject("TargetSelectionIndicator");
            return (new TargetSelectionIndicator(go.transform, logger: null), go);
        }
    }

    public sealed class FsmDefinitionStubController : IEntityStateMachineController
    {
        public EntityId EntityId => default;
        public IStateMachineRuntime Runtime => null;
        public bool IsMovementBlocked => false;
        public bool IsRotationBlocked => false;

        public void OnStatusEnter(StatePath path, TransitionContext context) { }
        public void OnStatusExit(StatePath path) { }
        public void TickStatus(StatePath path, float deltaTime) { }
        public bool IsInState(StatePath path) => false;
        public bool CanTransition(StatePath path, in TransitionContext context = default) => true;
        public bool TryTransition(StatePath path, in TransitionContext context = default) => true;
        public void ReleasePresentationEffects() { }
        public void AcquireCastInputLock(bool movement, bool rotation) { }
        public void ReleaseCastInputLock(bool movement, bool rotation) { }
        public void ReleaseCastInputLockLayer() { }
        public void ForceReleaseAllInputLocks() { }
        public void ReleaseInputLocks() { }
        public void CompleteCastEffect(int castLifecycleId) { }
        public void ScheduleHitReactExit(float delaySeconds) { }
        public bool TryDeactivate(StatePath path, in TransitionContext context = default) => true;
        public void Tick(float deltaTime) { }
        public bool TickCastAnimationWait(string animationName, ref float elapsed, ref int phase, float deltaTime) => false;
        public void PlayCastAnimation(string animationName) { }
        public void Dispose() { }
    }
}
