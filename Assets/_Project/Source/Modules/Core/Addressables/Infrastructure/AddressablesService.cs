using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Addressables.Infrastructure
{
    public sealed class AddressablesService : IAddressablesService
    {
        private readonly Dictionary<string, AsyncOperationHandle> _loadedAssets = new Dictionary<string, AsyncOperationHandle>();
        private readonly object _lock = new object();
        private bool _initialized;
        private UniTask _initializeTask;

        public UniTask EnsureInitializedAsync()
        {
            if (_initialized)
                return UniTask.CompletedTask;

            if (_initializeTask.Status == UniTaskStatus.Pending)
                return _initializeTask;

            _initializeTask = InitializeInternalAsync();
            return _initializeTask;
        }

        private async UniTask InitializeInternalAsync()
        {
            AsyncOperationHandle<IResourceLocator> handle = UnityEngine.AddressableAssets.Addressables.InitializeAsync();
            await handle.ToUniTask();
            _initialized = true;
        }

        public async UniTask<T> TryLoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
                return null;

            await EnsureInitializedAsync();

            if (!await HasLocationsAsync(address, typeof(T)))
                return null;

            try
            {
                return await LoadAssetAsync<T>(address);
            }
            catch (InvalidKeyException)
            {
                return null;
            }
        }

        public UniTask<T> TryLoadAssetAsync<T>(AssetReference reference) where T : UnityEngine.Object
        {
            string address = GetReferenceAddress(reference);
            if (string.IsNullOrEmpty(address))
                return UniTask.FromResult<T>(null);

            return TryLoadAssetAsync<T>(address);
        }

        public UniTask<T> LoadAssetAsync<T>(AssetReference reference) where T : UnityEngine.Object
        {
            string address = GetReferenceAddress(reference);
            if (string.IsNullOrEmpty(address))
                throw new System.ArgumentException("AssetReference is not assigned or has no runtime key.", nameof(reference));

            return LoadAssetAsync<T>(address);
        }

        private static string GetReferenceAddress(AssetReference reference)
        {
            if (reference == null || !reference.RuntimeKeyIsValid())
                return null;

            return reference.RuntimeKey as string;
        }

        public async UniTask<IReadOnlyList<T>> TryLoadAssetsByLabelAsync<T>(string label) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(label))
                return System.Array.Empty<T>();

            await EnsureInitializedAsync();

            AsyncOperationHandle<IList<T>> handle = UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync<T>(label, null);
            try
            {
                IList<T> assets = await handle.ToUniTask();
                return assets == null ? System.Array.Empty<T>() : (IReadOnlyList<T>)assets;
            }
            catch (InvalidKeyException)
            {
                return System.Array.Empty<T>();
            }
            finally
            {
                if (handle.IsValid())
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
            }
        }

        public async UniTask<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
                throw new System.ArgumentException("Address cannot be null or empty", nameof(address));

            await EnsureInitializedAsync();

            lock (_lock)
            {
                if (_loadedAssets.TryGetValue(address, out AsyncOperationHandle handle))
                {
                    if (handle.IsValid() && handle.IsDone)
                        return handle.Result as T;
                }
            }

            AsyncOperationHandle<T> loadHandle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(address);
            T asset = await loadHandle.ToUniTask();

            lock (_lock)
            {
                _loadedAssets[address] = loadHandle;
            }

            return asset;
        }

        private static async UniTask<bool> HasLocationsAsync(string address, System.Type assetType)
        {
            AsyncOperationHandle<IList<IResourceLocation>> locationsHandle = UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(address, assetType);
            IList<IResourceLocation> locations;
            try
            {
                locations = await locationsHandle.ToUniTask();
            }
            finally
            {
                if (locationsHandle.IsValid())
                    UnityEngine.AddressableAssets.Addressables.Release(locationsHandle);
            }

            return locations != null && locations.Count > 0;
        }

        public async UniTask ReleaseAssetAsync(string address)
        {
            lock (_lock)
            {
                if (_loadedAssets.TryGetValue(address, out AsyncOperationHandle handle))
                {
                    if (handle.IsValid())
                        UnityEngine.AddressableAssets.Addressables.Release(handle);
                    _loadedAssets.Remove(address);
                }
            }

            await UniTask.Yield();
        }

        public async UniTask ReleaseAllAsync()
        {
            List<AsyncOperationHandle> handles;
            lock (_lock)
            {
                handles = new List<AsyncOperationHandle>(_loadedAssets.Values);
                _loadedAssets.Clear();
            }

            foreach (AsyncOperationHandle h in handles)
            {
                if (h.IsValid())
                    UnityEngine.AddressableAssets.Addressables.Release(h);
            }

            await UniTask.Yield();
        }

        public bool IsAssetLoaded(string address)
        {
            lock (_lock)
            {
                if (_loadedAssets.TryGetValue(address, out AsyncOperationHandle handle))
                    return handle.IsValid() && handle.IsDone;
                return false;
            }
        }
    }
}

