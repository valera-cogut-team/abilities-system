using System.Collections.Generic;
using Effects.Facade;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    /// <summary>Tracks pooled VFX spawned for an entity so they can be released on death/despawn.</summary>
    public sealed class EntityAttachedVfxRegistry
    {
        private readonly Dictionary<EntityId, List<GameObject>> _byEntity = new Dictionary<EntityId, List<GameObject>>();

        private IEffectsFacade _effects;

        public void BindEffects(IEffectsFacade effects) => _effects = effects;

        public void Track(EntityId entityId, GameObject instance)
        {
            if (entityId == default || instance == null)
                return;

            if (!_byEntity.TryGetValue(entityId, out List<GameObject> list))
            {
                list = new List<GameObject>(4);
                _byEntity[entityId] = list;
            }

            list.Add(instance);
        }

        public void ReleaseAll(EntityId entityId)
        {
            if (entityId == default || !_byEntity.TryGetValue(entityId, out List<GameObject> list))
                return;

            _byEntity.Remove(entityId);
            if (_effects == null)
                return;

            foreach (GameObject instance in list)
            {
                if (instance != null)
                    _effects.Despawn(instance);
            }
        }
    }
}
