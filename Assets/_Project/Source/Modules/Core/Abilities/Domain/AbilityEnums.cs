namespace AvantajPrim.Abilities.Domain
{
    public enum AbilityPlayTimeType { OnStart, OnEnd, Delay }

    /// <summary>
    /// Ability-level target resolution (see <c>DemoAbilityTargetResolver</c>):
    /// <see cref="Player"/> — cast on caster without enemy selection;
    /// <see cref="Enemy"/> — cast on selected enemies only (empty selection fails).
    /// Component-level: effect on cast target (Enemy) vs on caster (Player).
    /// </summary>
    public enum AbilityTargetType
    {
        /// <summary>Resolver returns caster (<c>registry.PlayerId</c>); selection ignored.</summary>
        Player = 0,

        /// <summary>Resolver returns selected enemies; fails when none selected.</summary>
        Enemy = 1
    }

    public enum AbilityDurationType { Instant, Continuous }
    public enum AbilityRangeType { Melee, Ranged }

    /// <summary>Status effect applied via <see cref="Data.StatusEffectComponentData"/>.</summary>
    public enum StatusEffectType
    {
        None = 0,

        /// <summary>Damage-over-time burn. Test assignment wording: «burning». FSM path: <c>Status.Combustion</c>.</summary>
        Combustion = 1,

        Freezing = 2,
        Healing = 3,
        Bleeding = 4
    }

    public enum VfxPresentationStyle { Default, Healing }
}
