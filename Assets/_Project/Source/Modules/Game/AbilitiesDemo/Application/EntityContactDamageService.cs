using System.Collections.Generic;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using LifeCycle.Facade;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class EntityContactDamageService : IUpdateHandler
    {
        private readonly DemoEntityRegistry _registry;
        private readonly DemoCombatRegistry _combatRegistry;
        private readonly EntityStateRegistry _entityStateRegistry;
        private readonly DemoGameplaySession _session;
        private readonly Dictionary<EntityId, float> _lastDamageTime = new Dictionary<EntityId, float>(8);

        public EntityContactDamageService(
            DemoEntityRegistry registry,
            DemoCombatRegistry combatRegistry,
            EntityStateRegistry entityStateRegistry,
            DemoGameplaySession session)
        {
            _registry = registry;
            _combatRegistry = combatRegistry;
            _entityStateRegistry = entityStateRegistry;
            _session = session;
        }

        public void OnUpdate(float deltaTime)
        {
            if (!_session.IsActive || _registry.PlayerId == default)
                return;

            if (IsDead(_registry.PlayerId))
                return;

            if (!_registry.TryGetView(_registry.PlayerId, out EntityView playerView))
                return;

            Vector3 playerPos = playerView.transform.position;
            float contactDistanceSqr = DemoConstants.Combat.ContactDistance * DemoConstants.Combat.ContactDistance;
            float now = Time.time;

            foreach (EntityId enemyId in _registry.EnemyIds)
            {
                if (IsDead(enemyId))
                    continue;

                if (!_registry.TryGetView(enemyId, out EntityView enemyView))
                    continue;

                Vector3 delta = enemyView.transform.position - playerPos;
                delta.y = 0f;
                if (delta.sqrMagnitude > contactDistanceSqr)
                    continue;

                if (!CanDamage(now, _registry.PlayerId) || !CanDamage(now, enemyId))
                    continue;

                ApplyContactDamage(_registry.PlayerId);
                ApplyContactDamage(enemyId);
                _lastDamageTime[_registry.PlayerId] = now;
                _lastDamageTime[enemyId] = now;
            }
        }

        private bool CanDamage(float now, EntityId id)
        {
            if (!_lastDamageTime.TryGetValue(id, out float lastTime))
                return true;

            return now - lastTime >= DemoConstants.Combat.ContactCooldownSeconds;
        }

        private void ApplyContactDamage(EntityId id)
        {
            if (!_combatRegistry.TryGet(id, out EntityCombatState combat))
                return;

            combat.ApplyDamage(DemoConstants.Combat.ContactDamageAmount);
        }

        private bool IsDead(EntityId id)
        {
            return _entityStateRegistry.TryGet(id, out IEntityStateMachineController controller) &&
                   controller.IsInState(EntityStatePaths.Vitality.Dead);
        }
    }
}
