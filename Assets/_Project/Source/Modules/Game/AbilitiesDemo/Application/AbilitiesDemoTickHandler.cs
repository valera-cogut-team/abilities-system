using AvantajPrim.AbilitiesDemo.Facade;
using AvantajPrim.AbilitiesDemo.Presentation;
using LifeCycle.Facade;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class AbilitiesDemoTickHandler : IUpdateHandler, ILateUpdateHandler
    {
        private readonly EntityStateRegistry _entityStateRegistry;
        private readonly IAbilitiesDemoFacade _demoFacade;
        private TargetSelectionIndicator _targetIndicator;
        private DemoWorldUiCanvas _worldUi;
        private bool _presentationReady;

        public AbilitiesDemoTickHandler(
            EntityStateRegistry entityStateRegistry,
            IAbilitiesDemoFacade demoFacade)
        {
            _entityStateRegistry = entityStateRegistry;
            _demoFacade = demoFacade;
        }

        public void ConfigurePresentation(TargetSelectionIndicator targetIndicator, DemoWorldUiCanvas worldUi)
        {
            _targetIndicator = targetIndicator;
            _worldUi = worldUi;
        }

        public void SetPresentationReady(bool ready) => _presentationReady = ready;

        public void OnUpdate(float deltaTime)
        {
            if (!_presentationReady ||
                _entityStateRegistry == null ||
                _demoFacade == null ||
                !_demoFacade.IsGameplayActive)
                return;

            _entityStateRegistry.TickAll(deltaTime);
        }

        public void OnLateUpdate(float deltaTime)
        {
            if (!_presentationReady || _demoFacade == null)
                return;

            if (_demoFacade.IsGameplayActive)
                _targetIndicator?.LateFollow();

            if (_worldUi != null && (_demoFacade.IsGameplayActive || _demoFacade.IsWorldVisible))
                _worldUi.LateBillboard();
        }
    }
}
