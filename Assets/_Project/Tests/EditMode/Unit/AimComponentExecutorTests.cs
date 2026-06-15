using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Execution.Executors;
using AvantajPrim.Tests.EditMode;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AimComponentExecutorTests
    {
        [Test]
        public void Execute_EnemyTarget_PublishesAimTowardTarget()
        {
            var executor = new AimComponentExecutor();
            var presentation = new RecordingPresentationPort();
            var casterId = new EntityId(1);
            var targetId = new EntityId(2);
            AbilityExecutionContext context = CreateContext(casterId, targetId);
            var data = new AimComponentData { TargetType = AbilityTargetType.Enemy };

            executor.Execute(data, context, presentation, null);

            Assert.AreEqual(1, presentation.AimIntents.Count);
            Assert.AreEqual(casterId, presentation.AimIntents[0].CasterId);
            Assert.AreEqual(targetId, presentation.AimIntents[0].TargetId);
            Assert.AreEqual(AbilityTargetType.Enemy, presentation.AimIntents[0].TargetType);
        }

        [Test]
        public void Execute_PlayerTarget_PublishesAimTowardCaster()
        {
            var executor = new AimComponentExecutor();
            var presentation = new RecordingPresentationPort();
            var casterId = new EntityId(1);
            var targetId = new EntityId(2);
            AbilityExecutionContext context = CreateContext(casterId, targetId);
            var data = new AimComponentData { TargetType = AbilityTargetType.Player };

            executor.Execute(data, context, presentation, null);

            Assert.AreEqual(casterId, presentation.AimIntents[0].TargetId);
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
