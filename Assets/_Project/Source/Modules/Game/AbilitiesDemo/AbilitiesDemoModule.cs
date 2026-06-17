using Addressables.Facade;
using Audio.Facade;
using AvantajPrim.Abilities.Facade;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Facade;
using AvantajPrim.AbilitiesDemo.Installers;
using AvantajPrim.AbilitiesDemo.Presentation;
using Core;
using Effects.Facade;
using Input.Facade;
using LifeCycle.Facade;
using Logger.Facade;
using Pool.Facade;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo
{
    public sealed class AbilitiesDemoModule : IModule
    {
        public string Name => "AbilitiesDemo";
        public string Version => "1.0.0";
        public string[] Dependencies => new[]
        {
            "Abilities", "Input", "Addressables", "Pool", "Audio", "Effects", "LifeCycle", "Logger", "StateMachine"
        };
        public bool IsEnabled { get; private set; }

        private IModuleContext _context;
        private AbilitiesInputService _inputService;
        private AbilitiesDemoWorldBinder _worldBinder;
        private GameObject _root;
        private AbilitiesDemoPresentationOrchestrator _orchestrator;

        public void Initialize(IModuleContext context)
        {
            _context = context;
            AbilitiesDemoInstaller.Install(context.Container);
        }

        public void Enable()
        {
            if (IsEnabled)
                return;

            ILoggerFacade logger = _context.GetModuleFacade<ILoggerFacade>();
            IAbilitiesFacade abilities = _context.GetModuleFacade<IAbilitiesFacade>();
            IInputFacade input = _context.GetModuleFacade<IInputFacade>();
            IAddressablesFacade addressables = _context.GetModuleFacade<IAddressablesFacade>();
            IPoolFacade pool = _context.GetModuleFacade<IPoolFacade>();

            if (abilities == null || input == null || addressables == null || pool == null)
            {
                logger?.LogError("[AbilitiesDemo] Missing critical dependencies.");
                return;
            }

            IsEnabled = true;
            _inputService = _context.Container.Resolve<AbilitiesInputService>();
            _root = new GameObject("AbilitiesDemoWorld");
            Object.DontDestroyOnLoad(_root);

            _worldBinder = RegisterLifecycleHandlers();
            _orchestrator = CreatePresentationOrchestrator(abilities, addressables, pool, logger);

            IAbilitiesDemoFacade demo = _context.GetModuleFacade<IAbilitiesDemoFacade>();
            demo?.SetWorldVisible(true);
            demo?.SetGameplayActive(true);
        }

        private AbilitiesDemoWorldBinder RegisterLifecycleHandlers()
        {
            ILifeCycleFacade lifeCycle = _context.GetModuleFacade<ILifeCycleFacade>();
            var binder = new AbilitiesDemoWorldBinder(lifeCycle);

            binder.RegisterUpdate(_inputService);
            binder.RegisterUpdate(_context.Container.Resolve<AbilitiesDemoTickHandler>());
            binder.RegisterUpdate(_context.Container.Resolve<GradualCombatApplier>());
            binder.RegisterUpdate(_context.Container.Resolve<PlayerMovementService>());
            binder.RegisterUpdate(_context.Container.Resolve<CastAnimationWaitService>());
            binder.RegisterUpdate(_context.Container.Resolve<EntityContactDamageService>());
            binder.RegisterLateUpdate(_context.Container.Resolve<AbilitiesDemoTickHandler>());

            return binder;
        }

        private AbilitiesDemoPresentationOrchestrator CreatePresentationOrchestrator(
            IAbilitiesFacade abilities,
            IAddressablesFacade addressables,
            IPoolFacade pool,
            ILoggerFacade logger)
        {
            return new AbilitiesDemoPresentationOrchestrator(
                _root.transform,
                _context.Container.Resolve<AbilitiesDemoBootstrapService>(),
                _context.Container.Resolve<TargetingService>(),
                _context.Container.Resolve<PlayerInputRouter>(),
                abilities,
                _context.Container.Resolve<DemoEntityRegistry>(),
                _context.Container.Resolve<DemoCombatRegistry>(),
                _context.Container.Resolve<DemoGameplaySession>(),
                _context.Container.Resolve<EntityStateRegistry>(),
                _context.Container.Resolve<IAbilitiesDemoFacade>(),
                _context.Container.Resolve<AbilitiesDemoTickHandler>(),
                _context.Container.Resolve<CombatFeedbackPresenter>(),
                _context.GetModuleFacade<IAudioFacade>(),
                _context.GetModuleFacade<IEffectsFacade>(),
                addressables,
                pool,
                logger,
                _context.Container.Resolve<GradualCombatApplier>(),
                _context.Container.Resolve<EntityDespawnService>(),
                _context.Container.Resolve<EntityAttachedVfxRegistry>(),
                _context.Container.Resolve<DemoAddressableCatalog>());
        }

        public void Disable()
        {
            if (!IsEnabled)
                return;

            IsEnabled = false;

            IAbilitiesDemoFacade demo = _context.GetModuleFacade<IAbilitiesDemoFacade>();
            demo?.SetGameplayActive(false);
            demo?.SetWorldVisible(false);

            _worldBinder?.UnregisterAll();
            _worldBinder = null;

            _inputService = null;
            _orchestrator?.Dispose();
            _orchestrator = null;

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
        }

        public void Shutdown() => Disable();
    }
}