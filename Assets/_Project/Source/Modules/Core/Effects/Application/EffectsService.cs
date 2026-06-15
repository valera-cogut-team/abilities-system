using System;
using System.Collections.Generic;
using System.Threading;
using Addressables.Facade;
using Cysharp.Threading.Tasks;
using Logger.Facade;
using Pool.Application;
using Pool.Facade;
using UnityEngine;

namespace Effects.Application
{
    public sealed class EffectsService : IEffectsService
    {
        private readonly IAddressablesFacade _addressables;
        private readonly IPoolFacade _poolFacade;
        private readonly ILoggerFacade _logger;
        private readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, IObjectPool<PooledVfxHandle>> _pools = new Dictionary<string, IObjectPool<PooledVfxHandle>>();
        private readonly object _poolLock = new object();

        private GameObject _host;
        private PooledEffectLifetimeService _lifetime;

        public EffectsService(IAddressablesFacade addressables, IPoolFacade poolFacade, ILoggerFacade logger)
        {
            _addressables = addressables ?? throw new ArgumentNullException(nameof(addressables));
            _poolFacade = poolFacade ?? throw new ArgumentNullException(nameof(poolFacade));
            _logger = logger;
            _lifetime = new PooledEffectLifetimeService(this);
        }

        public PooledEffectLifetimeService Lifetime => _lifetime;

        public async UniTask PrewarmAsync(string address, int count, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(address) || count <= 0)
                return;

            GameObject prefab = await LoadPrefabAsync(address, cancellationToken);
            if (prefab == null)
                return;

            EnsureHost();
            IObjectPool<PooledVfxHandle> pool = GetOrCreatePool(address, prefab);
            while (pool.InactiveCount < count)
                pool.Return(CreatePooledInstance(address, prefab));
        }

        public async UniTask<GameObject> SpawnAsync(string address, Vector3 worldPosition, Quaternion worldRotation,
            Transform parent = null)
        {
            if (string.IsNullOrEmpty(address))
            {
                _logger?.LogWarning("[Effects] SpawnAsync: address is empty.");
                return null;
            }

            GameObject prefab = await LoadPrefabAsync(address);
            if (prefab == null)
            {
                _logger?.LogWarning($"[Effects] Prefab at '{address}' is null.");
                return null;
            }

            EnsureHost();
            IObjectPool<PooledVfxHandle> pool = GetOrCreatePool(address, prefab);
            PooledVfxHandle pooled = pool.Get();
            pooled.Activate(worldPosition, worldRotation, parent != null ? parent : _host.transform);
            return pooled.GameObject;
        }

        public async UniTask PlayOneShotAsync(string address, Vector3 worldPosition, Quaternion worldRotation,
            Transform parent = null, CancellationToken cancellationToken = default,
            float fallbackLifetimeSeconds = EffectsConstants.DefaultOneShotLifetimeSeconds)
        {
            GameObject instance = await SpawnAsync(address, worldPosition, worldRotation, parent);
            if (instance == null)
                return;

            float ttl = EstimateOneShotLifetime(instance, fallbackLifetimeSeconds);
            _lifetime.ScheduleDespawn(instance, ttl);
        }

        public void ScheduleDespawn(GameObject instance, float delaySeconds) =>
            _lifetime.ScheduleDespawn(instance, delaySeconds);

        public void Despawn(GameObject instance)
        {
            if (instance == null)
                return;

            _lifetime?.CancelScheduled(instance);

            PooledVfxHandle pooled = _lifetime?.TryGetHandle(instance);
            if (pooled == null || string.IsNullOrEmpty(pooled.Address))
            {
                UnityEngine.Object.Destroy(instance);
                return;
            }

            lock (_poolLock)
            {
                if (_pools.TryGetValue(pooled.Address, out IObjectPool<PooledVfxHandle> pool))
                    pool.Return(pooled);
                else
                    UnityEngine.Object.Destroy(instance);
            }
        }

        public void Shutdown()
        {
            _lifetime = new PooledEffectLifetimeService(this);
            lock (_poolLock)
                _pools.Clear();

            if (_host != null)
            {
                UnityEngine.Object.Destroy(_host);
                _host = null;
            }
        }

        private async UniTask<GameObject> LoadPrefabAsync(string address, CancellationToken cancellationToken = default)
        {
            if (_prefabCache.TryGetValue(address, out GameObject cached))
                return cached;

            GameObject prefab = await _addressables.TryLoadPrefabAsync(address)
                .AttachExternalCancellation(cancellationToken);
            if (prefab != null)
                _prefabCache[address] = prefab;

            return prefab;
        }

        private void EnsureHost()
        {
            if (_host != null)
                return;

            _host = new GameObject(EffectsConstants.HostObjectName);
            UnityEngine.Object.DontDestroyOnLoad(_host);
        }

        private IObjectPool<PooledVfxHandle> GetOrCreatePool(string address, GameObject prefab)
        {
            lock (_poolLock)
            {
                if (_pools.TryGetValue(address, out IObjectPool<PooledVfxHandle> existing))
                    return existing;

                string poolId = $"effects.{address}";
                IObjectPool<PooledVfxHandle> pool = null;
                pool = _poolFacade.GetPool<PooledVfxHandle>(poolId)
                    ?? _poolFacade.CreatePool(poolId, () => CreatePooledInstance(address, prefab),
                        EffectsConstants.DefaultPrewarmCount, EffectsConstants.DefaultMaxPoolSize);

                _pools[address] = pool;
                return pool;
            }
        }

        private PooledVfxHandle CreatePooledInstance(string address, GameObject prefab)
        {
            EnsureHost();
            GameObject instance = UnityEngine.Object.Instantiate(prefab, _host.transform);
            instance.name = $"{prefab.name} (pooled)";
            instance.SetActive(false);

            var pooled = new PooledVfxHandle(instance.transform);
            pooled.Bind(address);
            _lifetime.RegisterHandle(instance, pooled);
            return pooled;
        }

        private static float EstimateOneShotLifetime(GameObject go, float fallbackSeconds)
        {
            ParticleSystem[] systems = go.GetComponentsInChildren<ParticleSystem>(true);
            if (systems == null || systems.Length == 0)
                return Mathf.Max(fallbackSeconds, EffectsConstants.MinParticleLifetimeSeconds);

            float maxEnd = 0f;
            foreach (ParticleSystem ps in systems)
            {
                ParticleSystem.MainModule main = ps.main;
                if (main.loop)
                    return Mathf.Max(fallbackSeconds, EffectsConstants.LoopingParticleFallbackLifetimeSeconds);

                float life = Mathf.Max(main.startLifetime.constant, main.startLifetime.constantMax);
                maxEnd = Mathf.Max(maxEnd, main.duration + life);
            }

            return Mathf.Max(maxEnd, EffectsConstants.MinParticleLifetimeSeconds);
        }
    }
}
