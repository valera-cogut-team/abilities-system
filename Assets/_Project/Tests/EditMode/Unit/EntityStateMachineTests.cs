using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using AvantajPrim.Tests;
using NUnit.Framework;
using StateMachine.Application;
using StateMachine.Facade;

namespace AvantajPrim.Tests.EditMode.Unit
{
    public sealed class EntityStateMachineTests
    {
        [Test]
        public void PlayerDefinition_StartsAliveIdleAndActionNone()
        {
            EntityAnimationPresenter presenter = CreatePresenterStub();
            IEntityStateMachineController controller = TestPresentationFactory.CreateFsmDefinitionStub();
            IStateMachineDefinition definition = EntityStateMachineFactory.CreateDefinition(isPlayer: true, presenter, controller);
            using IStateMachineRuntime runtime = definition.CreateRuntime();

            Assert.That(runtime.IsInState(EntityStatePaths.Vitality.Alive), Is.True);
            Assert.That(runtime.IsInState(EntityStatePaths.Locomotion.Idle), Is.True);
            Assert.That(runtime.IsInState(EntityStatePaths.Action.None), Is.True);
        }

        [Test]
        public void CastFlow_TransitionsToCastingAndBack()
        {
            EntityAnimationPresenter presenter = CreatePresenterStub();
            IEntityStateMachineController controller = TestPresentationFactory.CreateFsmDefinitionStub();
            IStateMachineDefinition definition = EntityStateMachineFactory.CreateDefinition(isPlayer: true, presenter, controller);
            using IStateMachineRuntime runtime = definition.CreateRuntime();

            Assert.That(runtime.TryTransition(EntityStatePaths.Action.Casting("Dash")), Is.True);
            Assert.That(runtime.IsInState(EntityStatePaths.Action.Casting("Dash")), Is.True);
            Assert.That(runtime.TryTransition(EntityStatePaths.Action.None), Is.True);
        }

        [Test]
        public void DynamicCasting_AllowsUnregisteredAnimationName()
        {
            EntityAnimationPresenter presenter = CreatePresenterStub();
            IEntityStateMachineController controller = TestPresentationFactory.CreateFsmDefinitionStub();
            IStateMachineDefinition definition = EntityStateMachineFactory.CreateDefinition(isPlayer: true, presenter, controller);
            using IStateMachineRuntime runtime = definition.CreateRuntime();

            Assert.That(runtime.TryTransition(EntityStatePaths.Action.Casting("SuperNova")), Is.True);
            Assert.That(runtime.IsInState(EntityStatePaths.Action.Casting("SuperNova")), Is.True);
        }

        [Test]
        public void MovementLock_BlocksWalking_AllowsCasting()
        {
            EntityAnimationPresenter presenter = CreatePresenterStub();
            IEntityStateMachineController controller = TestPresentationFactory.CreateFsmDefinitionStub();
            IStateMachineDefinition definition = EntityStateMachineFactory.CreateDefinition(isPlayer: true, presenter, controller);
            using IStateMachineRuntime runtime = definition.CreateRuntime();

            runtime.TryTransition(EntityStatePaths.Input.Movement);
            Assert.That(runtime.TryTransition(EntityStatePaths.Locomotion.Walking), Is.False);
            Assert.That(runtime.TryTransition(EntityStatePaths.Action.Casting("Dash")), Is.True);
        }

        [Test]
        public void RotationLock_AllowsWalking()
        {
            EntityAnimationPresenter presenter = CreatePresenterStub();
            IEntityStateMachineController controller = TestPresentationFactory.CreateFsmDefinitionStub();
            IStateMachineDefinition definition = EntityStateMachineFactory.CreateDefinition(isPlayer: true, presenter, controller);
            using IStateMachineRuntime runtime = definition.CreateRuntime();

            runtime.TryTransition(EntityStatePaths.Input.Rotation);
            Assert.That(runtime.TryTransition(EntityStatePaths.Locomotion.Walking), Is.True);
        }

