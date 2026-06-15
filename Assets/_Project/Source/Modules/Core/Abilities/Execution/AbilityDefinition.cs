using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;

namespace AvantajPrim.Abilities.Execution
{
    public sealed class AbilityDefinition
    {
        public AbilityId Id { get; }
        public IReadOnlyList<IAbilityComponentData> Components { get; }
        public int HotkeySlot { get; }
        public int HotkeyKeyCode { get; }
        public AbilityTargetType TargetType { get; }
        public AbilityRangeType RangeType { get; }
        public float Range { get; }

        public AbilityDefinition(
            AbilityId id,
            IReadOnlyList<IAbilityComponentData> components,
            int hotkeySlot = 0,
            int hotkeyKeyCode = 0,
            AbilityTargetType targetType = AbilityTargetType.Enemy,
            AbilityRangeType rangeType = AbilityRangeType.Melee,
            float range = AbilityConstants.Execution.DefaultMeleeRange)
        {
            Id = id;
            Components = components;
            HotkeySlot = hotkeySlot;
            HotkeyKeyCode = hotkeyKeyCode;
            TargetType = targetType;
            RangeType = rangeType;
            Range = range;
        }
    }
}
