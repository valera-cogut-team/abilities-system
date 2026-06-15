using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Addressables.Infrastructure
{
    public interface IAddressablesService
    {
        UniTask EnsureInitializedAsync();
        UniTask<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object;
        UniTask<T> TryLoadAssetAsync<T>(string address) where T : UnityEngine.Object;
        UniTask<T> LoadAssetAsync<T>(AssetReference reference) where T : UnityEngine.Object;
        UniTask<T> TryLoadAssetAsync<T>(AssetReference reference) where T : UnityEngine.Object;
        UniTask<IReadOnlyList<T>> TryLoadAssetsByLabelAsync<T>(string label) where T : UnityEngine.Object;
        UniTask ReleaseAssetAsync(string address);
        UniTask ReleaseAllAsync();
        bool IsAssetLoaded(string address);
    }
}

