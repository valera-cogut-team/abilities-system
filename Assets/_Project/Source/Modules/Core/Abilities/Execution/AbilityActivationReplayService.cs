using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using Cysharp.Threading.Tasks;

namespace AvantajPrim.Abilities.Execution
{
    public sealed class AbilityActivationReplayService
    {
        private readonly AbilityActivationLog _log;
        private readonly System.Func<AbilityId, EntityId, EntityId, UniTask> _replaySingle;
        private readonly System.Func<AbilityId, EntityId, IReadOnlyList<EntityId>, UniTask> _replayMulti;

        public AbilityActivationLog Log => _log;

        public AbilityActivationReplayService(
            AbilityActivationLog log,
            System.Func<AbilityId, EntityId, EntityId, UniTask> replaySingle,
            System.Func<AbilityId, EntityId, IReadOnlyList<EntityId>, UniTask> replayMulti = null)
        {
            _log = log;
            _replaySingle = replaySingle;
            _replayMulti = replayMulti ?? ((abilityId, casterId, targetIds) =>
                replaySingle(abilityId, casterId, targetIds[0]));
        }

        public async UniTask ReplayLastAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            if (!TryGetLastCast(out AbilityActivationCast cast))
                return;

            await ReplayCastAsync(cast, cancellationToken);
        }

        public async UniTask ReplayByAbilityIdAsync(
            AbilityId abilityId,
            System.Threading.CancellationToken cancellationToken = default)
        {
            for (int i = _log.Casts.Count - 1; i >= 0; i--)
            {
                if (_log.Casts[i].AbilityId == abilityId)
                {
                    await ReplayCastAsync(_log.Casts[i], cancellationToken);
                    return;
                }
            }

            for (int i = _log.Frames.Count - 1; i >= 0; i--)
            {
                AbilityReplayFrame frame = _log.Frames[i];
                if (frame.AbilityId != abilityId)
                    continue;

                await _replaySingle(frame.AbilityId, frame.CasterId, frame.TargetId);
                return;
            }
        }

        private async UniTask ReplayCastAsync(
            AbilityActivationCast cast,
            System.Threading.CancellationToken cancellationToken)
        {
            if (cast.TargetIds.Length <= 1)
            {
                await _replaySingle(cast.AbilityId, cast.CasterId, cast.TargetIds[0]);
                return;
            }

            await _replayMulti(cast.AbilityId, cast.CasterId, cast.TargetIds);
        }

        private bool TryGetLastCast(out AbilityActivationCast cast)
        {
            if (_log.Casts.Count > 0)
            {
                cast = _log.Casts[_log.Casts.Count - 1];
                return true;
            }

            if (_log.Frames.Count == 0)
            {
                cast = default;
                return false;
            }

            AbilityReplayFrame last = _log.Frames[_log.Frames.Count - 1];
            cast = new AbilityActivationCast(
                last.Time,
                last.AbilityId,
                last.CasterId,
                new[] { last.TargetId });
            return true;
        }
    }
}
