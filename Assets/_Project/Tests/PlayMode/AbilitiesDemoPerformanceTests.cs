using System.Collections;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.AbilitiesDemo.Application;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AvantajPrim.Tests.PlayMode
{
    public sealed class AbilitiesDemoPerformanceTests
    {
        [UnityTest]
        public IEnumerator GradualCombatApplier_SoakTick_DoesNotThrow()
        {
            const int sampleFrames = 180;

            var registry = new DemoEntityRegistry();
            var entityStateRegistry = new EntityStateRegistry();
            var session = new DemoGameplaySession(new TargetingService(), new PlayerMovementService(registry, entityStateRegistry));
            session.SetActive(true);

            var combat = new DemoCombatRegistry();
            combat.Register(new EntityCombatState(new EntityId(2), "Enemy", 100f));

            var applier = new GradualCombatApplier(combat, session);
            applier.Enqueue(new DamageRequestedEvent(new EntityId(1), new EntityId(2), 40f, 8f, 4f, 1f));

            for (int i = 0; i < sampleFrames; i++)
            {
                applier.OnUpdate(0.016f);
                yield return null;
            }

            Assert.Pass();
        }
    }
}
