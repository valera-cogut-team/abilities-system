namespace AvantajPrim.Abilities.Domain
{
    public interface IAbilityEntity
    {
        EntityId Id { get; }
        bool IsPlayer { get; }
    }
}
