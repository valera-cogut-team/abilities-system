using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Execution.Executors;
using AvantajPrim.Abilities.Facade;
using AvantajPrim.Abilities.Infrastructure;
using Zenject;

namespace AvantajPrim.Abilities.Installers
{
    public static class AbilitiesInstaller
    {
        public static void Install(DiContainer container)
        {
            var presentation = new AbilityPresentationPort();
            container.Bind<AbilityPresentationPort>().FromInstance(presentation).AsSingle();
            container.Bind<IAbilityPresentationPort>().FromInstance(presentation).AsSingle();

            var phasePort = new AbilityPhasePort();
            container.Bind<AbilityPhasePort>().FromInstance(phasePort).AsSingle();
            container.Bind<IAbilityPhaseNotifier>().FromInstance(phasePort).AsSingle();

            NullEntityStatePort entityStatePort = NullEntityStatePort.Instance;
            container.Bind<IEntityStatePort>().FromInstance(entityStatePort).AsSingle();

            ImmediateAnimationAwaiter animationAwaiter = ImmediateAnimationAwaiter.Instance;
            container.Bind<IAbilityAnimationAwaiter>().FromInstance(animationAwaiter).AsSingle();

            var castLifecycle = new AbilityCastLifecycle();
            container.Bind<AbilityCastLifecycle>().FromInstance(castLifecycle).AsSingle();
            container.Bind<IAbilityCastLifecycle>().FromInstance(castLifecycle).AsSingle();
            container.Bind<IAbilityCastCompletionAwaiter>().FromInstance(castLifecycle).AsSingle();

            AbilityComponentRegistry registry = CreateRegistry(castLifecycle);
            container.Bind<AbilityComponentRegistry>().FromInstance(registry).AsSingle();

            AbilityExecutor executor = AbilitiesStackFactory.CreateExecutor(registry, presentation, entityStatePort, animationAwaiter);
            container.Bind<AbilityExecutor>().FromInstance(executor).AsSingle();

            var catalog = new AbilityCatalog();
            container.Bind<AbilityCatalog>().FromInstance(catalog).AsSingle();

            var log = new AbilityActivationLog();
            container.Bind<AbilityActivationLog>().FromInstance(log).AsSingle();

            AbilitiesService service = AbilitiesStackFactory.CreateService(catalog, executor, log, phasePort, castLifecycle);
            container.Bind<AbilitiesService>().FromInstance(service).AsSingle();

            var replay = new AbilityActivationReplayService(
                log,
                (abilityId, casterId, targetId) => service.CastAsync(abilityId, casterId, targetId),
                (abilityId, casterId, targetIds) => service.CastOnTargetsAsync(abilityId, casterId, targetIds));
            container.Bind<AbilityActivationReplayService>().FromInstance(replay).AsSingle();
            AbilityEditorPlayAccess.Log = log;
            AbilityEditorPlayAccess.Replay = replay;

            AbilitiesFacade facade = AbilitiesStackFactory.CreateFacade(service, presentation, phasePort);
            container.Bind<IAbilitiesFacade>().FromInstance(facade).AsSingle();
            container.Bind<AbilitiesFacade>().FromInstance(facade).AsSingle();
        }

        public static AbilityComponentRegistry CreateRegistry(IAbilityCastLifecycle castLifecycle = null)
        {
            castLifecycle ??= NullAbilityCastLifecycle.Instance;
            var registry = new AbilityComponentRegistry();
            registry.Register(new AnimationComponentExecutor());
            registry.Register(new SoundComponentExecutor());
            registry.Register(new DamageComponentExecutor(castLifecycle));
            registry.Register(new VisualFxComponentExecutor());
            registry.Register(new MovementComponentExecutor(castLifecycle));
            registry.Register(new StatusEffectComponentExecutor(castLifecycle));
            registry.Register(new AimComponentExecutor());
            registry.Register(new LockInputComponentExecutor());
            return registry;
        }
    }
}
