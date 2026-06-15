using System.Collections.Generic;
using System.Threading;
using Addressables.Facade;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Execution;
using Cysharp.Threading.Tasks;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public static class AbilityAddressableDiscovery
    {
        public const string AbilityLabel = "ability";

        public static async UniTask<int> LoadAllIntoCatalogAsync(
            IAddressablesFacade addressables,
            AbilityCatalog catalog,
            CancellationToken cancellationToken = default)
        {
            if (catalog != null && catalog.Count > 0)
                return catalog.Count;

            int loaded = 0;
            IReadOnlyList<AbilityConfigAsset> configs = await addressables.TryLoadAssetsByLabelAsync<AbilityConfigAsset>(AbilityLabel);

            foreach (AbilityConfigAsset asset in configs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterAsset(catalog, asset);
                loaded++;
            }

            return loaded;
        }

        public static void RegisterAsset(AbilityCatalog catalog, AbilityConfigAsset asset)
        {
            if (asset == null)
                return;

            catalog.Register(AbilityConfigMapper.ToDefinition(asset));
        }
    }
}
