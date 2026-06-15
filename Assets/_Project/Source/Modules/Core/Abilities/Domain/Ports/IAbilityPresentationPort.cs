namespace AvantajPrim.Abilities.Domain.Ports
{
    public interface IAbilityPresentationPort
    {
        void PublishAnimation(PresentationAnimationIntent intent);
        void PublishSound(PresentationSoundIntent intent);
        void PublishVfx(PresentationVfxIntent intent);
        void PublishMovement(PresentationMovementIntent intent);
        void PublishAim(PresentationAimIntent intent);
        void PublishDamage(DamageRequestedEvent evt);
    }
}
