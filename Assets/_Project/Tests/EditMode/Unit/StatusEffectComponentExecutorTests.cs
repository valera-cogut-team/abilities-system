using System.Collections.Generic;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Execution.Executors;
using AvantajPrim.Abilities.Infrastructure;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class StatusEffectComponentExecutorTests
    {
        [Test]
        public void ApplyStatus_PlayerTarget_AppliesToCaster()
        {
            var entityState = new RecordingEntityStatePort();
            var casterId = new EntityId(1);
            var targetId = new EntityId(2);
            var data = new StatusEffectComponentData
            {
                EffectType = StatusEffectType.Healing,
                TargetType = AbilityTargetType.Player,
                Duration = 2f,
                Value = 5f
            };
            AbilityExecutionContext context = CreateContext(casterId, targetId);

            StatusEffectComponentExecutor.ApplyStatus(data, context, new RecordingPresentationPort(), entityState);

            Assert.AreEqual(1, entityState.Transitions.Count);
            Assert.AreEqual(casterId, entityState.Transitions[0].Id);
        }

        [TestCase(StatusEffectType.Combustion, "Status.Combustion")]
        [TestCase(StatusEffectType.Freezing, "Status.Freezing")]
        [TestCase(StatusEffectType.Healing, "Status.Healing")]
        [TestCase(StatusEffectType.Bleeding, "Status.Bleeding")]
        public void ApplyStatus_MapsEffectTypeToFsmPath(StatusEffectType effectType, string expectedPath)
        {
            var entityState = new RecordingEntityStatePort();
            var targetId = new EntityId(2);
            var data = new StatusEffectComponentData
            {
                EffectType = effectType,
                TargetType = AbilityTargetType.Enemy,
                Duration = 2f,
                Value = 5f
            };
            AbilityExecutionContext context = CreateContext(new EntityId(1), targetId);

            StatusEffectComponentExecutor.ApplyStatus(data, context, new RecordingPresentationPort(), entityState);

            Assert.AreEqual(1, entityState.Transitions.Count);
            Assert.AreEqual(expectedPath, entityState.Transitions[0].Path.Full);
            Assert.AreEqual(targetId, entityState.Transitions[0].Id);
        }

        [Test]
        public void ApplyStatus_WhenTransitionFails_DoesNotRegisterPendingEffect()
        {
            var lifecycle = new AbilityCastLifecycle();
            int castId = lifecycle.BeginCast(new AbilityId("firewall"), new EntityId(1));
            var entityState = new FailingEntityStatePort();
            var data = new StatusEffectComponentData
            {
                EffectType = StatusEffectType.Combustion,
                TargetType = AbilityTargetType.Enemy,
                Duration = 4f,
                Value = 8f
            };
            var context = new AbilityExecutionContext(
                new AbilityId("firewall"),
                new EntityId(1),
                new EntityId(2),
                new AbilityEntityModel(new EntityId(1), isPlayer: true),
                new AbilityEntityModel(new EntityId(2), isPlayer: false),
                castLifecycleId: castId);

            StatusEffectComponentExecutor.ApplyStatus(
                data,
                context,
                new RecordingPresentationPort(),
                entityState,
                lifecycle);

            lifecycle.MarkExecutionFinished(castId);
            Assert.IsTrue(lifecycle.WaitForCompletionAsync(castId).GetAwaiter().IsCompleted);
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
