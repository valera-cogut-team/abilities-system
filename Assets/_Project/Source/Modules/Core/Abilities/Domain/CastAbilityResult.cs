namespace AvantajPrim.Abilities.Domain
{
    public enum CastAbilityErrorCode
    {
        None = 0,
        UnknownAbility,
        InvalidCaster,
        InvalidTarget,
        AlreadyCasting,
        Blocked
    }

    public readonly struct CastAbilityResult
    {
        public readonly bool Success;
        public readonly CastAbilityErrorCode ErrorCode;
        public CastAbilityResult(bool success, CastAbilityErrorCode errorCode)
        {
            Success = success;
            ErrorCode = errorCode;
        }

        public static CastAbilityResult Ok() => new CastAbilityResult(true, CastAbilityErrorCode.None);
        public static CastAbilityResult Fail(CastAbilityErrorCode code) => new CastAbilityResult(false, code);
    }
}
