using System.Collections.Generic;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Execution.Executors;
using AvantajPrim.Tests.EditMode;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Integration
{
    [TestFixture]
    public sealed class DefencedAttackStatusIntegrationTests
    {
        [Test]
        public void DefencedAttackShape_AppliesFreezingAndBleedingOnTarget()
        {
            var entityState = new RecordingEntityStatePort();
            var presentation = new RecordingPresentationPort();
            var targetId = new EntityId(2);
            var context = new AbilityExecutionContext(
                new AbilityId("defenced_attack"),
                new EntityId(1),
                targetId,
                new AbilityEntityModel(new EntityId(1), isPlayer: true),
                new AbilityEntityModel(targetId, isPlayer: false));

            StatusEffectComponentExecutor.ApplyStatus(
                new StatusEffectComponentData
                {
                    EffectType = StatusEffectType.Freezing,
                    TargetType = AbilityTargetType.Enemy,
                    Duration = 2f,
                    Value = 3f,
                    TickInterval = 1f
                },
                context,
                presentation,
                entityState);

            StatusEffectComponentExecutor.ApplyStatus(
                new StatusEffectComponentData
                {
                    EffectType = StatusEffectType.Bleeding,
                    TargetType = AbilityTargetType.Enemy,
                    Duration = 2f,
                    Value = 3f,
                    TickInterval = 1f
                },
                context,
                presentation,
                entityState);

            var paths = new List<string>();
            for (int i = 0; i < entityState.Transitions.Count; i++)
                paths.Add(entityState.Transitions[i].Path.Full);

            Assert.Contains("Status.Freezing", paths);
            Assert.Contains("Status.Bleeding", paths);
            Assert.AreEqual(2, entityState.Transitions.Count);
        }
    }
}
