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
    public sealed class LockInputComponentExecutorTests
    {
        [Test]
        public void Execute_BlockMovement_TransitionsInputMovement()
        {
            var executor = new LockInputComponentExecutor();
            var entityState = new RecordingEntityStatePort();
            var casterId = new EntityId(1);
            AbilityExecutionContext context = CreateContext(casterId);
            var data = new LockInputComponentData { BlockMovement = true };

            executor.ExecuteAsync(data, context, new RecordingPresentationPort(), entityState).GetAwaiter().GetResult();

            Assert.AreEqual(1, entityState.Transitions.Count);
            Assert.AreEqual("Input.Movement", entityState.Transitions[0].Path.Full);
            Assert.AreEqual(casterId, entityState.Transitions[0].Id);
        }

        [Test]
        public void Execute_BlockRotation_TransitionsInputRotation()
        {
            var executor = new LockInputComponentExecutor();
            var entityState = new RecordingEntityStatePort();
            var casterId = new EntityId(1);
            AbilityExecutionContext context = CreateContext(casterId);
            var data = new LockInputComponentData { BlockMovement = false, BlockRotation = true };

            executor.ExecuteAsync(data, context, new RecordingPresentationPort(), entityState).GetAwaiter().GetResult();

            Assert.AreEqual(1, entityState.Transitions.Count);
            Assert.AreEqual("Input.Rotation", entityState.Transitions[0].Path.Full);
        }

        [Test]
        public void Execute_BlockRotationOnly_DoesNotTransitionInputMovement()
        {
            var executor = new LockInputComponentExecutor();
            var entityState = new RecordingEntityStatePort();
            var casterId = new EntityId(1);
            AbilityExecutionContext context = CreateContext(casterId);
            var data = new LockInputComponentData { BlockMovement = false, BlockRotation = true };

            executor.ExecuteAsync(data, context, new RecordingPresentationPort(), entityState).GetAwaiter().GetResult();

            Assert.AreEqual(1, entityState.Transitions.Count);
            Assert.AreEqual("Input.Rotation", entityState.Transitions[0].Path.Full);
            Assert.IsFalse(entityState.IsInState(casterId, AbilityStatePaths.InputMovement));
        }

        [Test]
        public void Execute_BlockNeither_DoesNotAcquireInputLock()
        {
            var executor = new LockInputComponentExecutor();
            var entityState = new RecordingEntityStatePort();
            AbilityExecutionContext context = CreateContext(new EntityId(1));
            var data = new LockInputComponentData { BlockMovement = false, BlockRotation = false };

            executor.ExecuteAsync(data, context, new RecordingPresentationPort(), entityState).GetAwaiter().GetResult();

            Assert.AreEqual(0, entityState.Transitions.Count);
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
