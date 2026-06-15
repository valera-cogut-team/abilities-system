namespace AvantajPrim.Abilities.Domain.Ports
{
    public interface IAbilityPhaseNotifier
    {
        void NotifyPhaseChanged(AbilityPhaseChangedEvent evt);
    }
}
