namespace AvantajPrim.Abilities.Domain
{
    public sealed class AbilityEntityModel : IAbilityEntity
    {
        public EntityId Id { get; }
        public bool IsPlayer { get; }

        public AbilityEntityModel(EntityId id, bool isPlayer)
        {
            Id = id;
            IsPlayer = isPlayer;
        }
    }
}
