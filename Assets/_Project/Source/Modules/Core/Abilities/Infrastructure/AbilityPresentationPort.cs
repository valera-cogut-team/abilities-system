using System;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using UniRx;

namespace AvantajPrim.Abilities.Infrastructure
{
    public sealed class AbilityPresentationPort : IAbilityPresentationPort
    {
        private readonly Subject<PresentationAnimationIntent> _animation = new Subject<PresentationAnimationIntent>();
        private readonly Subject<PresentationSoundIntent> _sound = new Subject<PresentationSoundIntent>();
        private readonly Subject<PresentationVfxIntent> _vfx = new Subject<PresentationVfxIntent>();
        private readonly Subject<PresentationMovementIntent> _movement = new Subject<PresentationMovementIntent>();
        private readonly Subject<PresentationAimIntent> _aim = new Subject<PresentationAimIntent>();
        private readonly Subject<DamageRequestedEvent> _damage = new Subject<DamageRequestedEvent>();

        public IObservable<PresentationAnimationIntent> Animation => _animation;
        public IObservable<PresentationSoundIntent> Sound => _sound;
        public IObservable<PresentationVfxIntent> Vfx => _vfx;
        public IObservable<PresentationMovementIntent> Movement => _movement;
        public IObservable<PresentationAimIntent> Aim => _aim;
        public IObservable<DamageRequestedEvent> Damage => _damage;

        public void PublishAnimation(PresentationAnimationIntent intent) => _animation.OnNext(intent);
        public void PublishSound(PresentationSoundIntent intent) => _sound.OnNext(intent);
        public void PublishVfx(PresentationVfxIntent intent) => _vfx.OnNext(intent);
        public void PublishMovement(PresentationMovementIntent intent) => _movement.OnNext(intent);
        public void PublishAim(PresentationAimIntent intent) => _aim.OnNext(intent);
        public void PublishDamage(DamageRequestedEvent evt) => _damage.OnNext(evt);
    }
}
