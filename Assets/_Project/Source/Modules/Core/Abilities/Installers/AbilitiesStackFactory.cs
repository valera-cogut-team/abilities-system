using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Facade;
using AvantajPrim.Abilities.Infrastructure;

namespace AvantajPrim.Abilities.Installers
{
    public static class AbilitiesStackFactory
    {
        public static AbilityExecutor CreateExecutor(
            AbilityComponentRegistry registry,
            IAbilityPresentationPort presentation,
            IEntityStatePort entityState,
            IAbilityAnimationAwaiter animationAwaiter) =>
            new AbilityExecutor(registry, presentation, entityState, animationAwaiter);

        public static AbilitiesService CreateService(
            AbilityCatalog catalog,
            AbilityExecutor executor,
            AbilityActivationLog log,
            AbilityPhasePort phasePort,
            IAbilityCastLifecycle castLifecycle = null) =>
            new AbilitiesService(catalog, executor, log, phasePort, castLifecycle);

        public static AbilitiesFacade CreateFacade(
            AbilitiesService service,
            AbilityPresentationPort presentation,
            AbilityPhasePort phasePort) =>
            new AbilitiesFacade(service, presentation, phasePort);
    }
}
