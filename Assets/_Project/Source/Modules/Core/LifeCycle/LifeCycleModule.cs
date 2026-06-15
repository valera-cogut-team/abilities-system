using Core;
using LifeCycle.Application;
using LifeCycle.Facade;
using Logger.Facade;
using UniRx;
using UnityEngine;
using Zenject;

namespace LifeCycle
{
    public class LifeCycleModule : IModule
    {
        public string Name => "LifeCycle";
        public string Version => "1.0.0";
        public string[] Dependencies => new[] { "Logger" };
        public bool IsEnabled { get; private set; }

        private IModuleContext _context;
        private ILifeCycleService _service;
        private ILifeCycleFacade _facade;
        private CompositeDisposable _subscriptions;
        private float _lastUpdateTime;

        public void Initialize(IModuleContext context)
        {
            _context = context;
            ILoggerFacade logger = context.GetModuleFacade<ILoggerFacade>();
            _service = new LifeCycleService(logger);
            context.Container.Bind<ILifeCycleService>().FromInstance(_service).AsSingle();
            _facade = new LifeCycleFacade(_service);
            context.Container.Bind<ILifeCycleFacade>().FromInstance(_facade).AsSingle();
        }

        public void Enable()
        {
            if (IsEnabled) return;
            IsEnabled = true;

            _subscriptions?.Dispose();
            _subscriptions = new CompositeDisposable();
            _lastUpdateTime = Time.time;

            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    float t = Time.time;
                    _facade.TickUpdate(t - _lastUpdateTime);
                    _lastUpdateTime = t;
                })
                .AddTo(_subscriptions);

            Observable.EveryLateUpdate()
                .Subscribe(_ => _facade.TickLateUpdate(Time.deltaTime))
                .AddTo(_subscriptions);

            Observable.EveryFixedUpdate()
                .Subscribe(_ => _facade.TickFixedUpdate(Time.fixedDeltaTime))
                .AddTo(_subscriptions);

            _context.GetModuleFacade<ILoggerFacade>()?.LogInfo("LifeCycleModule enabled");
        }

        public void Disable()
        {
            if (!IsEnabled) return;
            IsEnabled = false;
            _subscriptions?.Dispose();
            _subscriptions = null;
            _context.GetModuleFacade<ILoggerFacade>()?.LogInfo("LifeCycleModule disabled");
        }

        public void Shutdown()
        {
            Disable();
            _service?.Clear();
            _service = null;
            _facade = null;
        }
    }
}
