using AvantajPrim.Abilities.Domain;

namespace AvantajPrim.Abilities.Execution
{
    public sealed class AbilityExecutionContext
    {
        public AbilityId AbilityId { get; }
        public EntityId CasterId { get; }
        public EntityId TargetId { get; }
        public IAbilityEntity Caster { get; }
        public IAbilityEntity Target { get; }
        public int CastLifecycleId { get; }
        public float Elapsed { get; set; }

        public AbilityExecutionContext(AbilityId abilityId, EntityId casterId, EntityId targetId,
            IAbilityEntity caster, IAbilityEntity target, int castLifecycleId = 0)
        {
            AbilityId = abilityId;
            CasterId = casterId;
            TargetId = targetId;
            Caster = caster;
            Target = target;
            CastLifecycleId = castLifecycleId;
        }
    }

    public static class AbilityTargetIdResolver
    {
        public static EntityId Resolve(AbilityTargetType targetType, AbilityExecutionContext context) =>
            targetType == AbilityTargetType.Player
                ? context.CasterId
                : context.TargetId;
    }
}
