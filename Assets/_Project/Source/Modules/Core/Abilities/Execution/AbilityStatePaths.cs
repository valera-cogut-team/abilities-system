using StateMachine.Facade;

namespace AvantajPrim.Abilities.Execution
{
    /// <summary>Cached FSM paths used by ability executors (no per-cast Parse allocations).</summary>
    public static class AbilityStatePaths
    {
        public static readonly StatePath LocomotionDisplaced = StatePath.Parse("Locomotion.Displaced");
        public static readonly StatePath LocomotionIdle = StatePath.Parse("Locomotion.Idle");
        public static readonly StatePath ActionHitReact = StatePath.Parse("Action.HitReact");
        public static readonly StatePath ActionNone = StatePath.Parse("Action.None");
        public static readonly StatePath InputMovement = StatePath.Parse("Input.Movement");
        public static readonly StatePath InputRotation = StatePath.Parse("Input.Rotation");
        public static readonly StatePath StatusCombustion = StatePath.Parse("Status.Combustion");
        public static readonly StatePath StatusFreezing = StatePath.Parse("Status.Freezing");
        public static readonly StatePath StatusHealing = StatePath.Parse("Status.Healing");
        public static readonly StatePath StatusBleeding = StatePath.Parse("Status.Bleeding");
    }
}
