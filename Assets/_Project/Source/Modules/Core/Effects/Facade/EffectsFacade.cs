using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Effects.Application;
using UnityEngine;

namespace Effects.Facade
{
    public sealed class EffectsFacade : IEffectsFacade
    {
        private readonly IEffectsService _service;

        public EffectsFacade(IEffectsService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public UniTask<GameObject> SpawnAsync(string address, Vector3 worldPosition, Quaternion worldRotation,
            Transform parent = null)
            => _service.SpawnAsync(address, worldPosition, worldRotation, parent);

        public UniTask PlayOneShotAsync(string address, Vector3 worldPosition, Quaternion worldRotation,
            Transform parent = null, CancellationToken cancellationToken = default)
            => _service.PlayOneShotAsync(address, worldPosition, worldRotation, parent, cancellationToken);

        public UniTask PrewarmAsync(string address, int count, CancellationToken cancellationToken = default) =>
            _service.PrewarmAsync(address, count, cancellationToken);

        public void ScheduleDespawn(GameObject instance, float delaySeconds) =>
            _service.ScheduleDespawn(instance, delaySeconds);

        public void Despawn(GameObject instance) => _service.Despawn(instance);
    }
}
