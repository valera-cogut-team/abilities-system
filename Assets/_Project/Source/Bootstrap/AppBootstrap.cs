using System.Threading;
using Cysharp.Threading.Tasks;
using Addressables;
using Addressables.Facade;
using Core;
using Input;
using LifeCycle;
using Logger;
using Logger.Application;
using Logger.Facade;
using Pool;
using StateMachine;
using Audio;
using Effects;
using AvantajPrim.Abilities;
using AvantajPrim.AbilitiesDemo;
using AvantajPrim.AbilitiesDemo.Facade;
using Zenject;

namespace Bootstrap
{
    public sealed class AppBootstrap
    {
        private readonly DiContainer _container;
        private readonly bool _enableDebugLogs;

        private ModuleManager _moduleManager;
        private IModuleContext _moduleContext;

        public IModuleContext ModuleContext => _moduleContext;
        public ModuleManager ModuleManager => _moduleManager;

        public AppBootstrap(DiContainer container, bool enableDebugLogs)
        {
            _container = container ?? throw new System.ArgumentNullException(nameof(container));
            _enableDebugLogs = enableDebugLogs;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            await InitializeCoreInfrastructureAsync(cancellationToken);
            await InitializeCoreModulesAsync(cancellationToken);
            await InitializeGameModulesAsync(cancellationToken);
            await FinalizeInitializationAsync(cancellationToken);
        }

        private async UniTask InitializeCoreInfrastructureAsync(CancellationToken cancellationToken)
        {
            _moduleManager = new ModuleManager(_container);
            _moduleContext = _moduleManager.Context;

            var loggerModule = new LoggerModule();
            loggerModule.Initialize(_moduleContext, LoggerConfig.CreateDefault(_enableDebugLogs));
            loggerModule.Enable();
            _moduleManager.RegisterModule(loggerModule);

            _container.Bind<ModuleManager>().FromInstance(_moduleManager).AsSingle();

            await UniTask.Yield(cancellationToken);
        }

        private async UniTask InitializeCoreModulesAsync(CancellationToken cancellationToken)
        {
            ILoggerFacade logger = _moduleContext.GetModuleFacade<ILoggerFacade>();

            var lifeCycle = new LifeCycleModule();
            lifeCycle.Initialize(_moduleContext);
            lifeCycle.Enable();
            _moduleManager.RegisterModule(lifeCycle);

            var input = new InputModule();
            input.Initialize(_moduleContext);
            input.Enable();
            _moduleManager.RegisterModule(input);

            var addressables = new AddressablesModule();
            addressables.Initialize(_moduleContext);
            addressables.Enable();
            _moduleManager.RegisterModule(addressables);

            IAddressablesFacade addressablesFacade = _moduleContext.GetModuleFacade<Addressables.Facade.IAddressablesFacade>();
            if (addressablesFacade != null)
            {
                await addressablesFacade.EnsureInitializedAsync();
            }

            var stateMachine = new StateMachineModule();
            stateMachine.Initialize(_moduleContext);
            stateMachine.Enable();
            _moduleManager.RegisterModule(stateMachine);

            var pool = new PoolModule();
            pool.Initialize(_moduleContext);
            pool.Enable();
            _moduleManager.RegisterModule(pool);

            var audio = new AudioModule();
            audio.Initialize(_moduleContext);
            audio.Enable();
            _moduleManager.RegisterModule(audio);

            var effects = new EffectsModule();
            effects.Initialize(_moduleContext);
            effects.Enable();
            _moduleManager.RegisterModule(effects);

            await UniTask.Yield(cancellationToken);
        }

        private async UniTask InitializeGameModulesAsync(CancellationToken cancellationToken)
        {
            var abilities = new AbilitiesModule();
            abilities.Initialize(_moduleContext);
            abilities.Enable();
            _moduleManager.RegisterModule(abilities);

            var abilitiesDemo = new AbilitiesDemoModule();
            abilitiesDemo.Initialize(_moduleContext);
            abilitiesDemo.Enable();
            _moduleManager.RegisterModule(abilitiesDemo);

            await UniTask.Yield(cancellationToken);
        }

        private async UniTask FinalizeInitializationAsync(CancellationToken cancellationToken)
        {
            IAbilitiesDemoFacade demo = _moduleContext.GetModuleFacade<IAbilitiesDemoFacade>();
            demo?.SetWorldVisible(true);
            demo?.SetGameplayActive(true);

            await UniTask.Yield(cancellationToken);
        }

        public void Shutdown()
        {
            _moduleContext = null;
            _moduleManager = null;
        }
    }
}
