using System;
using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using UnityEngine;

namespace AvantajPrim.Abilities.Data
{
    [CreateAssetMenu(fileName = "AbilityConfig", menuName = "AvantajPrim/Abilities/Ability Config")]
    public sealed class AbilityConfigAsset : ScriptableObject
    {
        [SerializeField] private string _abilityId = AbilityConstants.ComponentDefaults.DefaultAbilityId;
        [SerializeField] private string _displayName = "Ability";
        [SerializeField] private int _hotkeySlot;
        [SerializeField] private KeyCode _hotkeyKey = KeyCode.None;
        [SerializeField] private AbilityTargetType _targetType = AbilityTargetType.Enemy;
        [SerializeField] private AbilityRangeType _rangeType = AbilityRangeType.Melee;
        [SerializeField] private float _range = AbilityConstants.ComponentDefaults.DefaultRange;
        [SerializeReference] private List<IAbilityComponentData> _components = new List<IAbilityComponentData>();

        public string AbilityId => _abilityId;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? _abilityId : _displayName;
        public int HotkeySlot => _hotkeySlot;
        public KeyCode HotkeyKey => _hotkeyKey;
        public int HotkeyKeyCode => (int)_hotkeyKey;
        public AbilityTargetType TargetType => _targetType;
        public AbilityRangeType RangeType => _rangeType;
        public float Range => _range;
        public IReadOnlyList<IAbilityComponentData> Components => _components;
    }
}
