using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Infrastructure;
using AvantajPrim.AbilitiesDemo.Domain;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    public sealed class AbilityCastLifecycleTests
    {
        [Test]
        public void WaitForCompletionAsync_CompletesImmediately_WhenNoPendingEffects()
        {
            var lifecycle = new AbilityCastLifecycle();
            int castId = lifecycle.BeginCast(new AbilityId(DemoConstants.AbilityIds.Healing), new EntityId(1));

            lifecycle.MarkExecutionFinished(castId);

            Assert.DoesNotThrow(() =>
                lifecycle.WaitForCompletionAsync(castId).GetAwaiter().GetResult());
        }

        [Test]
        public void WaitForCompletionAsync_WaitsUntilPendingEffectCompletes()
        {
            var lifecycle = new AbilityCastLifecycle();
            int castId = lifecycle.BeginCast(new AbilityId(DemoConstants.AbilityIds.Healing), new EntityId(1));
            lifecycle.RegisterPendingEffect(castId);
            lifecycle.MarkExecutionFinished(castId);

            UniTask waitTask = lifecycle.WaitForCompletionAsync(castId);
            Assert.IsFalse(waitTask.GetAwaiter().IsCompleted);

            lifecycle.CompletePendingEffect(castId);
            waitTask.GetAwaiter().GetResult();
        }

        [Test]
        public void ForceComplete_CompletesPendingCastImmediately()
        {
            var lifecycle = new AbilityCastLifecycle();
            int castId = lifecycle.BeginCast(new AbilityId(DemoConstants.AbilityIds.Healing), new EntityId(1));
            lifecycle.RegisterPendingEffect(castId);
            lifecycle.MarkExecutionFinished(castId);

            UniTask waitTask = lifecycle.WaitForCompletionAsync(castId);
            Assert.IsFalse(waitTask.GetAwaiter().IsCompleted);

            lifecycle.ForceComplete(castId);
            waitTask.GetAwaiter().GetResult();
        }
    }
}
