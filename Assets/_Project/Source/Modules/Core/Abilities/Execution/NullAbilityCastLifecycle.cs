using System.Threading;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using Cysharp.Threading.Tasks;

namespace AvantajPrim.Abilities.Execution
{
    public sealed class NullAbilityCastLifecycle : IAbilityCastLifecycle, IAbilityCastCompletionAwaiter
    {
        public static readonly NullAbilityCastLifecycle Instance = new NullAbilityCastLifecycle();

        private NullAbilityCastLifecycle() { }

        public int BeginCast(AbilityId abilityId, EntityId casterId) => 0;

        public void RegisterPendingEffect(int castId) { }

        public void CompletePendingEffect(int castId) { }

        public void MarkExecutionFinished(int castId) { }

        public void ForceComplete(int castId) { }

        public UniTask WaitForCompletionAsync(int castId, CancellationToken cancellationToken = default) =>
            UniTask.CompletedTask;
    }
}
