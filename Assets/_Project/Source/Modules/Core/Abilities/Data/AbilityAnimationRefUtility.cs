using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AvantajPrim.Abilities.Data
{
    public static class AbilityAnimationRefUtility
    {
        public const string AddressPrefix = "anim_";
        private const string CastClipPrefix = "Cast_";

        public static string ResolveAnimationName(AssetReferenceT<AnimationClip> reference)
        {
            if (!AddressableAssetRefUtility.IsAssigned(reference))
                return null;

            string address = AddressableAssetRefUtility.GetRuntimeKey(reference);
            if (!string.IsNullOrEmpty(address) &&
                address.StartsWith(AddressPrefix, StringComparison.Ordinal))
            {
                return TriggerNameFromAddress(address);
            }

#if UNITY_EDITOR
            if (reference.editorAsset is AnimationClip clip && !string.IsNullOrEmpty(clip.name))
                return TriggerNameFromClipAssetName(clip.name);
#endif
            return null;
        }

        public static string TriggerNameFromClipAssetName(string clipAssetName)
        {
            if (string.IsNullOrEmpty(clipAssetName))
                return null;

            return clipAssetName.StartsWith(CastClipPrefix, StringComparison.Ordinal)
                ? clipAssetName.Substring(CastClipPrefix.Length)
                : clipAssetName;
        }

        public static string TriggerNameFromAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            return address.StartsWith(AddressPrefix, StringComparison.Ordinal)
                ? address.Substring(AddressPrefix.Length)
                : address;
        }
    }
}
