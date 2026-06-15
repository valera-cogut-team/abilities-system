using System.Collections.Generic;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;

namespace AvantajPrim.Abilities.Execution
{
    public static class AbilityConfigMapper
    {
        public static AbilityDefinition ToDefinition(AbilityConfigAsset asset)
        {
            var id = new AbilityId(asset.AbilityId);
            var list = new List<IAbilityComponentData>(asset.Components.Count);
            foreach (IAbilityComponentData c in asset.Components)
                list.Add(c);
            return new AbilityDefinition(
                id,
                list,
                asset.HotkeySlot,
                asset.HotkeyKeyCode,
                asset.TargetType,
                asset.RangeType,
                asset.Range);
        }
    }
}
