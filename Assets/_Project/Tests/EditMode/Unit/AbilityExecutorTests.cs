using System.Collections.Generic;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Execution.Executors;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilityExecutorTests
    {
        [Test]
        public void ExecuteAsync_RunsOnStartComponents()
        {
            var registry = new AbilityComponentRegistry();
            registry.Register(new DamageComponentExecutor());

            var presentation = new RecordingPresentationPort();
            var executor = new AbilityExecutor(registry, presentation, NullEntityStatePort.Instance);

            var casterId = new EntityId(1);
            var targetId = new EntityId(2);
            var caster = new AbilityEntityModel(casterId, isPlayer: true);
            var target = new AbilityEntityModel(targetId, isPlayer: false);
            var context = new AbilityExecutionContext(new AbilityId("test"), casterId, targetId, caster, target);

            var components = new List<IAbilityComponentData>
            {
                new DamageComponentData { PlayTimeType = AbilityPlayTimeType.OnStart, TotalValue = 42f }
            };
            var definition = new AbilityDefinition(new AbilityId("test"), components);

            executor.ExecuteAsync(definition, context, System.Threading.CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, presentation.DamageEvents.Count);
            Assert.AreEqual(42f, presentation.DamageEvents[0].Value);
            Assert.AreEqual(casterId, presentation.DamageEvents[0].SourceId);
            Assert.AreEqual(targetId, presentation.DamageEvents[0].TargetId);
        }

        [Test]
        public void ExecuteAsync_SkipsOnStart_WhenExecutorMissing()
        {
            var registry = new AbilityComponentRegistry();
            var presentation = new RecordingPresentationPort();
            var executor = new AbilityExecutor(registry, presentation, NullEntityStatePort.Instance);

            var casterId = new EntityId(1);
            var targetId = new EntityId(2);
            var context = new AbilityExecutionContext(
                new AbilityId("test"), casterId, targetId,
                new AbilityEntityModel(casterId, true),
                new AbilityEntityModel(targetId, false));

            var components = new List<IAbilityComponentData>
            {
                new DamageComponentData { PlayTimeType = AbilityPlayTimeType.OnStart, TotalValue = 10f }
            };

            executor.ExecuteAsync(new AbilityDefinition(new AbilityId("test"), components), context).GetAwaiter().GetResult();

            Assert.AreEqual(0, presentation.DamageEvents.Count);
        }
    }
}
