using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AvantajPrim.Abilities.Data
{
    public static class AddressableAssetRefUtility
    {
        public static bool IsAssigned(AssetReference reference) =>
            reference != null && !string.IsNullOrEmpty(reference.AssetGUID);

        public static string GetRuntimeKey(AssetReference reference)
        {
            if (!IsAssigned(reference))
                return null;

#if UNITY_EDITOR
            string editorAddress = ResolveEditorAddress(reference.AssetGUID);
            if (!string.IsNullOrEmpty(editorAddress))
                return editorAddress;
#endif

            if (reference.RuntimeKeyIsValid() && reference.RuntimeKey is string runtimeKey && !IsLikelyAssetGuid(runtimeKey))
                return runtimeKey;

            return null;
        }

        public static bool IsLikelyAssetGuid(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length != 32)
                return false;

            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex)
                    return false;
            }

            return true;
        }

#if UNITY_EDITOR
        private static string ResolveEditorAddress(string assetGuid)
        {
            try
            {
                Type defaultObjectType = Type.GetType(
                    "UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");
                if (defaultObjectType == null)
                    return null;

                PropertyInfo settingsProperty = defaultObjectType.GetProperty(
                    "Settings",
                    BindingFlags.Static | BindingFlags.Public);
                object settings = settingsProperty?.GetValue(null);
                if (settings == null)
                    return null;

                MethodInfo findMethod = settings.GetType().GetMethod(
                    "FindAssetEntry",
                    new[] { typeof(string) });
                object entry = findMethod?.Invoke(settings, new object[] { assetGuid });
                if (entry == null)
                    return null;

                PropertyInfo addressProperty = entry.GetType().GetProperty("address");
                return addressProperty?.GetValue(entry) as string;
            }
            catch
            {
                return null;
            }
        }
#endif
    }
}
