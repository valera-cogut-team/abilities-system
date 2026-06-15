using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using Cysharp.Threading.Tasks;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class EntityAnimationAwaiter : IAbilityAnimationAwaiter
    {
        private readonly CastAnimationWaitService _waitService;

        public EntityAnimationAwaiter(CastAnimationWaitService waitService)
        {
            _waitService = waitService;
        }

        public UniTask WaitForAnimationAsync(
            EntityId entityId,
            string animationName,
            System.Threading.CancellationToken cancellationToken = default) =>
            _waitService.WaitAsync(entityId, animationName, cancellationToken);
    }
}
