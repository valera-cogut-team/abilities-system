using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Addressables.Infrastructure;
using Core;
using Logger.Facade;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Addressables.Facade
{
    public sealed class AddressablesFacade : IAddressablesFacade
    {
        private readonly IAddressablesService _service;
        private readonly IModuleContext _context;

        public AddressablesFacade(IAddressablesService service, IModuleContext context)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _context = context;
        }

        public UniTask EnsureInitializedAsync()
            => _service.EnsureInitializedAsync();

        public UniTask<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
            => _service.LoadAssetAsync<T>(address);

        public UniTask<T> TryLoadAssetAsync<T>(string address) where T : UnityEngine.Object
            => _service.TryLoadAssetAsync<T>(address);

        public UniTask<T> LoadAssetAsync<T>(AssetReference reference) where T : UnityEngine.Object
            => _service.LoadAssetAsync<T>(reference);

        public UniTask<T> TryLoadAssetAsync<T>(AssetReference reference) where T : UnityEngine.Object
            => _service.TryLoadAssetAsync<T>(reference);

        public UniTask<IReadOnlyList<T>> TryLoadAssetsByLabelAsync<T>(string label) where T : UnityEngine.Object
            => _service.TryLoadAssetsByLabelAsync<T>(label);

        public UniTask<GameObject> LoadPrefabAsync(string address)
            => _service.LoadAssetAsync<GameObject>(address);

        public UniTask<GameObject> TryLoadPrefabAsync(string address)
            => _service.TryLoadAssetAsync<GameObject>(address);

        public UniTask<GameObject> LoadPrefabAsync(AssetReferenceGameObject reference)
            => _service.LoadAssetAsync<GameObject>(reference);

        public UniTask<GameObject> TryLoadPrefabAsync(AssetReferenceGameObject reference)
            => _service.TryLoadAssetAsync<GameObject>(reference);

        public UniTask ReleaseAssetAsync(string address)
            => _service.ReleaseAssetAsync(address);

        public bool IsAssetLoaded(string address)
            => _service.IsAssetLoaded(address);

        internal void LogInfo(string message) => _context?.GetModuleFacade<ILoggerFacade>()?.LogInfo(message);
    }
}

