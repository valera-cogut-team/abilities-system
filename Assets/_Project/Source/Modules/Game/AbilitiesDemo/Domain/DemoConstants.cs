using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Domain
{
    public static class DemoConstants
    {
        public static class AbilityIds
        {
            public const string Dash = "dash";
            public const string Firewall = "firewall";
            public const string Healing = "healing";
            public const string DefencedAttack = "defenced_attack";
        }

        public static class Animation
        {
            public const string IsWalking = "IsWalking";
            public const string Idle = "Idle";
            public const string WalkForward = "WalkForward";
            public const string Dead = "Dead";
            public const string GetHit = "GetHit";
            public const string DefencedAttack = "DefencedAttack";
            public const string DefendHitState = "DefendHit";
            public const string Heal = "Heal";
            public const string Healing = "Healing";
            public const string PotionDrinkState = "PotionDrink";
            public const string Dash = "Dash";
            public const string Firewall = "Firewall";

            public const float IdleBlendSeconds = 0.15f;
            public const float WalkBlendSeconds = 0.1f;
            public const float CastBlendSeconds = 0.1f;
            public const float CastWaitTimeoutSeconds = 1.5f;
            public const float CastNormalizedTimeComplete = 0.92f;
            public const float CastClipLengthBufferSeconds = 0.2f;
            public const float HitReactPunchScale = 0.15f;
            public const float HitReactPunchDurationSeconds = 0.25f;
            public const float MinDisplacementTweenSeconds = 0.01f;
        }

        public static class Layers
        {
            public const string Targeting = "Targeting";
            public const string Ground = "Ground";
            public const string IgnoreRaycast = "Ignore Raycast";
        }

        public static class Physics
        {
            public const float DirectionEpsilonSqr = 0.0001f;
            public const float ScaleComparisonEpsilon = 0.001f;
            public const float SnapEpsilon = 0.0001f;
        }

        public static class Spawn
        {
            public static readonly Vector3 PlayerPosition = new Vector3(-4f, 0f, 0f);
            public static readonly Vector3 ArenaScale = new Vector3(2f, 1f, 2f);

            public static readonly Vector3[] EnemyPositions =
            {
                new Vector3(2f, 0f, 2f),
                new Vector3(4f, 0f, 0f),
                new Vector3(2f, 0f, -2f)
            };
        }

        public static class Entity
        {
            public const int EnemyCount = 3;
            public const float DefaultMaxHealth = 100f;
            public const float DefaultBodyCenterLocalY = 1f;
            public const float CapsuleHeight = 2f;
            public const float CapsuleRadius = 0.45f;
            public static readonly Vector3 CapsuleCenter = new Vector3(0f, 1f, 0f);
            public static readonly Vector3 VfxSpawnLocalPosition = new Vector3(0f, 1.8f, 0f);

            public const string PlayerDisplayName = "Player";
            public const string EnemyDisplayNameFormat = "Enemy {0}";
            public static readonly string[] EnemyDisplayNames = { "Enemy A", "Enemy B", "Enemy C" };

            public static readonly Color[] EnemyTintColors =
            {
                new Color(0.95f, 0.3f, 0.25f),
                new Color(0.85f, 0.2f, 0.55f),
                new Color(0.75f, 0.25f, 0.95f)
            };
        }

        public static class ObjectNames
        {
            public const string VfxSpawnPoint = "VfxSpawnPoint";
            public const string FallbackArena = "DemoArena_Fallback";
        }

        public static class Bootstrap
        {
            public const float PrefabLoadTimeoutSeconds = 15f;
        }

        public static class Combat
        {
            public const float ContactDistance = 0.9f;
            public const float ContactDamageAmount = 8f;
            public const float ContactCooldownSeconds = 1f;
            public const float DefaultHealthTweenSeconds = 0.25f;
            public const float TweenDurationTickFactor = 0.9f;
            public const float MaxTweenDurationSeconds = 0.5f;
            public const float DefaultTweenDurationSeconds = 0.25f;
            public const float DefaultTickIntervalSeconds = 1f;
            public const float HitReactExitDelaySeconds = 0.35f;
            public const float DeathDespawnDelaySeconds = 0.35f;
        }

        public static class Movement
        {
            public const float DefaultMoveSpeed = 3.5f;
            public const float ArrivalThreshold = 0.15f;
            public const float RotationSlerpSpeed = 12f;
        }

        public static class Input
        {
            public const int MaxHotkeySlot = 9;
            public const float FailedCastLogCooldownSeconds = 1.5f;
            public const float MaxRaycastDistance = 200f;
        }

        public static class Presentation
        {
            public const int VfxPrewarmCountPerKey = 2;
            public const int CombatFloatPrewarmCount = 12;
            public const int CombatFloatMaxPoolSize = 32;
            public const string CombatFloatPoolId = "demo.combat_float";

            public const float HealingVfxScale = 1.15f;
            public const float DefaultVfxScale = 1f;
            public const float HealingVfxAlphaMultiplier = 1f;
            public const float HealingVfxMinLifetimeSeconds = 5f;
            public const float InstantVfxMinDespawnSeconds = 0.6f;
            public const float VfxWrapperScale = 0.6f;
        }

        public static class HealthBar
        {
            public static readonly Color DefaultFillColor = new Color(0.25f, 0.9f, 0.35f);
            public static readonly Color DamageFlashColor = new Color(1f, 0.35f, 0.35f);
            public static readonly Color HealFlashColor = new Color(0.35f, 1f, 0.45f);
            public static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.55f);

            public const float DefaultTweenSeconds = 0.25f;
            public const float SmallTickTweenThresholdSeconds = 0.35f;
            public const float SmallTickMaxDelta = 0.08f;
            public const float FillDeltaEpsilon = 0.001f;
            public const float FlashInSeconds = 0.08f;
            public const float FlashOutSeconds = 0.2f;
            public const float FollowMoveEpsilonSqr = 0.0004f;
            public const float MinVisualUpdateIntervalSeconds = 0.1f;
            public const float HeightOffset = 2.1f;
            public const float CanvasScale = 0.01f;
            public const int SortingOrder = 5;
            public static readonly Vector2 BarSize = new Vector2(120f, 14f);
        }

        public static class CombatFeedback
        {
            public static readonly Color HealColor = new Color(0.35f, 1f, 0.45f, 1f);
            public static readonly Color DotColor = new Color(1f, 0.55f, 0.2f, 1f);
            public static readonly Color DamageColor = new Color(1f, 0.35f, 0.35f, 1f);

            public const float FloatHeight = 2.2f;
            public const float DurationSeconds = 1f;
            public const float RiseDistance = 0.35f;
            public const float DriftMinX = -0.08f;
            public const float DriftMaxX = 0.12f;
            public const float PeakScaleMultiplier = 1.15f;
            public const float PopInDurationFraction = 0.12f;
            public const int FontSize = 24;
            public static readonly Vector2 LabelRectSize = new Vector2(120f, 48f);
            public const float LabelWorldScale = 0.35f;
        }

        public static class Targeting
        {
            public const float RingRadius = 1.05f;
            public const float HeightOffset = 0.04f;
            public static readonly Color RingColor = new Color(0.2f, 0.9f, 1f, 0.95f);
            public const float RingRotationX = -90f;
            public const int RingSortingOrder = 10;
            public const int RingTextureSize = 64;
            public const float RingOuterRadius = 0.46f;
            public const float RingInnerRadius = 0.32f;
        }

        public static class Camera
        {
            public static readonly Vector3 DefaultPosition = new Vector3(0f, 8f, -10f);
            public static readonly Color FallbackBackgroundColor = new Color(0.55f, 0.72f, 0.85f);
            public const float NearClipPlane = 0.1f;
            public const float FarClipPlane = 200f;
        }

        public static class Lighting
        {
            public static readonly Vector3 DefaultSunRotation = new Vector3(55f, -40f, 0f);
            public const float DefaultSunIntensity = 1.05f;
        }

        public static class Ui
        {
            public const float CameraMoveEpsilonSqr = 0.0004f;
            public const float CameraRotationAngleEpsilon = 0.05f;
            public static readonly Vector2 ChildCanvasSize = new Vector2(100f, 100f);
            public const string TmpFontResourcePath = "Fonts & Materials/LiberationSans SDF";
            public const string TmpFontFallbackResourcePath = "Fonts & Materials/LiberationSans SDF - Fallback";
        }

        public static class Fsm
        {
            public const string MachineName = "Entity";
            public const string VitalityRegion = "Vitality";
            public const string ActionRegion = "Action";
            public const string InputRegion = "Input";
            public const string LocomotionRegion = "Locomotion";
            public const string StatusRegion = "Status";
            public const string AiRegion = "AI";
        }

        public static class AddressableGroups
        {
            public const string Abilities = "Abilities";
            public const string Demo = "Demo";
            public const string Vfx = "VFX";
            public const string Audio = "Audio";
            public const string Animations = "Animations";
        }

        public static class DecorNaming
        {
            public const string TreePrefix = "Tree_";
            public const string BushPrefix = "Bush_";
            public const string RockPrefix = "Rock_";
            public const string StumpPrefix = "Stump_";
        }

        public static class AbilityAuthoring
        {
            public const float DashRange = 5f;
            public const float FirewallRange = 12f;
            public const float DefencedAttackRange = 12f;
            public const float DashOffsetZ = 4f;
            public const float DashDurationSeconds = 0.25f;
            public const float FirewallVfxDelaySeconds = 0.3f;
            public const float FirewallDamageDelaySeconds = 0.5f;
            public const float FirewallDamageTotal = 25f;
            public const float FirewallCombustionDurationSeconds = 4f;
            public const float FirewallCombustionValue = 8f;
            public const float HealingDurationSeconds = 5f;
            public const float HealingValue = 12f;
            public const float DefencedAttackDamage = 15f;
            public const float DefencedAttackBleedDurationSeconds = 2f;
            public const float DefencedAttackBleedValue = 3f;
            public const float FreezingDurationSeconds = 2f;
            public const float FreezingValue = 3f;
        }
    }
}
