using System;
using System.Collections.Generic;
using System.Threading;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Execution;
using Cysharp.Threading.Tasks;

namespace AvantajPrim.Abilities.Infrastructure
{
    public sealed class AbilityCastLifecycle : IAbilityCastLifecycle, IAbilityCastCompletionAwaiter
    {
        private sealed class CastState
        {
            internal int PendingEffects;
            internal bool ExecutionFinished;
            internal bool Completed;
            internal readonly UniTaskCompletionSource Completion = new();
        }

        private readonly Dictionary<int, CastState> _casts = new(4);
        private int _nextCastId = 1;

        public int BeginCast(AbilityId abilityId, EntityId casterId)
        {
            int castId = _nextCastId++;
            _casts[castId] = new CastState();
            return castId;
        }

        public void RegisterPendingEffect(int castId)
        {
            if (castId <= 0 || !_casts.TryGetValue(castId, out CastState state) || state.Completed)
            {
                return;
            }

            state.PendingEffects++;
        }

        public void CompletePendingEffect(int castId)
        {
            if (castId <= 0 || !_casts.TryGetValue(castId, out CastState state) || state.Completed)
            {
                return;
            }

            state.PendingEffects = Math.Max(0, state.PendingEffects - 1);
            TryComplete(castId, state);
        }

        public void MarkExecutionFinished(int castId)
        {
            if (castId <= 0 || !_casts.TryGetValue(castId, out CastState state) || state.Completed)
            {
                return;
            }

            state.ExecutionFinished = true;
            TryComplete(castId, state);
        }

        public void ForceComplete(int castId)
        {
            if (castId <= 0 || !_casts.TryGetValue(castId, out CastState state) || state.Completed)
            {
                return;
            }

            state.PendingEffects = 0;
            state.ExecutionFinished = true;
            TryComplete(castId, state);
        }

        public UniTask WaitForCompletionAsync(int castId, CancellationToken cancellationToken = default)
        {
            if (castId <= 0)
            {
                return UniTask.CompletedTask;
            }

            if (!_casts.TryGetValue(castId, out CastState state) || state.Completed)
            {
                return UniTask.CompletedTask;
            }

            return state.Completion.Task.AttachExternalCancellation(cancellationToken);
        }

        private void TryComplete(int castId, CastState state)
        {
            if (state.Completed || !state.ExecutionFinished || state.PendingEffects > 0)
            {
                return;
            }

            state.Completed = true;
            state.Completion.TrySetResult();
            _casts.Remove(castId);
        }
    }
}
