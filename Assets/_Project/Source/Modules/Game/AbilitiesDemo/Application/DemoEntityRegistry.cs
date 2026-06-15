using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class DemoEntityRegistry
    {
        private readonly List<EntityId> _enemyIds = new List<EntityId>();
        private readonly Dictionary<EntityId, EntityView> _views = new Dictionary<EntityId, EntityView>();

        public EntityId PlayerId { get; private set; }
        public IReadOnlyList<EntityId> EnemyIds => _enemyIds;

        public void SetPlayer(EntityId id, EntityView view)
        {
            PlayerId = id;
            RegisterView(id, view);
        }

        public void AddEnemy(EntityId id, EntityView view)
        {
            if (!_enemyIds.Contains(id))
                _enemyIds.Add(id);
            RegisterView(id, view);
        }

        public bool TryGetView(EntityId id, out EntityView view)
        {
            if (!_views.TryGetValue(id, out view))
            {
                view = null;
                return false;
            }

            if (view == null)
            {
                _views.Remove(id);
                view = null;
                return false;
            }

            return true;
        }

        public bool IsEnemy(EntityId id) => _enemyIds.Contains(id);

        public void RemoveEnemy(EntityId id)
        {
            _enemyIds.Remove(id);
            _views.Remove(id);
        }

        public void Clear()
        {
            _enemyIds.Clear();
            _views.Clear();
            PlayerId = default;
        }

        private void RegisterView(EntityId id, EntityView view)
        {
            if (view != null)
                _views[id] = view;
        }
    }
}
