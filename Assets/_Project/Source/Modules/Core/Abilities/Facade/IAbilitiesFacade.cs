using System;
using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using Cysharp.Threading.Tasks;
using UniRx;

namespace AvantajPrim.Abilities.Facade
{
    public interface IAbilitiesFacade
    {
        IObservable<AbilityPhaseChangedEvent> PhaseChanged { get; }
        IObservable<PresentationAnimationIntent> AnimationIntents { get; }
        IObservable<PresentationSoundIntent> SoundIntents { get; }
        IObservable<PresentationVfxIntent> VfxIntents { get; }
        IObservable<PresentationMovementIntent> MovementIntents { get; }
        IObservable<PresentationAimIntent> AimIntents { get; }
        IObservable<DamageRequestedEvent> DamageEvents { get; }

        void RegisterEntity(IAbilityEntity entity);
        void UnregisterEntity(EntityId id);
        bool HasActiveCasts(EntityId casterId);
        UniTask<CastAbilityResult> CastAsync(AbilityId abilityId, EntityId casterId, EntityId targetId,
            System.Threading.CancellationToken cancellationToken = default);
        UniTask<CastAbilityResult> CastOnTargetsAsync(AbilityId abilityId, EntityId casterId,
            IReadOnlyList<EntityId> targetIds, System.Threading.CancellationToken cancellationToken = default);
        void OnUpdate(float deltaTime);
    }
}
