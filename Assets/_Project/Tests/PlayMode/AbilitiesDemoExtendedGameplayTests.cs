using System.Collections;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using AvantajPrim.AbilitiesDemo.Presentation;
using AvantajPrim.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AvantajPrim.Tests.PlayMode
{
    public sealed class AbilitiesDemoExtendedGameplayTests
    {
        [UnityTest]
        public IEnumerator EntityCombatState_ReachesZeroHealth()
        {
            var combat = new EntityCombatState(new EntityId(1), "Player", 50f);
            combat.ApplyDamage(50f);
            yield return null;
            Assert.AreEqual(0f, combat.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator EntityContactDamageService_DamagesOverlappingEntities()
        {
            var playerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var enemyGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerGo.transform.position = Vector3.zero;
            enemyGo.transform.position = new Vector3(0.5f, 0f, 0f);

            EntityView playerView = playerGo.AddComponent<EntityView>();
            playerView.Configure(new EntityId(1), isPlayer: true);
            EntityView enemyView = enemyGo.AddComponent<EntityView>();
            enemyView.Configure(new EntityId(2), isPlayer: false);

            var registry = new DemoEntityRegistry();
            registry.SetPlayer(new EntityId(1), playerView);
            registry.AddEnemy(new EntityId(2), enemyView);

            var combatRegistry = new DemoCombatRegistry();
            var playerCombat = new EntityCombatState(new EntityId(1), "Player", 100f);
            var enemyCombat = new EntityCombatState(new EntityId(2), "Enemy", 100f);
            combatRegistry.Register(playerCombat);
            combatRegistry.Register(enemyCombat);

            var entityStateRegistry = new EntityStateRegistry();
            var session = new DemoGameplaySession(new TargetingService(), new PlayerMovementService(registry, entityStateRegistry));
            session.SetActive(true);

            var contact = new EntityContactDamageService(registry, combatRegistry, entityStateRegistry, session);
            contact.OnUpdate(0f);

            yield return null;

            Assert.Less(playerCombat.CurrentHealth, 100f);
            Assert.Less(enemyCombat.CurrentHealth, 100f);

            Object.Destroy(playerGo);
            Object.Destroy(enemyGo);
        }

        [UnityTest]
        public IEnumerator TargetingService_ClearSelection_RemovesAllTargets()
        {
            var targeting = new TargetingService();
            targeting.SetSingleSelection(new EntityId(2));
            targeting.ToggleSelection(new EntityId(3));
            targeting.ClearSelection();
            yield return null;
            Assert.AreEqual(0, targeting.SelectedTargets.Count);
        }

        [UnityTest]
        public IEnumerator EntityStateMachineController_TransitionsToDeadAtZeroHealth()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            EntityView view = go.AddComponent<EntityView>();
            view.Configure(new EntityId(5), isPlayer: false);

            var entityStateRegistry = new EntityStateRegistry();
            EntityStateMachineController controller = TestPresentationFactory.CreateController(
                view,
                new EntityId(5),
                isPlayer: false,
                entityStateRegistry,
                new DemoCombatRegistry());

            var combat = new EntityCombatState(new EntityId(5), "Enemy", 20f);
            var registry = new DemoEntityRegistry();
            registry.AddEnemy(new EntityId(5), view);
            var wire = new CombatEntityFeedbackWire(combat, registry, entityStateRegistry, combatFeedback: null);
            combat.ApplyDamage(20f);

            yield return null;

            Assert.IsTrue(controller.IsInState(EntityStatePaths.Vitality.Dead));
            wire.Dispose();

            Object.Destroy(go);
        }
    }
}
