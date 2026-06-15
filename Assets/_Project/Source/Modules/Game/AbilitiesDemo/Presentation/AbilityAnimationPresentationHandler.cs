using AvantajPrim.Abilities.Domain;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using AvantajPrim.AbilitiesDemo.Application;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    public sealed class AbilityAnimationPresentationHandler
    {
        private readonly EntityStateRegistry _entityStateRegistry;

        public AbilityAnimationPresentationHandler(EntityStateRegistry entityStateRegistry)
        {
            _entityStateRegistry = entityStateRegistry;
        }

        public void HandleAnimationIntent(PresentationAnimationIntent intent)
        {
            if (string.IsNullOrEmpty(intent.AnimationName))
                return;

            if (!_entityStateRegistry.TryGet(intent.EntityId, out IEntityStateMachineController controller))
                return;

            controller.PlayCastAnimation(intent.AnimationName);
        }
    }
}
