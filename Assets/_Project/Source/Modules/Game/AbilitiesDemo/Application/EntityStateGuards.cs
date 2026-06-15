using AvantajPrim.AbilitiesDemo.Domain;
using StateMachine.Facade;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public static class EntityStateGuards
    {
        public static bool NotDead(StatePath from, StatePath to, IStateMachineRuntime runtime, in TransitionContext context)
        {
            if (to.Region == EntityStatePaths.Vitality.Dead.Region &&
                to == EntityStatePaths.Vitality.Dead)
                return true;

            return !runtime.IsInState(EntityStatePaths.Vitality.Dead);
        }

        public static bool NotMovementLocked(StatePath from, StatePath to, IStateMachineRuntime runtime, in TransitionContext context)
        {
            if (to.Region != EntityStatePaths.Locomotion.Idle.Region)
                return true;

            if (to == EntityStatePaths.Locomotion.Idle)
                return true;

            if (to == EntityStatePaths.Locomotion.Displaced)
                return true;

            return !runtime.IsInState(EntityStatePaths.Input.Movement);
        }
    }
}
