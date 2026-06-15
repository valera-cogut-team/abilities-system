using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Execution.Executors;
using AvantajPrim.Tests.EditMode;
using AvantajPrim.Tests.Shared;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AnimationComponentExecutorTests
    {
        [Test]
        public void Execute_EmptyClip_DoesNothing()
        {
            var executor = new AnimationComponentExecutor();
            var presentation = new RecordingPresentationPort();
            var entityState = new RecordingEntityStatePort();
            AbilityExecutionContext context = CreateContext(new EntityId(1));
            var data = new AnimationComponentData();

            executor.Execute(data, context, presentation, entityState);

            Assert.AreEqual(0, entityState.Transitions.Count);
            Assert.AreEqual(0, presentation.AnimationIntents.Count);
        }

        [Test]
        public void Execute_TransitionsCastingStateAndPublishesAnimation()
        {
            var executor = new AnimationComponentExecutor();
            var presentation = new RecordingPresentationPort();
            var entityState = new RecordingEntityStatePort();
            var casterId = new EntityId(1);
            AbilityExecutionContext context = CreateContext(casterId);
            var data = TestAddressableRefs.CreateAnimationComponent(
                TestAddressableRefs.AnimDashPath,
                waitUntilEnd: true);
            string animationName = data.ResolveAnimationName();
            Assume.That(!string.IsNullOrEmpty(animationName), "Cast_Dash.anim addressable entry is required for this test.");

            executor.Execute(data, context, presentation, entityState);

            Assert.AreEqual(1, entityState.Transitions.Count);
            Assert.AreEqual($"Action.Casting.{animationName}", entityState.Transitions[0].Path.Full);
            Assert.AreEqual(1, presentation.AnimationIntents.Count);
            Assert.AreEqual(animationName, presentation.AnimationIntents[0].AnimationName);
            Assert.IsTrue(presentation.AnimationIntents[0].WaitUntilEnd);
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
