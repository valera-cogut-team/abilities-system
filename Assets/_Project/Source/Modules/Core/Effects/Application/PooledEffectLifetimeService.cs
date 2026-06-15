using System.Collections.Generic;
using LifeCycle.Facade;
using UnityEngine;

namespace Effects.Application
{
    public sealed class PooledEffectLifetimeService : IUpdateHandler
    {
        private struct Entry
        {
            internal GameObject Instance;
            internal float Remaining;
        }

        private readonly IEffectsService _effects;
        private readonly List<Entry> _entries = new List<Entry>(16);
        private readonly Dictionary<int, PooledVfxHandle> _handlesByInstanceId = new Dictionary<int, PooledVfxHandle>(32);

        public PooledEffectLifetimeService(IEffectsService effects)
        {
            _effects = effects;
        }

        public void RegisterHandle(GameObject instance, PooledVfxHandle handle)
        {
            if (instance == null || handle == null)
                return;

            _handlesByInstanceId[instance.GetInstanceID()] = handle;
        }

        public PooledVfxHandle TryGetHandle(GameObject instance)
        {
            if (instance == null)
                return null;

            return _handlesByInstanceId.TryGetValue(instance.GetInstanceID(), out PooledVfxHandle handle) ? handle : null;
        }

        public void ScheduleDespawn(GameObject instance, float delaySeconds)
        {
            if (instance == null || delaySeconds <= 0f)
            {
                _effects?.Despawn(instance);
                return;
            }

            CancelScheduled(instance);
            _entries.Add(new Entry { Instance = instance, Remaining = delaySeconds });
        }

        public void CancelScheduled(GameObject instance)
        {
            if (instance == null || _entries.Count == 0)
                return;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].Instance == instance)
                    _entries.RemoveAt(i);
            }
        }

        public void OnUpdate(float deltaTime)
        {
            if (_entries.Count == 0)
                return;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry entry = _entries[i];
                if (entry.Instance == null)
                {
                    _entries.RemoveAt(i);
                    continue;
                }

                entry.Remaining -= deltaTime;
                if (entry.Remaining > 0f)
                {
                    _entries[i] = entry;
                    continue;
                }

                GameObject instance = entry.Instance;
                _entries.RemoveAt(i);
                _effects.Despawn(instance);
            }
        }
    }
}
