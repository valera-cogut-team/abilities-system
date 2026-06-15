using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Facade;
using AvantajPrim.Abilities.Infrastructure;
using AvantajPrim.Abilities.Installers;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Facade;
using AvantajPrim.AbilitiesDemo.Presentation;
using UnityEngine;
using Zenject;

namespace AvantajPrim.AbilitiesDemo.Installers
{
    public static class AbilitiesDemoInstaller
    {
        public static void Install(DiContainer container)
        {
            DemoAddressableCatalog addressCatalog = Resources.Load<DemoAddressableCatalog>("DemoAddressableCatalog");
            if (addressCatalog == null)
                UnityEngine.Debug.LogError("[AbilitiesDemo] DemoAddressableCatalog not found in Resources.");

            container.Bind<DemoAddressableCatalog>().FromInstance(addressCatalog).AsSingle();

            var entityStateRegistry = new EntityStateRegistry();
            container.Bind<EntityStateRegistry>().FromInstance(entityStateRegistry).AsSingle();

            var entityVfxRegistry = new EntityAttachedVfxRegistry();
            container.Bind<EntityAttachedVfxRegistry>().FromInstance(entityVfxRegistry).AsSingle();

            var registry = new DemoEntityRegistry();
            container.Bind<DemoEntityRegistry>().FromInstance(registry).AsSingle();

            var combatRegistry = new DemoCombatRegistry();
            container.Bind<DemoCombatRegistry>().FromInstance(combatRegistry).AsSingle();

            var targeting = new TargetingService();
            container.Bind<TargetingService>().FromInstance(targeting).AsSingle();

            container.Bind<IAbilityTargetResolver>().To<DemoAbilityTargetResolver>().AsSingle();
            container.Bind<IEntityTargetFilter>().To<RegisteredEnemyTargetFilter>().AsSingle();
            container.Bind<EntityDespawnService>().AsSingle();
            container.Bind<AbilityHotkeyBindingService>().AsSingle();

            container.Bind<PlayerMovementService>().AsSingle();
            container.Bind<DemoGameplaySession>().AsSingle();
            container.Bind<PlayerInputRouter>().AsSingle();
            container.Bind<AbilityCastingService>().AsSingle();
            container.Bind<AbilitiesDemoBootstrapService>().AsSingle();
            container.Bind<AbilitiesInputService>().AsSingle();
            container.Bind<AbilitiesDemoTickHandler>().AsSingle();
            container.Bind<GradualCombatApplier>().AsSingle();
            container.Bind<EntityContactDamageService>().AsSingle();
            container.Bind<CastAnimationWaitService>().AsSingle();

            var entityStatePort = new EntityStatePort(entityStateRegistry);
            container.Rebind<IEntityStatePort>().FromInstance(entityStatePort);

            var animationAwaiter = new EntityAnimationAwaiter(container.Resolve<CastAnimationWaitService>());
            container.Rebind<IAbilityAnimationAwaiter>().FromInstance(animationAwaiter);

            IAbilityCastLifecycle castLifecycle = container.Resolve<IAbilityCastLifecycle>();
            AbilityComponentRegistry componentRegistry = AbilitiesInstaller.CreateRegistry(castLifecycle);
            container.Rebind<AbilityComponentRegistry>().FromInstance(componentRegistry);

            IAbilityPresentationPort presentation = container.Resolve<IAbilityPresentationPort>();
            AbilityExecutor executor = AbilitiesStackFactory.CreateExecutor(componentRegistry, presentation, entityStatePort, animationAwaiter);
            container.Rebind<AbilityExecutor>().FromInstance(executor);

            AbilityCatalog catalog = container.Resolve<AbilityCatalog>();
            AbilityActivationLog log = container.Resolve<AbilityActivationLog>();
            AbilityPhasePort phasePort = container.Resolve<AbilityPhasePort>();
            AbilitiesService service = AbilitiesStackFactory.CreateService(catalog, executor, log, phasePort, castLifecycle);
            container.Rebind<AbilitiesService>().FromInstance(service);

            AbilityPresentationPort presentationPort = container.Resolve<AbilityPresentationPort>();
            AbilitiesFacade facade = AbilitiesStackFactory.CreateFacade(service, presentationPort, phasePort);
            container.Rebind<IAbilitiesFacade>().FromInstance(facade);
            container.Rebind<AbilitiesFacade>().FromInstance(facade);

            var replay = new AbilityActivationReplayService(
                log,
                (abilityId, casterId, targetId) => service.CastAsync(abilityId, casterId, targetId),
                (abilityId, casterId, targetIds) => service.CastOnTargetsAsync(abilityId, casterId, targetIds));
            container.Rebind<AbilityActivationReplayService>().FromInstance(replay);
            AbilityEditorPlayAccess.Replay = replay;
            AbilityEditorPlayAccess.HotkeyBindings = container.Resolve<AbilityHotkeyBindingService>();
            AbilityEditorPlayAccess.AbilityCatalog = catalog;

            container.Bind<CombatFeedbackPresenter>().AsSingle();

            var demoFacade = new AbilitiesDemoFacade(registry, targeting, combatRegistry, container.Resolve<DemoGameplaySession>());
            container.Bind<IAbilitiesDemoFacade>().FromInstance(demoFacade).AsSingle();
            container.Bind<AbilitiesDemoFacade>().FromInstance(demoFacade).AsSingle();
        }
    }
}
