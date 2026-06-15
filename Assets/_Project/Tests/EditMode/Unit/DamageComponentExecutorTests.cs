using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Execution.Executors;
using AvantajPrim.Tests.EditMode;
using NUnit.Framework;
using StateMachine.Facade;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class DamageComponentExecutorTests
    {
        [Test]
        public void Execute_PublishesDamageAndHitReactTransition()
        {
            var executor = new DamageComponentExecutor();
            var presentation = new RecordingPresentationPort();
            var entityState = new RecordingEntityStatePort();
            var targetId = new EntityId(2);
            AbilityExecutionContext context = CreateContext(new EntityId(1), targetId);
            var data = new DamageComponentData { TotalValue = 15f };

            executor.Execute(data, context, presentation, entityState);

            Assert.AreEqual(1, presentation.DamageEvents.Count);
            Assert.AreEqual(15f, presentation.DamageEvents[0].TotalValue);
            Assert.AreEqual(targetId, presentation.DamageEvents[0].TargetId);
            Assert.IsTrue(entityState.Transitions.Exists(t =>
                t.Id.Equals(targetId) && t.Path.Equals(StatePath.Parse("Action.HitReact"))));
        }

        [Test]
        public void Execute_GradualDamage_PublishesTickPayload()
        {
            var executor = new DamageComponentExecutor();
            var presentation = new RecordingPresentationPort();
            AbilityExecutionContext context = CreateContext(new EntityId(1), new EntityId(2));
            var data = new DamageComponentData
            {
                TotalValue = 12f,
                TickValue = 3f,
                ApplicationDuration = 2f,
                TickInterval = 0.5f
            };
            Assert.IsTrue(data.IsGradual);

            executor.Execute(data, context, presentation, NullEntityStatePort.Instance);

            Assert.AreEqual(1, presentation.DamageEvents.Count);
            Assert.AreEqual(12f, presentation.DamageEvents[0].TotalValue);
            Assert.AreEqual(3f, presentation.DamageEvents[0].TickValue);
            Assert.AreEqual(2f, presentation.DamageEvents[0].ApplicationDuration);
        }

        private static AbilityExecutionContext CreateContext(EntityId casterId, EntityId targetId) =>
            new AbilityExecutionContext(
                new AbilityId("test"),
                casterId,
                targetId,
                new AbilityEntityModel(casterId, isPlayer: true),
                new AbilityEntityModel(targetId, isPlayer: false));
    }
}
