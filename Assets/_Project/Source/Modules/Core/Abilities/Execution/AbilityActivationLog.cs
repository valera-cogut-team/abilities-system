using System;
using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;

namespace AvantajPrim.Abilities.Execution
{
    public readonly struct AbilityReplayFrame
    {
        public readonly float Time;
        public readonly AbilityId AbilityId;
        public readonly EntityId CasterId;
        public readonly EntityId TargetId;
        public readonly string ComponentTypeName;

        public AbilityReplayFrame(float time, AbilityId abilityId, EntityId casterId, EntityId targetId, string componentTypeName)
        {
            Time = time;
            AbilityId = abilityId;
            CasterId = casterId;
            TargetId = targetId;
            ComponentTypeName = componentTypeName;
        }
    }

    public readonly struct AbilityActivationCast
    {
        public readonly float Time;
        public readonly AbilityId AbilityId;
        public readonly EntityId CasterId;
        public readonly EntityId[] TargetIds;

        public AbilityActivationCast(float time, AbilityId abilityId, EntityId casterId, EntityId[] targetIds)
        {
            Time = time;
            AbilityId = abilityId;
            CasterId = casterId;
            TargetIds = targetIds ?? Array.Empty<EntityId>();
        }
    }

    public sealed class AbilityActivationLog
    {
        private readonly List<AbilityReplayFrame> _frames = new List<AbilityReplayFrame>(64);
        private readonly List<AbilityActivationCast> _casts = new List<AbilityActivationCast>(16);
        private float _time;

        public IReadOnlyList<AbilityReplayFrame> Frames => _frames;
        public IReadOnlyList<AbilityActivationCast> Casts => _casts;

        public void Reset()
        {
            _frames.Clear();
            _casts.Clear();
            _time = 0f;
        }

        public void Tick(float deltaTime) => _time += deltaTime;

        public void RecordCast(AbilityId abilityId, EntityId casterId, IReadOnlyList<EntityId> targetIds)
        {
            if (targetIds == null || targetIds.Count == 0)
                return;

            var copy = new EntityId[targetIds.Count];
            for (int i = 0; i < targetIds.Count; i++)
                copy[i] = targetIds[i];

            _casts.Add(new AbilityActivationCast(_time, abilityId, casterId, copy));
        }

        public void Record(AbilityId abilityId, EntityId casterId, EntityId targetId, string componentTypeName)
        {
            _frames.Add(new AbilityReplayFrame(_time, abilityId, casterId, targetId, componentTypeName));
        }
    }
}
