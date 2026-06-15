using System.Threading;
using Cysharp.Threading.Tasks;

namespace AvantajPrim.Abilities.Execution
{
    public interface IAbilityCastCompletionAwaiter
    {
        UniTask WaitForCompletionAsync(int castId, CancellationToken cancellationToken = default);
    }
}
