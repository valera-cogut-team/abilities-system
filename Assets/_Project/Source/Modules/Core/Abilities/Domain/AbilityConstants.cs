namespace AvantajPrim.Abilities.Domain
{
    public static class AbilityConstants
    {
        public static class Phases
        {
            public const string Start = "Start";
            public const string End = "End";
            /// <summary>All pending cast effects finished (DoT, displacement, gradual damage, status duration).</summary>
            public const string Complete = "Complete";
        }

        public static class Execution
        {
            public const float OnEndComponentDelaySeconds = 0.5f;
            public const float InstantStatusDurationSeconds = 0.5f;
            public const float DefaultStatusTickIntervalSeconds = 1f;
            public const float DefaultMeleeRange = 5f;
        }

        public static class Casting
        {
            public const string PathPrefix = "Action.Casting.";
        }

        public static class ComponentDefaults
        {
            public const string DefaultAbilityId = "ability_1";
            public const string DefaultAnimationName = "Cast";
            public const float DefaultMovementDurationSeconds = 0.3f;
            public const float DefaultStatusDurationSeconds = 3f;
            public const float DefaultStatusValue = 5f;
            public const float DefaultDamageValue = 10f;
            public const float DefaultRange = 5f;
        }
    }
}
