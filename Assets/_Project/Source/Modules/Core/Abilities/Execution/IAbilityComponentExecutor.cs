using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;

namespace AvantajPrim.Abilities.Execution
{
    public interface IAbilityComponentExecutor
    {
        System.Type DataType { get; }
        void Execute(IAbilityComponentData data, AbilityExecutionContext context,
            IAbilityPresentationPort presentation, IEntityStatePort entityState);
    }
}
