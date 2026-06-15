using System;
using System.Collections.Generic;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using AvantajPrim.AbilitiesDemo.Domain;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class TargetingService
    {
        private static readonly int TargetingLayerMask = ResolveTargetingLayerMask();

        private Camera _camera;
        private readonly List<EntityId> _selectedTargets = new List<EntityId>(4);

        public event Action SelectedTargetsChanged;

        private static int ResolveTargetingLayerMask()
        {
            int mask = LayerMask.GetMask(DemoConstants.Layers.Targeting);
            return mask != 0 ? mask : Physics.DefaultRaycastLayers;
        }

        public IReadOnlyList<EntityId> SelectedTargets => _selectedTargets;

        public EntityId? SelectedTarget => _selectedTargets.Count > 0 ? _selectedTargets[0] : null;

        public void AttachCamera(Camera camera) => _camera = camera;

        public void SetSingleSelection(EntityId targetId)
        {
            _selectedTargets.Clear();
            _selectedTargets.Add(targetId);
            NotifySelectionChanged();
        }

        public void ToggleSelection(EntityId targetId)
        {
            int index = _selectedTargets.IndexOf(targetId);
            if (index >= 0)
                _selectedTargets.RemoveAt(index);
            else
                _selectedTargets.Add(targetId);

            NotifySelectionChanged();
        }

        public void ClearSelection()
        {
            if (_selectedTargets.Count == 0)
                return;

            _selectedTargets.Clear();
            NotifySelectionChanged();
        }

        public void RemoveSelection(EntityId targetId)
        {
            if (!_selectedTargets.Remove(targetId))
                return;

            NotifySelectionChanged();
        }

        public bool IsSelected(EntityId targetId) => _selectedTargets.Contains(targetId);

        private void NotifySelectionChanged() => SelectedTargetsChanged?.Invoke();
    }
}
