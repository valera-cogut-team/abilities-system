using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public static class AbilityInputLockResolver
    {
        public static bool TryGetCasterOnStartLock(
            AbilityDefinition definition,
            out bool blockMovement,
            out bool blockRotation)
        {
            blockMovement = false;
            blockRotation = false;

            if (definition?.Components == null)
                return false;

            for (int i = 0; i < definition.Components.Count; i++)
            {
                IAbilityComponentData component = definition.Components[i];
                if (component is not LockInputComponentData lockData ||
                    component.PlayTimeType != AbilityPlayTimeType.OnStart)
                    continue;

                blockMovement = lockData.BlockMovement;
                blockRotation = lockData.BlockRotation;
                return blockMovement || blockRotation;
            }

            return false;
        }

        public static bool BlocksMovement(AbilityDefinition definition)
        {
            TryGetCasterOnStartLock(definition, out bool blockMovement, out _);
            return blockMovement;
        }

        public static bool BlocksRotation(AbilityDefinition definition)
        {
            TryGetCasterOnStartLock(definition, out _, out bool blockRotation);
            return blockRotation;
        }
    }
}
