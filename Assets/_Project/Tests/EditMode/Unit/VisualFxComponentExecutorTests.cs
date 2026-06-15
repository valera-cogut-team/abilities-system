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
    public sealed class VisualFxComponentExecutorTests
    {
        [Test]
        public void Execute_PlayerTarget_PublishesVfxForCaster()
        {
            var executor = new VisualFxComponentExecutor();
            var presentation = new RecordingPresentationPort();
            var casterId = new EntityId(1);
            var targetId = new EntityId(2);
            AbilityExecutionContext context = CreateContext(casterId, targetId);
            var data = TestAddressableRefs.CreateVisualFxComponent(targetType: AbilityTargetType.Player);
            string expectedKey = data.ResolveVfxKey();
            Assume.That(!string.IsNullOrEmpty(expectedKey), "VFX dash addressable entry is required for this test.");

            executor.Execute(data, context, presentation, null);

            Assert.AreEqual(1, presentation.VfxIntents.Count);
            Assert.AreEqual(casterId, presentation.VfxIntents[0].TargetId);
            Assert.AreEqual(expectedKey, presentation.VfxIntents[0].PrefabKey);
        }

        [Test]
        public void Execute_EnemyTarget_PublishesVfxForTarget()
        {
            var executor = new VisualFxComponentExecutor();
            var presentation = new RecordingPresentationPort();
            var casterId = new EntityId(1);
            var targetId = new EntityId(2);
            AbilityExecutionContext context = CreateContext(casterId, targetId);
            var data = TestAddressableRefs.CreateVisualFxComponent();
            string expectedKey = data.ResolveVfxKey();
            Assume.That(!string.IsNullOrEmpty(expectedKey), "VFX dash addressable entry is required for this test.");

            executor.Execute(data, context, presentation, null);

            Assert.AreEqual(1, presentation.VfxIntents.Count);
            Assert.AreEqual(targetId, presentation.VfxIntents[0].TargetId);
            Assert.AreEqual(expectedKey, presentation.VfxIntents[0].PrefabKey);
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