        [Test]
        public void ParallelStatus_AllowsCombustionAndWalking()
        {
            EntityAnimationPresenter presenter = CreatePresenterStub();
            IEntityStateMachineController controller = TestPresentationFactory.CreateFsmDefinitionStub();
            IStateMachineDefinition definition = EntityStateMachineFactory.CreateDefinition(isPlayer: true, presenter, controller);
            using IStateMachineRuntime runtime = definition.CreateRuntime();

            runtime.TryTransition(EntityStatePaths.Locomotion.Walking);
            runtime.TryTransition(EntityStatePaths.Status.Combustion);

            Assert.That(runtime.IsInState(EntityStatePaths.Locomotion.Walking), Is.True);
            Assert.That(runtime.IsInState(EntityStatePaths.Status.Combustion), Is.True);
        }

        [Test]
        public void Death_BlocksLocomotion()
        {
            EntityAnimationPresenter presenter = CreatePresenterStub();
            IEntityStateMachineController controller = TestPresentationFactory.CreateFsmDefinitionStub();
            IStateMachineDefinition definition = EntityStateMachineFactory.CreateDefinition(isPlayer: true, presenter, controller);
            using IStateMachineRuntime runtime = definition.CreateRuntime();

            runtime.TryTransition(EntityStatePaths.Vitality.Dead);
            Assert.That(runtime.TryTransition(EntityStatePaths.Locomotion.Walking), Is.False);
        }

        [Test]
        public void CastingWildcard_AllowsWalkingWithoutMovementLock()
        {
            EntityAnimationPresenter presenter = CreatePresenterStub();
            IEntityStateMachineController controller = TestPresentationFactory.CreateFsmDefinitionStub();
            IStateMachineDefinition definition = EntityStateMachineFactory.CreateDefinition(isPlayer: true, presenter, controller);
            using IStateMachineRuntime runtime = definition.CreateRuntime();

            runtime.TryTransition(EntityStatePaths.Action.Casting("DefencedAttack"));
            Assert.That(runtime.IsInState(EntityStatePaths.Action.CastingWildcard), Is.True);
            Assert.That(EntityStateGuards.NotMovementLocked(
                EntityStatePaths.Locomotion.Idle,
                EntityStatePaths.Locomotion.Walking,
                runtime,
                default), Is.True);
            Assert.That(runtime.TryTransition(EntityStatePaths.Locomotion.Walking), Is.True);
        }

        [Test]
        public void CastInputLockLayer_ReleasesOnlyAfterAllCastLayersEnd()
        {
            var go = new UnityEngine.GameObject("EntityFsmInputLock");
            EntityView view = go.AddComponent<EntityView>();
            view.Configure(new AvantajPrim.Abilities.Domain.EntityId(1), isPlayer: true);
            EntityStateMachineController controller = TestPresentationFactory.CreateController(view, new AvantajPrim.Abilities.Domain.EntityId(1), isPlayer: true);

            controller.AcquireCastInputLock(movement: true, rotation: true);
            controller.AcquireCastInputLock(movement: true, rotation: true);
            Assert.That(controller.Runtime.IsInState(EntityStatePaths.Input.Movement), Is.True);

            controller.ReleaseCastInputLockLayer();
            Assert.That(controller.Runtime.IsInState(EntityStatePaths.Input.Movement), Is.True);

            controller.ReleaseCastInputLockLayer();
            Assert.That(controller.Runtime.IsInState(EntityStatePaths.Input.Movement), Is.False);

            controller.Dispose();
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Casting_AllowsTransitionFromOneCastToAnother()
        {
            EntityAnimationPresenter presenter = CreatePresenterStub();
            IEntityStateMachineController controller = TestPresentationFactory.CreateFsmDefinitionStub();
            IStateMachineDefinition definition = EntityStateMachineFactory.CreateDefinition(isPlayer: true, presenter, controller);
            using IStateMachineRuntime runtime = definition.CreateRuntime();

            Assert.That(runtime.TryTransition(EntityStatePaths.Action.Casting("Dash")), Is.True);
            Assert.That(runtime.TryTransition(EntityStatePaths.Action.Casting("Firewall")), Is.True);
            Assert.That(runtime.IsInState(EntityStatePaths.Action.Casting("Firewall")), Is.True);
        }

        private static EntityAnimationPresenter CreatePresenterStub()
        {
            var go = new UnityEngine.GameObject("EntityViewStub");
            EntityView view = go.AddComponent<EntityView>();
            view.Configure(new AvantajPrim.Abilities.Domain.EntityId(1), isPlayer: true);
            return new EntityAnimationPresenter(view);
        }
    }
}
