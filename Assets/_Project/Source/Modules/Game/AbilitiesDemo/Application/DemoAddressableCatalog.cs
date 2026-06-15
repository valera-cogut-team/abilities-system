using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AvantajPrim.AbilitiesDemo.Application
{
    [CreateAssetMenu(fileName = "DemoAddressableCatalog", menuName = "AvantajPrim/Demo Addressable Catalog")]
    public sealed class DemoAddressableCatalog : ScriptableObject
    {
        public AssetReferenceGameObject Arena;
        public AssetReferenceGameObject Player;
        public AssetReferenceGameObject Enemy;

        [Serializable]
        public struct DecorEntry
        {
            public AssetReferenceGameObject Prefab;
            public Vector3 LocalPosition;
            public float RotationY;
            public float Scale;
        }

        public DecorEntry[] ExtraDecor = Array.Empty<DecorEntry>();
        public AssetReferenceT<AudioClip>[] SoundClips = Array.Empty<AssetReferenceT<AudioClip>>();
        public AssetReferenceGameObject[] VfxPrefabs = Array.Empty<AssetReferenceGameObject>();
        public AssetReferenceT<AnimationClip>[] CastAnimationClips = Array.Empty<AssetReferenceT<AnimationClip>>();
    }
}
