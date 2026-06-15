using AvantajPrim.Abilities.Domain;
using Cysharp.Threading.Tasks;

namespace AvantajPrim.Abilities.Execution
{
    public sealed class ImmediateAnimationAwaiter : IAbilityAnimationAwaiter
    {
        public static readonly ImmediateAnimationAwaiter Instance = new ImmediateAnimationAwaiter();

        private ImmediateAnimationAwaiter() { }

        public UniTask WaitForAnimationAsync(EntityId entityId, string animationName, System.Threading.CancellationToken cancellationToken = default) =>
            UniTask.CompletedTask;
    }
}
