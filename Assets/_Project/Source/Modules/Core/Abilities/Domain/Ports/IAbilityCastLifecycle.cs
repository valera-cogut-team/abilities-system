namespace AvantajPrim.Abilities.Domain.Ports
{
    public interface IAbilityCastLifecycle
    {
        int BeginCast(AbilityId abilityId, EntityId casterId);
        void RegisterPendingEffect(int castId);
        void CompletePendingEffect(int castId);
        void MarkExecutionFinished(int castId);
        void ForceComplete(int castId);
    }
}
