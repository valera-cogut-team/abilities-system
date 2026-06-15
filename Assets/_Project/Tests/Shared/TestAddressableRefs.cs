using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AvantajPrim.Tests.Shared
{
    public static class TestAddressableRefs
    {
        public const string SfxDashPath = "Assets/_Project/Audio/SFX/SFX_Dash.wav";
        public const string VfxDashPath = "Assets/_Project/Prefabs/VFX/VFX_Dash.prefab";
        public const string AnimDashPath = "Assets/_Project/Animations/Cast/Cast_Dash.anim";

        public static SoundComponentData CreateSoundComponent(
            string assetPath = SfxDashPath,
            float volume = 1f,
            AbilityPlayTimeType playTime = AbilityPlayTimeType.OnStart)
        {
            return new SoundComponentData
            {
                PlayTimeType = playTime,
                SoundClip = AudioClip(assetPath),
                Volume = volume
            };
        }

        public static VisualFxComponentData CreateVisualFxComponent(
            string assetPath = VfxDashPath,
            AbilityTargetType targetType = AbilityTargetType.Enemy,
            AbilityDurationType durationType = AbilityDurationType.Instant)
        {
            return new VisualFxComponentData
            {
                VfxPrefab = GameObjectRef(assetPath),
                TargetType = targetType,
                DurationType = durationType
            };
        }

        public static AnimationComponentData CreateAnimationComponent(
            string assetPath = AnimDashPath,
            bool waitUntilEnd = false,
            AbilityPlayTimeType playTime = AbilityPlayTimeType.OnStart)
        {
            return new AnimationComponentData
            {
                PlayTimeType = playTime,
                CastClip = AnimationClipRef(assetPath),
                WaitUntilEnd = waitUntilEnd
            };
        }

        private static AssetReferenceT<AnimationClip> AnimationClipRef(string assetPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            return string.IsNullOrEmpty(guid) ? null : new AssetReferenceT<AnimationClip>(guid);
        }

        private static AssetReferenceT<AudioClip> AudioClip(string assetPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            return string.IsNullOrEmpty(guid) ? null : new AssetReferenceT<AudioClip>(guid);
        }

        private static AssetReferenceGameObject GameObjectRef(string assetPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            return string.IsNullOrEmpty(guid) ? null : new AssetReferenceGameObject(guid);
        }
    }
}
