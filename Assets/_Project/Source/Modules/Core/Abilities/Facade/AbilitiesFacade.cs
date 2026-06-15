using System;
using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Infrastructure;
using Cysharp.Threading.Tasks;
using UniRx;

namespace AvantajPrim.Abilities.Facade
{
    public sealed class AbilitiesFacade : IAbilitiesFacade
    {
        private readonly AbilitiesService _service;
        private readonly AbilityPresentationPort _presentation;
        private readonly AbilityPhasePort _phasePort;

        public AbilitiesFacade(AbilitiesService service, AbilityPresentationPort presentation, AbilityPhasePort phasePort)
        {
            _service = service;
            _presentation = presentation;
            _phasePort = phasePort;
        }

        public IObservable<AbilityPhaseChangedEvent> PhaseChanged => _phasePort.PhaseChanged;
        public IObservable<PresentationAnimationIntent> AnimationIntents => _presentation.Animation;
        public IObservable<PresentationSoundIntent> SoundIntents => _presentation.Sound;
        public IObservable<PresentationVfxIntent> VfxIntents => _presentation.Vfx;
        public IObservable<PresentationMovementIntent> MovementIntents => _presentation.Movement;
        public IObservable<PresentationAimIntent> AimIntents => _presentation.Aim;
        public IObservable<DamageRequestedEvent> DamageEvents => _presentation.Damage;

        public void RegisterEntity(IAbilityEntity entity) => _service.RegisterEntity(entity);
        public void UnregisterEntity(EntityId id) => _service.UnregisterEntity(id);
        public bool HasActiveCasts(EntityId casterId) => _service.HasActiveCasts(casterId);

        public UniTask<CastAbilityResult> CastAsync(AbilityId abilityId, EntityId casterId, EntityId targetId,
            System.Threading.CancellationToken cancellationToken = default) =>
            _service.CastAsync(abilityId, casterId, targetId, cancellationToken);

        public UniTask<CastAbilityResult> CastOnTargetsAsync(AbilityId abilityId, EntityId casterId,
            IReadOnlyList<EntityId> targetIds, System.Threading.CancellationToken cancellationToken = default) =>
            _service.CastOnTargetsAsync(abilityId, casterId, targetIds, cancellationToken);

        public void OnUpdate(float deltaTime) => _service.OnUpdate(deltaTime);
    }
}
