using AvantajPrim.Abilities.Domain;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using AvantajPrim.Tests.EditMode;
using AvantajPrim.Tests;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using NUnit.Framework;
using StateMachine.Facade;
using UnityEngine;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class EntityStateMachineStatusTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (EntityView view in Object.FindObjectsByType<EntityView>(FindObjectsSortMode.None))
                Object.DestroyImmediate(view.gameObject);
        }

        [Test]
        public void FreezingStatus_BlocksWalking()
        {
            EntityStateMachineController controller = CreateInitializedController(out _);
            var payload = new StatusTransitionPayload(StatusEffectType.Freezing, 0f, 2f);

            controller.Runtime.TryTransition(EntityStatePaths.Status.Freezing, new TransitionContext(payload));

            Assert.IsTrue(controller.IsMovementBlocked);
            Assert.IsFalse(controller.Runtime.TryTransition(EntityStatePaths.Locomotion.Walking));

            controller.Runtime.TryDeactivate(EntityStatePaths.Status.Freezing);
            Assert.IsFalse(controller.IsMovementBlocked);
        }

        [Test]
        public void BleedingStatus_TicksDamage()
        {
            EntityStateMachineController controller = CreateInitializedController(out RecordingPresentationPort presentation);
            var payload = new StatusTransitionPayload(StatusEffectType.Bleeding, 7f, 3f, tickInterval: 0.5f);

            controller.Runtime.TryTransition(EntityStatePaths.Status.Bleeding, new TransitionContext(payload));
            controller.TickStatus(EntityStatePaths.Status.Bleeding, 0.6f);

            Assert.AreEqual(1, presentation.DamageEvents.Count);
            Assert.AreEqual(7f, presentation.DamageEvents[0].Value);
        }

        [Test]
        public void HealingStatus_RespectsTotalValueCap()
        {
            EntityStateMachineController controller = CreateInitializedController(out _, out DemoCombatRegistry combatRegistry, out EntityCombatState combat);
            combat.ApplyDamage(50f);

            var payload = new StatusTransitionPayload(StatusEffectType.Healing, 12f, 5f, tickInterval: 0.5f, totalValue: 20f);
            controller.Runtime.TryTransition(EntityStatePaths.Status.Healing, new TransitionContext(payload));

            controller.TickStatus(EntityStatePaths.Status.Healing, 0.6f);
            controller.TickStatus(EntityStatePaths.Status.Healing, 0.6f);

            Assert.AreEqual(70f, combat.CurrentHealth);
        }

        private static EntityStateMachineController CreateInitializedController(out RecordingPresentationPort presentation)
        {
            return CreateInitializedController(out presentation, out _, out _);
        }

        private static EntityStateMachineController CreateInitializedController(
            out RecordingPresentationPort presentation,
            out DemoCombatRegistry combatRegistry,
            out EntityCombatState combat)
        {
            presentation = new RecordingPresentationPort();
            var entityId = new EntityId(1);
            var go = new GameObject("EntityFsmStatus");
            EntityView view = go.AddComponent<EntityView>();
            view.Configure(entityId, isPlayer: true);

            var registry = new EntityStateRegistry();
            combatRegistry = new DemoCombatRegistry();
            combat = new EntityCombatState(entityId, "Player", 100f);
            combatRegistry.Register(combat);

            return TestPresentationFactory.CreateController(
                view,
                entityId,
                isPlayer: true,
                registry,
                combatRegistry,
                presentation);
        }
    }
}
