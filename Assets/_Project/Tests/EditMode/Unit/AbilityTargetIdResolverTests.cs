using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilityTargetIdResolverTests
    {
        [Test]
        public void Resolve_PlayerTarget_ReturnsCasterId()
        {
            var casterId = new EntityId(1);
            var targetId = new EntityId(2);
            AbilityExecutionContext context = CreateContext(casterId, targetId);

            EntityId resolved = AbilityTargetIdResolver.Resolve(AbilityTargetType.Player, context);

            Assert.AreEqual(casterId, resolved);
        }

        [Test]
        public void Resolve_EnemyTarget_ReturnsTargetId()
        {
            var casterId = new EntityId(1);
            var targetId = new EntityId(2);
            AbilityExecutionContext context = CreateContext(casterId, targetId);

            EntityId resolved = AbilityTargetIdResolver.Resolve(AbilityTargetType.Enemy, context);

            Assert.AreEqual(targetId, resolved);
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
