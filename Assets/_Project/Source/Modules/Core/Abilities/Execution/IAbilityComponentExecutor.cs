using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using Cysharp.Threading.Tasks;

namespace AvantajPrim.Abilities.Execution
{
    public interface IAbilityComponentExecutor
    {
        System.Type DataType { get; }

        /// <summary>
        /// Executes the component. Returns a UniTask for async operations.
        /// Default synchronous behavior can use UniTask.CompletedTask.
        /// </summary>
        UniTask ExecuteAsync(IAbilityComponentData data, AbilityExecutionContext context,
            IAbilityPresentationPort presentation, IEntityStatePort entityState);
    }
}
