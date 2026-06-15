using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Effects.Facade
{
    public interface IEffectsFacade
    {
        UniTask<GameObject> SpawnAsync(string address, Vector3 worldPosition, Quaternion worldRotation,
            Transform parent = null);

        UniTask PlayOneShotAsync(string address, Vector3 worldPosition, Quaternion worldRotation, Transform parent = null,
            CancellationToken cancellationToken = default);

        UniTask PrewarmAsync(string address, int count, CancellationToken cancellationToken = default);

        void ScheduleDespawn(GameObject instance, float delaySeconds);

        void Despawn(GameObject instance);
    }
}
