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
    public sealed class SoundComponentExecutorTests
    {
        [Test]
        public void Execute_PublishesSoundIntent()
        {
            var executor = new SoundComponentExecutor();
            var presentation = new RecordingPresentationPort();
            AbilityExecutionContext context = CreateContext(new EntityId(1));
            var data = TestAddressableRefs.CreateSoundComponent(volume: 0.8f);
            string expectedKey = data.ResolveClipKey();
            Assume.That(!string.IsNullOrEmpty(expectedKey), "SFX dash addressable entry is required for this test.");

            executor.Execute(data, context, presentation, null);

            Assert.AreEqual(1, presentation.SoundIntents.Count);
            Assert.AreEqual(expectedKey, presentation.SoundIntents[0].ClipKey);
            Assert.AreEqual(0.8f, presentation.SoundIntents[0].Volume);
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
