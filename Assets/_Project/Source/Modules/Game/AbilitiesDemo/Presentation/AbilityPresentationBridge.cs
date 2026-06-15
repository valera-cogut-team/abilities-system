using System.Threading;
using Addressables.Facade;
using Audio.Facade;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Facade;
using AvantajPrim.AbilitiesDemo.Application;
using Cysharp.Threading.Tasks;
using Effects.Facade;
using Logger.Facade;
using UniRx;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    /// <summary>Subscribes to <see cref="IAbilitiesFacade"/> presentation streams and dispatches to focused handlers.</summary>
    public sealed class AbilityPresentationBridge : System.IDisposable
    {
        private readonly CastPhasePresentationHandler _phaseHandler;
        private readonly AbilitySoundPresenter _soundPresenter;
        private readonly AbilityVfxPresenter _vfxPresenter;
        private readonly AbilityCombatPresentationHandler _combatHandler;
        private readonly AbilityAnimationPresentationHandler _animationHandler;
        private readonly DemoAddressableCatalog _addressCatalog;
        private CompositeDisposable _subscriptions;

        public AbilityPresentationBridge(
            IAbilitiesFacade abilities,
            DemoEntityRegistry registry,
            DemoCombatRegistry combatRegistry,
            EntityStateRegistry entityStateRegistry,
            CombatFeedbackPresenter combatFeedback,
            IAudioFacade audio,
            IEffectsFacade effects,
            IAddressablesFacade addressables,
            DemoAddressableCatalog addressCatalog,
            ILoggerFacade logger,
            GradualCombatApplier gradualApplier,
            EntityAttachedVfxRegistry entityVfxRegistry)
        {
            _addressCatalog = addressCatalog;
            _phaseHandler = new CastPhasePresentationHandler(entityStateRegistry);
            _soundPresenter = new AbilitySoundPresenter(audio, addressables, logger);
            var vfxRegistry = entityVfxRegistry;
            vfxRegistry?.BindEffects(effects);
            _vfxPresenter = new AbilityVfxPresenter(registry, effects, addressables, logger, vfxRegistry);
            _combatHandler = new AbilityCombatPresentationHandler(registry, combatRegistry, gradualApplier);
            _animationHandler = new AbilityAnimationPresentationHandler(entityStateRegistry);

            _subscriptions = new CompositeDisposable();

            abilities.PhaseChanged
                .Subscribe(_phaseHandler.HandlePhaseChanged)
                .AddTo(_subscriptions);

            abilities.SoundIntents
                .Subscribe(intent => _soundPresenter.HandleSoundIntent(intent).Forget())
                .AddTo(_subscriptions);

            abilities.VfxIntents
                .Subscribe(intent => _vfxPresenter.HandleVfxIntent(intent).Forget())
                .AddTo(_subscriptions);

            abilities.DamageEvents
                .Subscribe(_combatHandler.HandleDamageEvent)
                .AddTo(_subscriptions);

            abilities.AimIntents
                .Subscribe(_combatHandler.HandleAimIntent)
                .AddTo(_subscriptions);

            abilities.AnimationIntents
                .Subscribe(_animationHandler.HandleAnimationIntent)
                .AddTo(_subscriptions);

            abilities.MovementIntents
                .Subscribe(_ => { })
                .AddTo(_subscriptions);
        }

        public async UniTask PreloadPresentationAssetsAsync(CancellationToken cancellationToken = default)
        {
            await _soundPresenter.PreloadClipsAsync(_addressCatalog, cancellationToken);
            await _vfxPresenter.PreloadVfxAsync(_addressCatalog, cancellationToken);
        }

        public void Dispose()
        {
            _subscriptions?.Dispose();
            _subscriptions = null;
        }
    }
}
