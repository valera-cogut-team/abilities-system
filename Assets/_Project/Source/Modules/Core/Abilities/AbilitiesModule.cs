using AvantajPrim.Abilities.Facade;
using AvantajPrim.Abilities.Installers;
using Core;
using LifeCycle.Facade;
using Logger.Facade;

namespace AvantajPrim.Abilities
{
    public sealed class AbilitiesModule : IModule
    {
        public string Name => "Abilities";
        public string Version => "1.0.0";
        public string[] Dependencies => new[] { "Logger", "LifeCycle" };
        public bool IsEnabled { get; private set; }

        private IModuleContext _context;
        private IAbilitiesFacade _facade;
        private AbilitiesUpdateHandler _updateHandler;

        public void Initialize(IModuleContext context)
        {
            _context = context;
            AbilitiesInstaller.Install(context.Container);
            _facade = context.Container.Resolve<IAbilitiesFacade>();
            _updateHandler = new AbilitiesUpdateHandler(_facade);
            context.Container.Bind<AbilitiesUpdateHandler>().FromInstance(_updateHandler).AsSingle();
        }

        public void Enable()
        {
            if (IsEnabled) return;
            IsEnabled = true;
            ILifeCycleFacade lifeCycle = _context.GetModuleFacade<ILifeCycleFacade>();
            lifeCycle?.RegisterUpdateHandler(_updateHandler);
            _context.GetModuleFacade<ILoggerFacade>()?.LogInfo("AbilitiesModule enabled");
        }

        public void Disable()
        {
            if (!IsEnabled) return;
            IsEnabled = false;
            ILifeCycleFacade lifeCycle = _context.GetModuleFacade<ILifeCycleFacade>();
            lifeCycle?.UnregisterUpdateHandler(_updateHandler);
            _context.GetModuleFacade<ILoggerFacade>()?.LogInfo("AbilitiesModule disabled");
        }

        public void Shutdown()
        {
            Disable();
            _facade = null;
            _updateHandler = null;
        }
    }
}
