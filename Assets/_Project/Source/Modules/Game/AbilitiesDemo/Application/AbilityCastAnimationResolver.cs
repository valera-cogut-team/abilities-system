using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public static class AbilityCastAnimationResolver
    {
        public static bool TryGetCastAnimationName(AbilityCatalog catalog, AbilityId abilityId, out string animationName)
        {
            animationName = null;
            if (catalog == null || !catalog.TryGet(abilityId, out AbilityDefinition definition))
                return false;

            foreach (IAbilityComponentData component in definition.Components)
            {
                if (component is not AnimationComponentData animation)
                    continue;

                animationName = animation.ResolveAnimationName();
                if (string.IsNullOrEmpty(animationName))
                    continue;

                return true;
            }

            return false;
        }
    }
}
