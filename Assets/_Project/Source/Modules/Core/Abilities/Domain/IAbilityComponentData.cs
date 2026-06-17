namespace AvantajPrim.Abilities.Domain
{
    public interface IAbilityComponentData
    {
        AbilityPlayTimeType PlayTimeType { get; }
        float DelaySeconds { get; }

        /// <summary>
        /// If true, the component executes on the caster (e.g. animation, sound, movement).
        /// If false, the component executes on the target entity.
        /// Defaults to false for data-driven flexibility.
        /// </summary>
        bool IsCasterScoped { get; }
    }
}
