using AvantajPrim.Abilities.Domain;
using Cysharp.Threading.Tasks;

namespace AvantajPrim.Abilities.Execution
{
    public interface IAbilityAnimationAwaiter
    {
        UniTask WaitForAnimationAsync(
            EntityId entityId,
            string animationName,
            System.Threading.CancellationToken cancellationToken = default);
    }
}
