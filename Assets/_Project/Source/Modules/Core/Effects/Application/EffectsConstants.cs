namespace Effects.Application
{
    public static class EffectsConstants
    {
        public const float DefaultOneShotLifetimeSeconds = 2f;
        public const float MinParticleLifetimeSeconds = 0.05f;
        public const float LoopingParticleFallbackLifetimeSeconds = 5f;
        public const int DefaultPrewarmCount = 2;
        public const int DefaultMaxPoolSize = 16;
        public const string HostObjectName = "EffectsModule_Root";
    }
}
