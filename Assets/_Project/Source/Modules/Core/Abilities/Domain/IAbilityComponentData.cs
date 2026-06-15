namespace AvantajPrim.Abilities.Domain
{
    public interface IAbilityComponentData
    {
        AbilityPlayTimeType PlayTimeType { get; }
        float DelaySeconds { get; }
    }
}
