using Addressables.Facade;
using Core;
using Effects.Application;
using Effects.Facade;
using LifeCycle.Facade;
using Logger.Facade;
using Pool.Facade;

namespace Effects
{
    public sealed class EffectsModule : IModule
    {
        public string Name => "Effects";
        public string Version => "1.0.0";
        public string[] Dependencies => new[] { "Logger", "Addressables", "Pool", "LifeCycle" };
        public bool IsEnabled { get; private set; }

        private IModuleContext _context;
        private EffectsService _service;
        private IEffectsFacade _facade;
        private PooledEffectLifetimeService _lifetime;

        public void Initialize(IModuleContext context)
        {
            _context = context;

            IAddressablesFacade addressables = context.GetModuleFacade<IAddressablesFacade>()
                                               ?? throw new System.InvalidOperationException("IAddressablesFacade is required before EffectsModule.");
            IPoolFacade pool = context.GetModuleFacade<IPoolFacade>()
                               ?? throw new System.InvalidOperationException("IPoolFacade is required before EffectsModule.");
            ILoggerFacade logger = context.GetModuleFacade<ILoggerFacade>();

            _service = new EffectsService(addressables, pool, logger);
            context.Container.Bind<IEffectsService>().FromInstance(_service).AsSingle();
            context.Container.Bind<EffectsService>().FromInstance(_service).AsSingle();

            _lifetime = _service.Lifetime;
            context.Container.Bind<PooledEffectLifetimeService>().FromInstance(_lifetime).AsSingle();

            _facade = new EffectsFacade(_service);
            context.Container.Bind<IEffectsFacade>().FromInstance(_facade).AsSingle();
        }

        public void Enable()
        {
            if (IsEnabled)
                return;

            IsEnabled = true;
            _context.GetModuleFacade<ILifeCycleFacade>()?.RegisterUpdateHandler(_lifetime);
            _context.GetModuleFacade<ILoggerFacade>()?.LogInfo("EffectsModule enabled");
        }

        public void Disable()
        {
            if (!IsEnabled)
                return;

            IsEnabled = false;
            _context.GetModuleFacade<ILifeCycleFacade>()?.UnregisterUpdateHandler(_lifetime);
            _service?.Shutdown();
            _context.GetModuleFacade<ILoggerFacade>()?.LogInfo("EffectsModule disabled");
        }

        public void Shutdown()
        {
            if (IsEnabled)
                Disable();

            _service = null;
            _facade = null;
            _lifetime = null;
        }
    }
}
