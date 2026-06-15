using System.Collections.Generic;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Execution.Executors;
using AvantajPrim.Tests.EditMode;
using AvantajPrim.Tests.Shared;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilityExecutorWaitUntilEndTests
    {
        [Test]
        public void ExecuteAsync_WaitUntilEnd_AwaitsAnimationBeforeNextComponent()
        {
            var registry = new AbilityComponentRegistry();
            registry.Register(new AnimationComponentExecutor());
            registry.Register(new DamageComponentExecutor());

            var presentation = new RecordingPresentationPort();
            var awaiter = new RecordingAnimationAwaiter();
            var executor = new AbilityExecutor(registry, presentation, NullEntityStatePort.Instance, awaiter);

            var casterId = new EntityId(1);
            var targetId = new EntityId(2);
            var context = new AbilityExecutionContext(
                new AbilityId("test"),
                casterId,
                targetId,
                new AbilityEntityModel(casterId, isPlayer: true),
                new AbilityEntityModel(targetId, isPlayer: false));

            var components = new List<IAbilityComponentData>
            {
                TestAddressableRefs.CreateAnimationComponent(
                    TestAddressableRefs.AnimDashPath,
                    waitUntilEnd: true),
                new DamageComponentData { PlayTimeType = AbilityPlayTimeType.Delay, DelaySeconds = 0f, TotalValue = 10f }
            };

            string expectedAnimationName = ((AnimationComponentData)components[0]).ResolveAnimationName();
            Assume.That(!string.IsNullOrEmpty(expectedAnimationName), "Cast_Dash.anim addressable entry is required for this test.");

            executor.ExecuteAsync(new AbilityDefinition(new AbilityId("test"), components), context)
                .GetAwaiter().GetResult();

            Assert.AreEqual(1, awaiter.Calls.Count);
            Assert.AreEqual(casterId, awaiter.Calls[0].EntityId);
            Assert.AreEqual(expectedAnimationName, awaiter.Calls[0].AnimationName);
            Assert.AreEqual(1, presentation.DamageEvents.Count);
        }

        private sealed class RecordingAnimationAwaiter : IAbilityAnimationAwaiter
        {
            public readonly List<(EntityId EntityId, string AnimationName)> Calls =
                new List<(EntityId, string)>();

            public UniTask WaitForAnimationAsync(
                EntityId entityId,
                string animationName,
                System.Threading.CancellationToken cancellationToken = default)
            {
                Calls.Add((entityId, animationName));
                return UniTask.CompletedTask;
            }
        }
    }
}
