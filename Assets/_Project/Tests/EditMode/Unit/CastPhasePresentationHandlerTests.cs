using AvantajPrim.Abilities.Domain;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using AvantajPrim.Tests;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using NUnit.Framework;
using UnityEngine;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class CastPhasePresentationHandlerTests
    {
        [Test]
        public void HandlePhaseChanged_Complete_ReleasesInputLockLayer()
        {
            var registry = new EntityStateRegistry();
            var combatRegistry = new DemoCombatRegistry();
            EntityView view = CreateView(new EntityId(1), isPlayer: true);
            IEntityStateMachineController controller = TestPresentationFactory.CreateController(
                view,
                new EntityId(1),
                isPlayer: true,
                registry,
                combatRegistry);

            controller.AcquireCastInputLock(movement: true, rotation: true);
            Assert.IsTrue(controller.IsMovementBlocked);

            var handler = new CastPhasePresentationHandler(registry);
            handler.HandlePhaseChanged(new AbilityPhaseChangedEvent(
                new AbilityId("firewall"),
                new EntityId(1),
                AbilityConstants.Phases.Start));
            handler.HandlePhaseChanged(new AbilityPhaseChangedEvent(
                new AbilityId("firewall"),
                new EntityId(1),
                AbilityConstants.Phases.End));
            handler.HandlePhaseChanged(new AbilityPhaseChangedEvent(
                new AbilityId("firewall"),
                new EntityId(1),
                AbilityConstants.Phases.Complete));

            Assert.IsFalse(controller.IsMovementBlocked);
            Assert.IsFalse(controller.IsRotationBlocked);
        }

        [Test]
        public void HandlePhaseChanged_ParallelCasts_OnlyReleasesLockWhenAllComplete()
        {
            var registry = new EntityStateRegistry();
            var combatRegistry = new DemoCombatRegistry();
            EntityView view = CreateView(new EntityId(1), isPlayer: true);
            IEntityStateMachineController controller = TestPresentationFactory.CreateController(
                view,
                new EntityId(1),
                isPlayer: true,
                registry,
                combatRegistry);

            var handler = new CastPhasePresentationHandler(registry);
            var casterId = new EntityId(1);

            handler.HandlePhaseChanged(new AbilityPhaseChangedEvent(new AbilityId("a"), casterId, AbilityConstants.Phases.Start));
            handler.HandlePhaseChanged(new AbilityPhaseChangedEvent(new AbilityId("b"), casterId, AbilityConstants.Phases.Start));
            controller.AcquireCastInputLock(movement: true, rotation: false);
            controller.AcquireCastInputLock(movement: true, rotation: false);

            handler.HandlePhaseChanged(new AbilityPhaseChangedEvent(new AbilityId("a"), casterId, AbilityConstants.Phases.End));
            handler.HandlePhaseChanged(new AbilityPhaseChangedEvent(new AbilityId("a"), casterId, AbilityConstants.Phases.Complete));
            Assert.IsTrue(controller.IsMovementBlocked);

            handler.HandlePhaseChanged(new AbilityPhaseChangedEvent(new AbilityId("b"), casterId, AbilityConstants.Phases.End));
            handler.HandlePhaseChanged(new AbilityPhaseChangedEvent(new AbilityId("b"), casterId, AbilityConstants.Phases.Complete));
            Assert.IsFalse(controller.IsMovementBlocked);
        }

        private static EntityView CreateView(EntityId id, bool isPlayer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            EntityView view = go.AddComponent<EntityView>();
            view.Configure(id, isPlayer);
            return view;
        }
    }
}
