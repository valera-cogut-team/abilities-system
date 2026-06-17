using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Execution.Executors;
using AvantajPrim.Abilities.Infrastructure;
using AvantajPrim.Tests.EditMode;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class MovementComponentExecutorTests
    {
        [Test]
        public void Execute_TransitionsDisplacedState()
        {
            var executor = new MovementComponentExecutor();
            var entityState = new RecordingEntityStatePort();
            var casterId = new EntityId(1);
            AbilityExecutionContext context = CreateContext(casterId);
            var data = new MovementComponentData { OffsetX = 1f, OffsetY = 0f, OffsetZ = 2f, Duration = 0.5f };

            var presentation = new RecordingPresentationPort();
            executor.ExecuteAsync(data, context, presentation, entityState).GetAwaiter().GetResult();

            Assert.AreEqual(1, presentation.MovementIntents.Count);
            Assert.AreEqual(1f, presentation.MovementIntents[0].OffsetX);
            Assert.AreEqual(1, entityState.Transitions.Count);
            Assert.AreEqual("Locomotion.Displaced", entityState.Transitions[0].Path.Full);
            Assert.AreEqual(casterId, entityState.Transitions[0].Id);
        }

        [Test]
        public void Execute_WhenDisplacedTransitionFails_DoesNotRegisterPendingEffect()
        {
            var lifecycle = new AbilityCastLifecycle();
            int castId = lifecycle.BeginCast(new AbilityId("dash"), new EntityId(1));
            var executor = new MovementComponentExecutor(lifecycle);
            var entityState = new FailingEntityStatePort();
            var context = new AbilityExecutionContext(
                new AbilityId("dash"),
                new EntityId(1),
                new EntityId(2),
                new AbilityEntityModel(new EntityId(1), isPlayer: true),
                new AbilityEntityModel(new EntityId(2), isPlayer: false),
                castLifecycleId: castId);
            var data = new MovementComponentData { OffsetX = 1f, Duration = 0.5f };

            executor.ExecuteAsync(data, context, new RecordingPresentationPort(), entityState).GetAwaiter().GetResult();

            lifecycle.MarkExecutionFinished(castId);
            Assert.IsTrue(lifecycle.WaitForCompletionAsync(castId).GetAwaiter().IsCompleted);
        }

        private static AbilityExecutionContext CreateContext(EntityId casterId) =>
            new AbilityExecutionContext(
                new AbilityId("test"),
                casterId,
                new EntityId(2),
                new AbilityEntityModel(casterId, isPlayer: true),
                new AbilityEntityModel(new EntityId(2), isPlayer: false));
    }
}
