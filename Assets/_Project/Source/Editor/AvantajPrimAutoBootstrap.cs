using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace EditorTools
{
    /// <summary>Registers missing demo/VFX/SFX Addressables when the editor loads.</summary>
    [InitializeOnLoad]
    public static class AvantajPrimAutoBootstrap
    {
        private const string BootstrapDoneKey = "AvantajPrim.AddressablesBootstrapDone";
        private const string DemoArenaPrefabPath = "Assets/_Project/Prefabs/Demo/DemoArena.prefab";

        static AvantajPrimAutoBootstrap()
        {
            EditorApplication.delayCall += TryBootstrapOnLoad;
        }

        private static void TryBootstrapOnLoad()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (SessionState.GetBool(BootstrapDoneKey, false) && HasDemoArenaEntry())
                return;

            if (!HasDemoArenaEntry())
                AvantajPrimProjectTools.SetupAddressablesBootstrap(buildPlayerContent: false);

            if (HasDemoArenaEntry())
                SessionState.SetBool(BootstrapDoneKey, true);
        }

        private static bool HasDemoArenaEntry()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return false;

            string guid = AssetDatabase.AssetPathToGUID(DemoArenaPrefabPath);
            return !string.IsNullOrEmpty(guid) && settings.FindAssetEntry(guid) != null;
        }
    }
}
