using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Addressables.Facade
{
    public interface IAddressablesFacade
    {
        UniTask EnsureInitializedAsync();
        UniTask<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object;
        UniTask<T> TryLoadAssetAsync<T>(string address) where T : UnityEngine.Object;
        UniTask<T> LoadAssetAsync<T>(AssetReference reference) where T : UnityEngine.Object;
        UniTask<T> TryLoadAssetAsync<T>(AssetReference reference) where T : UnityEngine.Object;
        UniTask<IReadOnlyList<T>> TryLoadAssetsByLabelAsync<T>(string label) where T : UnityEngine.Object;
        UniTask<GameObject> LoadPrefabAsync(string address);
        UniTask<GameObject> TryLoadPrefabAsync(string address);
        UniTask<GameObject> LoadPrefabAsync(AssetReferenceGameObject reference);
        UniTask<GameObject> TryLoadPrefabAsync(AssetReferenceGameObject reference);
        UniTask ReleaseAssetAsync(string address);
        bool IsAssetLoaded(string address);
    }
}
