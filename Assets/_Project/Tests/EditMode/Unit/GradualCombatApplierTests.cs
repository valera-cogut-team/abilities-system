using System.IO;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Infrastructure;
using AvantajPrim.AbilitiesDemo.Application;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class GradualCombatApplierTests
    {
        private DemoCombatRegistry _combatRegistry;
        private DemoGameplaySession _session;
        private GradualCombatApplier _applier;

        [SetUp]
        public void SetUp()
        {
            _combatRegistry = new DemoCombatRegistry();
            _combatRegistry.Register(new EntityCombatState(new EntityId(2), "Enemy", 100f));
            var entityRegistry = new DemoEntityRegistry();
            var entityStateRegistry = new EntityStateRegistry();
            _session = new DemoGameplaySession(
                new TargetingService(),
                new PlayerMovementService(entityRegistry, entityStateRegistry));
            _session.SetActive(true);
            _applier = new GradualCombatApplier(_combatRegistry, _session);
        }

        [Test]
        public void OnUpdate_WhenSessionInactive_DoesNotApplyDamage()
        {
            _session.SetActive(false);
            _applier.Enqueue(new DamageRequestedEvent(
                new EntityId(1),
                new EntityId(2),
                totalValue: 15f,
                tickValue: 5f,
                applicationDuration: 2f,
                tickInterval: 0.5f));

            _applier.OnUpdate(0.5f);

            Assert.IsTrue(_combatRegistry.TryGet(new EntityId(2), out EntityCombatState combat));
            Assert.AreEqual(100f, combat.CurrentHealth);
        }

        [Test]
        public void OnUpdate_AppliesGradualDamageOverTicks()
        {
            _applier.Enqueue(new DamageRequestedEvent(
                new EntityId(1),
                new EntityId(2),
                totalValue: 15f,
                tickValue: 5f,
                applicationDuration: 2f,
                tickInterval: 0.5f));

            _applier.OnUpdate(0.5f);
            Assert.IsTrue(_combatRegistry.TryGet(new EntityId(2), out EntityCombatState combat1));
            Assert.AreEqual(95f, combat1.CurrentHealth);

            _applier.OnUpdate(0.5f);
            Assert.IsTrue(_combatRegistry.TryGet(new EntityId(2), out EntityCombatState combat2));
            Assert.AreEqual(90f, combat2.CurrentHealth);

            _applier.OnUpdate(0.5f);
            Assert.IsTrue(_combatRegistry.TryGet(new EntityId(2), out EntityCombatState combat3));
            Assert.AreEqual(85f, combat3.CurrentHealth);
        }

        [Test]
        public void ComputeTweenDuration_UsesTickInterval()
        {
            Assert.AreEqual(0.45f, GradualCombatApplier.ComputeTweenDuration(0.5f), 0.001f);
            Assert.AreEqual(0.25f, GradualCombatApplier.ComputeTweenDuration(0f), 0.001f);
        }

        [Test]
        public void CancelPendingForTarget_CompletesCastLifecycle()
        {
            var lifecycle = new AbilityCastLifecycle();
            int castId = lifecycle.BeginCast(new AbilityId("firewall"), new EntityId(1));
            lifecycle.RegisterPendingEffect(castId);
            lifecycle.MarkExecutionFinished(castId);

            _applier = new GradualCombatApplier(_combatRegistry, _session, lifecycle);
            _applier.Enqueue(new DamageRequestedEvent(
                new EntityId(1),
                new EntityId(2),
                totalValue: 15f,
                tickValue: 5f,
                applicationDuration: 2f,
                tickInterval: 0.5f,
                castLifecycleId: castId));

            _applier.CancelPendingForTarget(new EntityId(2));

            Assert.IsTrue(lifecycle.WaitForCompletionAsync(castId).GetAwaiter().IsCompleted);
        }
    }
}
