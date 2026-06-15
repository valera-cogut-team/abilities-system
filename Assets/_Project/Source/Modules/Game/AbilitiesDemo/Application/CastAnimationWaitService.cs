using System.Collections.Generic;
using System.Threading;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using Cysharp.Threading.Tasks;
using LifeCycle.Facade;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class CastAnimationWaitService : IUpdateHandler
    {
        private struct WaitEntry
        {
            internal IEntityStateMachineController Controller;
            internal string AnimationName;
            internal float Elapsed;
            internal int Phase;
            internal UniTaskCompletionSource Completion;
            internal CancellationToken CancellationToken;
        }

        private readonly EntityStateRegistry _registry;
        private readonly List<WaitEntry> _entries = new List<WaitEntry>(4);

        public CastAnimationWaitService(EntityStateRegistry registry)
        {
            _registry = registry;
        }

        public UniTask WaitAsync(EntityId entityId, string animationName, CancellationToken cancellationToken = default)
        {
            if (!_registry.TryGet(entityId, out IEntityStateMachineController controller))
                return UniTask.CompletedTask;

            var tcs = new UniTaskCompletionSource();
            _entries.Add(new WaitEntry
            {
                Controller = controller,
                AnimationName = animationName,
                Completion = tcs,
                CancellationToken = cancellationToken
            });

            return tcs.Task.AttachExternalCancellation(cancellationToken);
        }

        public void OnUpdate(float deltaTime)
        {
            if (_entries.Count == 0)
                return;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                WaitEntry entry = _entries[i];
                if (entry.CancellationToken.IsCancellationRequested)
                {
                    entry.Completion.TrySetCanceled(entry.CancellationToken);
                    _entries.RemoveAt(i);
                    continue;
                }

                if (entry.Controller == null)
                {
                    entry.Completion.TrySetResult();
                    _entries.RemoveAt(i);
                    continue;
                }

                float elapsed = entry.Elapsed;
                int phase = entry.Phase;
                bool done = entry.Controller.TickCastAnimationWait(
                    entry.AnimationName,
                    ref elapsed,
                    ref phase,
                    deltaTime);

                entry.Elapsed = elapsed;
                entry.Phase = phase;
                _entries[i] = entry;

                if (!done)
                    continue;

                entry.Completion.TrySetResult();
                _entries.RemoveAt(i);
            }
        }
    }
}
