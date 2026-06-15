using UnityEngine.AddressableAssets;

namespace AvantajPrim.AbilitiesDemo.Application
{
    internal static class DemoAddressableRefExtensions
    {
        public static string TryGetRuntimeKey(this AssetReference reference)
        {
            if (reference == null || !reference.RuntimeKeyIsValid())
                return null;

            return reference.RuntimeKey as string;
        }
    }
}
