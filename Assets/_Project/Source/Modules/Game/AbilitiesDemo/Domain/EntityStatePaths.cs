using AvantajPrim.Abilities.Domain;
using StateMachine.Facade;

namespace AvantajPrim.AbilitiesDemo.Domain
{
    public static class EntityStatePaths
    {
        public static class Vitality
        {
            public static readonly StatePath Alive = StatePath.Parse("Vitality.Alive");
            public static readonly StatePath Dead = StatePath.Parse("Vitality.Dead");
        }

        public static class Locomotion
        {
            public static readonly StatePath Idle = StatePath.Parse("Locomotion.Idle");
            public static readonly StatePath Walking = StatePath.Parse("Locomotion.Walking");
            public static readonly StatePath Displaced = StatePath.Parse("Locomotion.Displaced");
        }

        public static class Action
        {
            public static readonly StatePath None = StatePath.Parse("Action.None");
            public static readonly StatePath HitReact = StatePath.Parse("Action.HitReact");
            public static readonly StatePath CastingRoot = StatePath.Parse("Action.Casting");
            public static readonly StatePath CastingWildcard = StatePath.Parse("Action.Casting.*");
            public const string CastingPathPrefix = AbilityConstants.Casting.PathPrefix;

            public static StatePath Casting(string animationName) =>
                StatePath.Parse($"{CastingPathPrefix}{animationName}");
        }

        public static class Input
        {
            public static readonly StatePath Movement = StatePath.Parse("Input.Movement");
            public static readonly StatePath Rotation = StatePath.Parse("Input.Rotation");
        }

        public static class Status
        {
            public static readonly StatePath Combustion = StatePath.Parse("Status.Combustion");
            public static readonly StatePath Freezing = StatePath.Parse("Status.Freezing");
            public static readonly StatePath Healing = StatePath.Parse("Status.Healing");
            public static readonly StatePath Bleeding = StatePath.Parse("Status.Bleeding");
        }

        public static class Ai
        {
            public static readonly StatePath Idle = StatePath.Parse("AI.Idle");
        }
    }
}
